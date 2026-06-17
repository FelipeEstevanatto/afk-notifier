using System;
using System.IO;

namespace AfkNotifier
{
    internal class LogService
    {
        private const string LogsFolder = "logs";
        private const string AppLogFile = "logs/app-log.txt";

        public LogService()
        {
            Directory.CreateDirectory(LogsFolder);
        }

        public void Info(string message) => WriteLog("INFO", message);
        public void Warn(string message) => WriteLog("WARN", message);
        public void Error(string message) => WriteLog("ERROR", message);

        private void WriteLog(string level, string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            // Grava no ficheiro
            File.AppendAllText(AppLogFile, logEntry + Environment.NewLine);

            // Imprime na consola (útil se executar com --show-console)
            Console.WriteLine(logEntry);
        }
    }
}