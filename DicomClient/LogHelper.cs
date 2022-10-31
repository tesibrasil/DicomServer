using System;
using System.IO;

namespace DicomClient
{
    public static class LogHelper
    {
        private static string BasePath = Program.AssemblyLocation + "\\Log";
        public static void Write(string message)
        {
            var Date = DateTime.Now.ToString("yyyyMMdd");
            var path = $"{BasePath}\\{Date}.txt";

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.AppendAllText(path, $"[{DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss")}]   {message}{Environment.NewLine}");
        }
    }
}
