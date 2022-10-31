using Dicom.Network;
using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace DicomClient
{
    static class Program
    {
        public static string AssemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static Thread t1; //Main Files 
        public static Thread t2; //Retry Files

        public static bool DebugMode = false;
        public static string ServerHost = string.Empty;

        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].ToLower() == "-d") DebugMode = true;
#if DEBUG
            DebugMode = true;
            Start(args);
#else
            if (!DebugMode)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new Service()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                Start(args);
            }
#endif
        }


        public static void Start(string[] args = null)
        {
            ReadConfiguration();
            t1 = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        int sleep = Convert.ToInt32(CfgHelper.Read("Main", "pooling_time_main", "5000"));
                        string dir = CfgHelper.Read("Main", "pooling_directory", "");

                        string server_host = ServerHost;
                        string server_port = CfgHelper.Read("Main", "server_ip", "11112");
                        string server_aet = CfgHelper.Read("Main", "server_aet", "TESICSSCP");
                        string local_aet = CfgHelper.Read("Main", "local_aet", "DICOMTOOL");

                        Thread.Sleep(sleep);

                        DirectoryInfo di = new DirectoryInfo(dir);
                        FileInfo[] files = di.GetFiles("*.dcm");

                        if (files.Length > 0)
                        {
                            foreach (FileInfo file in files)
                            {
                                var client = new Dicom.Network.DicomClient();
                                client.NegotiateAsyncOps();

                                DicomCStoreRequest request = new DicomCStoreRequest(file.FullName);
                                request.OnResponseReceived = (DicomCStoreRequest rq, DicomCStoreResponse rp) =>
                                {
                                    if (rp.Status == DicomStatus.Success)
                                    {
                                        LogHelper.Write($"Successfully sent {file.Name} to {server_host} ({server_aet} : {server_port})");
                                        file.Delete();
                                        LogHelper.Write($"File {file.Name} deleted");
                                    }
                                    else
                                    {
                                        LogHelper.Write($"Failed to send {file.Name} to {server_host} ({server_aet} : {server_port})");
                                        LogHelper.Write($"Client received {rp.Status.ToString()}");

                                        if (!Directory.Exists(dir + "\\Retry")) Directory.CreateDirectory(dir + "\\Retry");
                                        file.MoveTo(dir + "\\Retry\\" + file.Name + file.Extension);

                                        LogHelper.Write($"File {file.Name} moved to Retry Directory");
                                    }
                                };

                                client.AddRequest(request);

                                client.Send(server_host, Convert.ToInt32(server_port), false, local_aet, server_aet);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogHelper.Write($"An Exception was thrown: {e.Message}");
                    }
                }

            });
            t1.Start();

            t2 = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        int sleep = Convert.ToInt32(CfgHelper.Read("Main", "pooling_time_retry", "60000"));
                        string dir = CfgHelper.Read("Main", "pooling_directory", "") + "\\Retry";

                        string server_host = ServerHost;
                        string server_port = CfgHelper.Read("Main", "server_ip", "11112");
                        string server_aet = CfgHelper.Read("Main", "server_aet", "TESICSSCP");
                        string local_aet = CfgHelper.Read("Main", "local_aet", "DICOMTOOL");

                        Thread.Sleep(sleep);

                        DirectoryInfo di = new DirectoryInfo(dir);
                        FileInfo[] files = di.GetFiles("*.dcm");

                        if (files.Length > 0)
                        {
                            foreach (FileInfo file in files)
                            {
                                var client = new Dicom.Network.DicomClient();
                                client.NegotiateAsyncOps();

                                DicomCStoreRequest request = new DicomCStoreRequest(file.FullName);
                                request.OnResponseReceived = (DicomCStoreRequest rq, DicomCStoreResponse rp) =>
                                {
                                    if (rp.Status == DicomStatus.Success)
                                    {
                                        LogHelper.Write($"Successfully sent {file.Name} to {server_host} ({server_aet} : {server_port})");
                                        file.Delete();
                                        LogHelper.Write($"File {file.Name} deleted");
                                    }
                                    else
                                    {
                                        LogHelper.Write($"Failed to send {file.Name} to {server_host} ({server_aet} : {server_port})");
                                        LogHelper.Write($"Client received {rp.Status.ToString()}");

                                        if (!Directory.Exists(dir + "\\Failed")) Directory.CreateDirectory(dir + "\\Failed");

                                        file.MoveTo(dir + "\\Failed\\" + file.Name + file.Extension);
                                        LogHelper.Write($"File {file.Name} moved to Failed Directory");
                                    }
                                        
                                };

                                client.AddRequest(request);

                                client.Send(server_host, Convert.ToInt32(server_port), false, local_aet, server_aet);
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        LogHelper.Write($"An Exception was thrown: {e.Message}");
                    }
                }
            });
            t2.Start();

            LogHelper.Write("Client Started");
        }

        public static void Stop()
        {
            t1.Abort();
            t2.Abort();

            LogHelper.Write("Client Stopped");
        }

        public static void ReadConfiguration()
        {            
            try
            {
                CfgHelper.tagDSNCONNECTION conn = CfgHelper.ReadtagDSNCONNECTION("ODBC Settings", "DSN");
                ServerHost = ExtractServer(DecryptString(conn.strServer));
            }
            catch (Exception e)
            {
                LogHelper.Write($"Failed to Read Configuration ({e.Message}).");
            }
            if (ServerHost.Length == 0)
            {
                LogHelper.Write($"Service did not find DSN Key on Endox Configuration file.");
                ServerHost = CfgHelper.Read("Main", "server_host", "LOCALHOST");
            }
        }

        public static string DecryptString(char[] szSource)
        {
            string result = string.Empty;
            for (int i = 0; i < szSource.Length; i++)
            {
                if(szSource[i] != '\0')
                    result += (char)( (int)szSource[i] + ((i % 2) == 0 ? -5 : 5) );
            }
            
            return result;
        }

        public static string ExtractServer(string text)
        {
            text = text.ToUpper();

            if (text.IndexOf("\\") > 0)
                return text.Substring(0, (text.IndexOf("\\")));

            if (text.IndexOf(",") > 0)
                return text.Substring(0, (text.IndexOf(",")));

            if (text.IndexOf(":") > 0)
                return text.Substring(0, (text.IndexOf(":")));

            if (text.IndexOf("/") > 0)
                return text.Substring(0, (text.IndexOf("/")));

            if (text.IndexOf(";") > 0)
                return text.Substring(0, (text.IndexOf(";")));

            return text.ToUpper();
            
            /*
                        int iSrv = text.IndexOf("SRV");
                        int iTes = text.IndexOf("-TESI");

                        string server = text.Substring(
                            (iSrv + 3),
                            (iTes - (iSrv + 3) ));

                        bool bIs2 = false;
                        if((iTes + 6) <= text.Length)
                            bIs2 = text.Substring(iTes + 5, 1) == "2" ? true : false;

                        return bIs2 ? server + "-2" : server;
            */
        }
    }
}
