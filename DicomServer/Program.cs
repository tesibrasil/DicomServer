using Dicom.Log;
using DicomServer.CStore;
using DicomServer.Modules;
using DicomServer.Worklist;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace DicomServer
{
    public static class Program
    {
        public static string AssemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static List<Module> RunningModules = new List<Module>();
        public static bool DebugMode = false;
        public static bool DebugSaveDcm = true;
        public static string DebugDcmFolder = AssemblyLocation;

        public static void Main(string[] args)
        {
#if DEBUG
            DebugMode = true;
            Start(args);            
#else
            if (args.Length > 0 && args[0].ToLower() == "-d") DebugMode = true;
            
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
            StartWorklistServer();
            StartCStoreServer();
            if (DebugMode)
            {
                Console.WriteLine("Press any key to stop the Services");
                Console.Read();
                Stop();
            }
#if DEBUG
            Console.WriteLine("Press any key to stop the Services");
            Console.Read();
            Stop();
#endif
        }

        public static void Stop(string[] args = null)
        {
            StopCStoreServer();
            StopWorklistServer();
        }

        #region C-Store SCP
        static void StartCStoreServer()
        {
            LogManager.SetImplementation(ConsoleLogManager.Instance);

            if (RunningModules.Count > 0)
            {
                foreach (var Module in RunningModules)
                {
                    if (Module.UseCSSCP)
                    {
                        LogHelper.Verbose($"Starting {Module.ModuleType.ToString()} C-Store SCP Server with AET: {Module.CSAETitle} on Port {Module.CSPort}", DebugMode);
                        Task.Factory.StartNew(() =>
                        {
                            CStoreServer.Start(Module);
                        });
                    }
                }
            }
        }

        static void StopCStoreServer(Module module = null)
        {
            if (module is null)
            {
                foreach (var m in RunningModules)
                {
                    if (m.UseCSSCP)
                    {
                        LogHelper.Verbose($"Stopping {m.ModuleType.ToString()} C-Store SCP Server AET({m.CSAETitle}), port ({m.CSPort})", DebugMode);
                        CStoreServer.Stop(m.WLPort);
                    }
                }
            }
            else
            {
                if (module.UseCSSCP)
                {
                    LogHelper.Verbose($"Stopping all C-Store SCP Servers currently running", DebugMode);
                    CStoreServer.Stop(module.WLPort);
                }
            }
        }
        #endregion

        #region Worklist SCP
        static void StartWorklistServer()
        {
            LogManager.SetImplementation(ConsoleLogManager.Instance);
            if (RunningModules.Count > 0)
            {
                foreach (var Module in RunningModules)
                {
                    if (Module.UseWLSCP)
                    {
                        LogHelper.Verbose($"Starting {Module.ModuleType.ToString()} Worklist SCP Server with AET: {Module.WLAETitle} on Port {Module.WLPort}", DebugMode);
                        Task.Factory.StartNew(() =>
                        {
                            WorklistServer.Start(Module);
                        });
                    }
                }
            }
        }

        static void StopWorklistServer(Module module = null)
        {
            if (module is null)
            {
                foreach (var m in RunningModules)
                {
                    if (m.UseWLSCP)
                    {
                        LogHelper.Verbose($"Stopping {m.ModuleType.ToString()} Worklist SCP Server AET({m.WLAETitle}), port ({m.WLPort})", DebugMode);
                        WorklistServer.Stop(m.WLPort);
                    }
                }
            }
            else
            {
                if (module.UseWLSCP)
                {
                    LogHelper.Verbose($"Stopping all Worklist SCP Servers currently running", DebugMode);
                    WorklistServer.Stop(module.WLPort);
                }
            }
        }
        #endregion

        public static void ReadConfiguration()
        {
            try
            {
                string DebugSectionName = $"Debug Mode";

                DebugSaveDcm = Convert.ToBoolean(Convert.ToInt32(CfgHelper.Read(
                    DebugSectionName,
                    CfgHelper.GetKeyFor(CfgKeys.SaveDcm),
                    "1")));

                DebugDcmFolder = CfgHelper.Read(
                    DebugSectionName,
                    CfgHelper.GetKeyFor(CfgKeys.DcmFolder),
                    AssemblyLocation);

                RunningModules.Clear();

                //Modules
                var Modules = Enum.GetValues(typeof(IntegratedModules));
                foreach (IntegratedModules Module in Modules)
                {
                    string ModuleSectionName = $"Module {Module.ToString()}";

                    bool ModuleExists = CfgHelper.Read(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.WLPort)).Length > 0;
                    if (ModuleExists)
                    {
                        if (!File.Exists(CfgHelper.sCfgFile))
                        {
                            CfgHelper.Write(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.DBConn),
                                DicomServer.Modules.Module.DefaultModule.ConnectionString);
                        }

                        //Database                                 
                        var DbConn = CfgHelper.Read(
                            ModuleSectionName,
                            CfgHelper.GetKeyFor(CfgKeys.DBConn),
                            DicomServer.Modules.Module.DefaultModule.ConnectionString);

                        var CfgIsUsingDefaultDBConn =
                            (DbConn.ToUpper() == DicomServer.Modules.Module.DefaultModule.ConnectionString);

                        if (!CfgIsUsingDefaultDBConn &&
                            (DbConn.ToUpper().Contains("DRIVER=") || DbConn.ToUpper().Contains("DSN=")))
                        {
                            CfgHelper.Write(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.DBConn),
                                CryptoHelper.Encrypt(DbConn));
                        }

                        var NewModule = new Module
                        {
                            ConnectionString = CfgIsUsingDefaultDBConn ? DbConn : CryptoHelper.Decrypt(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.DBConn),
                                DicomServer.Modules.Module.DefaultModule.ConnectionString)),

                            UseWLSCP = Convert.ToBoolean(Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.UseWLSCP),
                                DicomServer.Modules.Module.DefaultModule.UseWLSCP ? "1" : "0"))),

                            WLAETitle = CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.WLAETitle),
                                DicomServer.Modules.Module.DefaultModule.WLAETitle),

                            WLPort = Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.WLPort),
                                DicomServer.Modules.Module.DefaultModule.WLPort.ToString())),

                            ItemsLoaderTimeSpan = Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.ItemsLoaderTimeSpan),
                                DicomServer.Modules.Module.DefaultModule.ItemsLoaderTimeSpan.ToString())),

                            WLUsesAssociationCallingAE = Convert.ToBoolean(Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.WLUsesAssociationCallingAE),
                                DicomServer.Modules.Module.DefaultModule.WLUsesAssociationCallingAE ? "1" : "0"))),

                            WLViewName = CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.WLViewName),
                                DicomServer.Modules.Module.DefaultModule.WLViewName),

                            UseCSSCP = Convert.ToBoolean(Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.UseCSSCP),
                                DicomServer.Modules.Module.DefaultModule.UseCSSCP ? "1" : "0"))),

                            CSAETitle = CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSAETitle),
                                DicomServer.Modules.Module.DefaultModule.CSAETitle),

                            CSPort = Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSPort),
                                DicomServer.Modules.Module.DefaultModule.CSPort.ToString())),

                            CSDocFolder = CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSDocFolder),
                                DicomServer.Modules.Module.DefaultModule.CSDocFolder.ToString()),

                            CSImgFolder = CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSImgFolder),
                                DicomServer.Modules.Module.DefaultModule.CSImgFolder.ToString()),

                            CSMirthEndpoint = CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSMirthEndpoint),
                                DicomServer.Modules.Module.DefaultModule.CSMirthEndpoint.ToString()),

                            CSMirthMessageType = Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSMirthMessageType),
                                DicomServer.Modules.Module.DefaultModule.CSMirthMessageType.ToString())),

                            CSMaxImgSizeKB = Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSMaxImgSizeKB),
                                DicomServer.Modules.Module.DefaultModule.CSMaxImgSizeKB.ToString())),

                            CSStorePdfs = Convert.ToBoolean(Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSStorePdfs),
                                DicomServer.Modules.Module.DefaultModule.CSStorePdfs ? "1" : "0"))),

                            CSStoreImages = Convert.ToBoolean(Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSStoreImages),
                                DicomServer.Modules.Module.DefaultModule.CSStoreImages ? "1" : "0"))),

                            CSStoreMeasuremnts = Convert.ToBoolean(Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSStoreMeasuremnts),
                                DicomServer.Modules.Module.DefaultModule.CSStoreMeasuremnts ? "1" : "0"))),

                            //Gabriel BUG 6225 - Lista DO
                            CSVerificationTags = CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSVerificationTags),
                                DicomServer.Modules.Module.DefaultModule.CSVerificationTags.ToString()),

                            CSEnableImageComments = Convert.ToBoolean(Convert.ToInt32(CfgHelper.Read(
                                ModuleSectionName,
                                CfgHelper.GetKeyFor(CfgKeys.CSEnableImageComments),
                                DicomServer.Modules.Module.DefaultModule.CSEnableImageComments ? "1" : "0"))),

                            ModuleType = Module
                        };

                        //Disable server if there is already a server registered with same AET or Port
                        if (RunningModules.FirstOrDefault(mod =>
                            mod.WLAETitle == NewModule.WLAETitle || mod.WLPort == NewModule.WLPort)
                            != null)
                        {
                            var m = RunningModules.First(mod => mod.WLAETitle == NewModule.WLAETitle || mod.WLPort == NewModule.WLPort);

                            NewModule.UseWLSCP = false;
                            CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.UseWLSCP), "0");
                            LogHelper.Warning($"{ModuleSectionName} Worklist SCP will be disabled because it's AET or IP is already in use", DebugMode);
                            LogHelper.Warning($"{ModuleSectionName} = ({NewModule.WLAETitle},{NewModule.WLPort}) | {m.ModuleType.ToString()} = ({m.WLAETitle},{m.WLPort})", DebugMode);

                        }
                        if (RunningModules.FirstOrDefault(mod =>
                            mod.CSAETitle == NewModule.CSAETitle || mod.CSPort == NewModule.CSPort)
                            != null)
                        {
                            var m = RunningModules.First(mod => mod.WLAETitle == NewModule.WLAETitle || mod.WLPort == NewModule.WLPort);

                            NewModule.UseCSSCP = false;
                            CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.UseCSSCP), "0");
                            LogHelper.Warning($"{ModuleSectionName} C-Store SCP will be disabled because it's AET or IP is already in use", DebugMode);
                            LogHelper.Warning($"{ModuleSectionName} = ({NewModule.CSAETitle},{NewModule.CSPort}) | {m.ModuleType.ToString()} = ({m.CSAETitle},{m.CSPort})", DebugMode);
                        }
                        //
                        RunningModules.Add(NewModule);
                    }
                }

                if (RunningModules.Count == 0) //If no modules are running, implement the default Module
                {                    
                    CfgHelper.Write(DebugSectionName, CfgHelper.GetKeyFor(CfgKeys.SaveDcm), "1");
                    CfgHelper.Write(DebugSectionName, CfgHelper.GetKeyFor(CfgKeys.DcmFolder), AssemblyLocation);

                    //

                    string ModuleSectionName = $"Module {IntegratedModules.Default}";                    

                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.DBConn), Module.DefaultModule.ConnectionString);

                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.UseWLSCP), Module.DefaultModule.UseWLSCP ? "1" : "0");
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.WLAETitle), Module.DefaultModule.WLAETitle);
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.WLPort), Module.DefaultModule.WLPort.ToString());
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.ItemsLoaderTimeSpan), Module.DefaultModule.ItemsLoaderTimeSpan.ToString());
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.WLUsesAssociationCallingAE), Module.DefaultModule.WLUsesAssociationCallingAE ? "1" : "0");
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.WLViewName), Module.DefaultModule.WLViewName);

                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.UseCSSCP), Module.DefaultModule.UseCSSCP ? "1" : "0");
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSAETitle), Module.DefaultModule.CSAETitle);
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSPort), Module.DefaultModule.CSPort.ToString());
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSDocFolder), Module.DefaultModule.CSDocFolder);
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSImgFolder), Module.DefaultModule.CSImgFolder);
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSMirthEndpoint), Module.DefaultModule.CSMirthEndpoint);
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSMirthMessageType), Module.DefaultModule.CSMirthMessageType.ToString());
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSMaxImgSizeKB), Module.DefaultModule.CSMaxImgSizeKB.ToString());
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSStorePdfs), Module.DefaultModule.CSStorePdfs ? "1" : "0");
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSStoreImages), Module.DefaultModule.CSStoreImages ? "1" : "0");
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSStoreMeasuremnts), Module.DefaultModule.CSStoreMeasuremnts ? "1" : "0");
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSVerificationTags), Module.DefaultModule.CSVerificationTags.ToString());//Gabriel BUG 6225 - Lista DO
                    CfgHelper.Write(ModuleSectionName, CfgHelper.GetKeyFor(CfgKeys.CSEnableImageComments), Module.DefaultModule.CSEnableImageComments ? "1" : "0"); //Gabriel BUG 6225 - Lista DO

                    RunningModules.Add(Module.DefaultModule);
                }
            }
            catch (Exception error)
            {
                LogHelper.Fatal($"Failed to load configuration from {CfgHelper.sCfgFile} {Environment.NewLine} {error.Message}", DebugMode);
            }
        }
    }
}
