using System;
using System.IO;
using System.Threading;

namespace AfkNotifier
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Carrega as variáveis do ficheiro .env
            EnvLoader.Load(".env");

            // Inicia os serviços
            var logService = new LogService();
            var nativeMethods = new NativeMethods();
            var idleMonitor = new IdleMonitor(nativeMethods, logService);
            var processLogger = new ProcessLogger(logService);
            var emailNotifier = new EmailNotifier(logService);
            var stateTracker = new AfkStateTracker(idleMonitor, emailNotifier, processLogger, logService);

            stateTracker.Start();
            
            Thread.Sleep(Timeout.Infinite);
        }
    }

    internal static class EnvLoader
    {
        public static void Load(string fileName)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = Path.Combine(basePath, fileName);

            if (!File.Exists(envPath)) return; // Se não existir, falha silenciosamente

            foreach (string line in File.ReadAllLines(envPath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                int sep = trimmed.IndexOf('=');
                if (sep < 1) continue;

                string key = trimmed.Substring(0, sep).Trim();
                string value = trimmed.Substring(sep + 1).Trim();

                if (value.Length >= 2 &&
                    ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                     (value.StartsWith("'") && value.EndsWith("'"))))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}