using Serilog;
using Serilog.Enrichers;
using System.IO;
using System;

namespace DicomServer
{
    public static class LogHelper
    {
        private static string BasePath = Program.AssemblyLocation + "\\DicomLog";

        public static void Verbose(string message, bool debug = false)
        {
            var Date = DateTime.Now.ToString("yyyyMMdd");
            var config = new LoggerConfiguration()
                .Enrich.With<MachineNameEnricher>()
                .Enrich.With<EnvironmentUserNameEnricher>()
                .MinimumLevel.Verbose()
                .WriteTo.Console();

            var logger = config.CreateLogger();
            logger.Verbose(message);

            //if (!debug) Write(message, $"{BasePath}\\Verbose\\{Date}.txt");
            if (!debug) Write("Verbose --> " + message, $"{BasePath}\\{Date}.txt");
        }
        public static void Debug(string message, bool debug = false)
        {
            var Date = DateTime.Now.ToString("yyyyMMdd");
            var config = new LoggerConfiguration()
                .Enrich.With<MachineNameEnricher>()
                .Enrich.With<EnvironmentUserNameEnricher>()
                .MinimumLevel.Debug()
                .WriteTo.Console();

            var logger = config.CreateLogger();
            logger.Debug(message);

            //if (!debug) Write(message, $"{BasePath}\\Debug\\{Date}.txt");
            if (!debug) Write("Debug --> " + message, $"{BasePath}\\{Date}.txt");
        }
        public static void Info(string message, bool debug = false)
        {
            var Date = DateTime.Now.ToString("yyyyMMdd");
            var config = new LoggerConfiguration()
                .Enrich.With<MachineNameEnricher>()
                .Enrich.With<EnvironmentUserNameEnricher>()
                .MinimumLevel.Information()
                .WriteTo.Console();

            var logger = config.CreateLogger();
            logger.Information(message);

            //if (!debug) Write(message, $"{BasePath}\\Info\\{Date}.txt");
            if (!debug) Write("Info --> " + message, $"{BasePath}\\{Date}.txt");
        }
        public static void Warning(string message, bool debug = false)
        {
            var Date = DateTime.Now.ToString("yyyyMMdd");
            var config = new LoggerConfiguration()
               .Enrich.With<MachineNameEnricher>()
               .Enrich.With<EnvironmentUserNameEnricher>()
               .MinimumLevel.Warning()
               .WriteTo.Console();

            var logger = config.CreateLogger();
            logger.Warning(message);

            //if (!debug) Write(message, $"{BasePath}\\Warning\\{Date}.txt");
            if (!debug) Write("Warning --> " + message, $"{BasePath}\\{Date}.txt");
        }
        public static void Error(string message, bool debug = false)
        {
            var Date = DateTime.Now.ToString("yyyyMMdd");
            var config = new LoggerConfiguration()
                .Enrich.With<MachineNameEnricher>()
                .Enrich.With<EnvironmentUserNameEnricher>()
                .MinimumLevel.Error()
                .WriteTo.Console();

            var logger = config.CreateLogger();
            logger.Error(message);

            //if (!debug) Write(message, $"{BasePath}\\Error\\{Date}.txt");
            if (!debug) Write("Error --> " + message, $"{BasePath}\\{Date}.txt");
        }
        public static void Fatal(string message, bool debug = false)
        {
            var Date = DateTime.Now.ToString("yyyyMMdd");
            var config = new LoggerConfiguration()
                .Enrich.With<MachineNameEnricher>()
                .Enrich.With<EnvironmentUserNameEnricher>()
                .MinimumLevel.Fatal()
                .WriteTo.Console();

            var logger = config.CreateLogger();
            logger.Fatal(message);

            //if (!debug) Write(message, $"{BasePath}\\Fatal\\{Date}.txt");
            if (!debug) Write("Fatal --> " + message, $"{BasePath}\\{Date}.txt");
        }

        private static void Write(string message, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.AppendAllText(path, $"[{DateTime.Now.ToString("dd/MM/yyyy - HH:mm:ss")}]   {message}{Environment.NewLine}");
        }
    }
}