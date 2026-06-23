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
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            // Inicia o log primeiro para registar o resultado do carregamento do .env
            var logService = new LogService();

            // Carrega as variáveis do ficheiro .env (procura em vários locais)
            string? loadedFrom = EnvLoader.Load(".env");
            if (loadedFrom != null)
                logService.Info($".env carregado de: {loadedFrom}");
            else
                logService.Warn(".env NÃO encontrado — a usar valores padrão. Verifique se o ficheiro está na pasta de saída.");

            // Inicia os serviços
            var nativeMethods = new NativeMethods();
            var idleMonitor = new IdleMonitor(nativeMethods, logService);
            var processLogger = new ProcessLogger(logService);
            var emailNotifier = new EmailNotifier(logService);
            var alertService = new AlertService(logService);
            var stateTracker = new AfkStateTracker(idleMonitor, emailNotifier, processLogger, alertService, logService);

            stateTracker.Start();
            
            Thread.Sleep(Timeout.Infinite);
        }
    }

    internal static class EnvLoader
    {
        /// <summary>
        /// Procura o ficheiro .env em vários locais (pasta de saída, diretório
        /// de trabalho e diretórios-pai) e carrega as variáveis. Retorna o
        /// caminho efetivamente utilizado, ou null se não encontrar.
        /// </summary>
        public static string? Load(string fileName)
        {
            string? envPath = ResolvePath(fileName);
            if (envPath == null) return null; // Não encontrado

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

            return envPath;
        }

        private static string? ResolvePath(string fileName)
        {
            var candidates = new System.Collections.Generic.List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName)
            };

            // Sobe nos diretórios-pai (útil ao executar via 'dotnet run',
            // onde o .env fica na raiz do projeto, não na pasta bin)
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
            {
                candidates.Add(Path.Combine(dir.FullName, fileName));
            }

            foreach (string path in candidates)
            {
                if (File.Exists(path)) return path;
            }

            return null;
        }
    }
}