using System;
using Microsoft.Owin.Hosting;
using System.Net.Http;

namespace DicomAPI
{
    public class Server
    {
        internal static string BaseAddress = string.Empty;
        internal static IDisposable App = null;
        static void Main(string[] args)
        {
            Start("http://localhost:3031");
            Console.WriteLine($"Server Started at {BaseAddress} \n press any key to stop");
            Console.ReadKey();
            Stop();
            Console.WriteLine($"Server Stopped");
            Console.ReadKey();
        }

        public static void Start(string serverUrl)
        {
            BaseAddress = serverUrl;
            
            try
            {
                App = WebApp.Start<Startup>(url:BaseAddress);
            }
            catch
            {
                if (App != null)
                {
                    App.Dispose();
                    App = null;
                }
            }            
        }
        public static void Stop()
        {
            if (App != null)
            {
                App.Dispose();
                App = null;
            }
        }

        public static string GET(Commands.GET cmd, string param = null)
        {
            try
            {
                if (App == null) return "Cannot perform operation. Server is not alive...";

                using (HttpClient client = new HttpClient())
                {
                    string methodUrl = BaseAddress + Commands.GetEnumDescription(cmd) + param ?? "";

                    var response = client.GetAsync(methodUrl).Result;
                    return response.Content.ReadAsStringAsync().Result;                    
                }
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}
