using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AfkNotifier
{
  
    internal class ProcessLogger
    {
        private readonly LogService _log;

        // ── P/Invoke para janela em primeiro plano ────────────────────────────
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private readonly Dictionary<int, (long cpu, DateTime stamp)> _prevCpu
            = new Dictionary<int, (long, DateTime)>();

        public ProcessLogger(LogService log)
        {
            _log = log;
        }

        public SessionContext CaptureContext(int topN = 10)
        {
            var ctx = new SessionContext();

            // 1. Janela em primeiro plano
            ctx = CaptureForegrondWindow(ctx);

            // 2. Lista de processos
            ctx.TopProcesses = CaptureTopProcesses(topN);

            _log.Info($"Contexto capturado: janela='{ctx.ForegroundWindowTitle}' " +
                      $"processo={ctx.ForegroundProcessName} " +
                      $"total_procs={ctx.TopProcesses.Length}");

            return ctx;
        }

        private SessionContext CaptureForegrondWindow(SessionContext ctx)
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return ctx;

                // Título da janela
                var titleBuf = new StringBuilder(512);
                GetWindowText(hwnd, titleBuf, titleBuf.Capacity);
                ctx.ForegroundWindowTitle = titleBuf.ToString().Trim();

                // PID → processo
                GetWindowThreadProcessId(hwnd, out uint pid);
                using var proc = Process.GetProcessById((int)pid);

                ctx.ForegroundProcessName = proc.ProcessName;
                ctx.ForegroundWindowTitle = string.IsNullOrEmpty(ctx.ForegroundWindowTitle)
                    ? proc.MainWindowTitle
                    : ctx.ForegroundWindowTitle;

                ctx.ForegroundExecutablePath = SafeGetExecutablePath(proc);
                ctx.ForegroundProcessDescription = GetFileDescription(ctx.ForegroundExecutablePath);
            }
            catch (Exception ex)
            {
                _log.Warn($"Não foi possível capturar a janela em foco: {ex.Message}");
            }

            return ctx;
        }

        private ProcessInfo[] CaptureTopProcesses(int topN)
        {
            var results = new List<ProcessInfo>();

            // Snapshot de CPU antes (segunda chamada calcula delta)
            var allProcs = Process.GetProcesses();

            foreach (var proc in allProcs)
            {
                try
                {
                    long   cpuTicks   = proc.TotalProcessorTime.Ticks;
                    double memMb      = proc.WorkingSet64 / 1_048_576.0;
                    double cpuPercent = 0;

                    // Calcula delta de CPU em relação à medição anterior
                    if (_prevCpu.TryGetValue(proc.Id, out var prev))
                    {
                        double elapsed = (DateTime.UtcNow - prev.stamp).TotalSeconds;
                        if (elapsed > 0)
                        {
                            double deltaTicks = cpuTicks - prev.cpu;
                            // Normaliza para porcentagem por núcleo
                            int cores = Environment.ProcessorCount;
                            cpuPercent = deltaTicks / (TimeSpan.TicksPerSecond * elapsed * cores) * 100.0;
                            cpuPercent = Math.Max(0, Math.Min(100, cpuPercent));
                        }
                    }

                    _prevCpu[proc.Id] = (cpuTicks, DateTime.UtcNow);

                    string exePath   = SafeGetExecutablePath(proc);
                    string desc      = GetFileDescription(exePath);
                    string iconLabel = ResolveProcessLabel(proc.ProcessName, desc);

                    results.Add(new ProcessInfo
                    {
                        Name        = proc.ProcessName,
                        Description = iconLabel,
                        MemoryMb    = memMb,
                        CpuPercent  = cpuPercent,
                        StartTime   = SafeGetStartTime(proc)
                    });
                }
                catch { /* Processos protegidos (System, smss, etc.) — ignora */ }
                finally
                {
                    proc.Dispose();
                }
            }

            // Ordena por RAM decrescente e retorna os top N
            return results
                .OrderByDescending(p => p.MemoryMb)
                .Take(topN)
                .ToArray();
        }

        
        private static string SafeGetExecutablePath(Process proc)
        {
            try { return proc.MainModule?.FileName ?? ""; }
            catch { return ""; }
        }

        private static DateTime? SafeGetStartTime(Process proc)
        {
            try { return proc.StartTime; }
            catch { return null; } // processos protegidos não expõem StartTime
        }

        /// <summary>
        /// Grava num ficheiro de log dedicado quais programas estão abertos
        /// e desde quando (start time + tempo de atividade).
        /// </summary>
        public void LogOpenProcesses(ProcessInfo[] processes)
        {
            const string ProcLogFile = "logs/processos-log.txt";

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"===== Captura em {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");
                sb.AppendLine($"{"Processo",-28}{"Aberto desde",-22}{"Há",-14}{"RAM (MB)",10}");
                sb.AppendLine(new string('-', 74));

                foreach (var p in processes)
                {
                    string since   = p.StartTime.HasValue
                        ? p.StartTime.Value.ToString("dd/MM/yyyy HH:mm:ss")
                        : "n/d";
                    string uptime  = p.StartTime.HasValue
                        ? FormatUptime(DateTime.Now - p.StartTime.Value)
                        : "n/d";

                    sb.AppendLine($"{Trunc(p.Name, 27),-28}{since,-22}{uptime,-14}{p.MemoryMb,10:F0}");
                }

                sb.AppendLine();
                File.AppendAllText(ProcLogFile, sb.ToString());

                _log.Info($"Log de processos atualizado ({processes.Length} processos).");
            }
            catch (Exception ex)
            {
                _log.Error($"Falha ao gravar log de processos: {ex.Message}");
            }
        }

        private static string FormatUptime(TimeSpan ts)
        {
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours:D2}h";
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds:D2}s";
            return $"{ts.Seconds}s";
        }

        private static string Trunc(string s, int max) =>
            s != null && s.Length > max ? s.Substring(0, max) : (s ?? "");

       
        private static string GetFileDescription(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return "";

            try
            {
                var vi = FileVersionInfo.GetVersionInfo(exePath);
                return vi.FileDescription ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string ResolveProcessLabel(string processName, string description)
        {
            if (!string.IsNullOrWhiteSpace(description))
                return description;

           
            return processName.ToLowerInvariant() switch
            {
                "chrome"            => "Google Chrome",
                "firefox"           => "Mozilla Firefox",
                "msedge"            => "Microsoft Edge",
                "code"              => "Visual Studio Code",
                "devenv"            => "Visual Studio",
                "explorer"          => "Windows Explorer",
                "teams"             => "Microsoft Teams",
                "slack"             => "Slack",
                "discord"           => "Discord",
                "zoom"              => "Zoom",
                "outlook"           => "Microsoft Outlook",
                "winword"           => "Microsoft Word",
                "excel"             => "Microsoft Excel",
                "powerpnt"          => "Microsoft PowerPoint",
                "notepad"           => "Bloco de Notas",
                "notepad++"         => "Notepad++",
                "rider64"           => "JetBrains Rider",
                "idea64"            => "JetBrains IntelliJ IDEA",
                "datagrip64"        => "JetBrains DataGrip",
                "webstorm64"        => "JetBrains WebStorm",
                "pycharm64"         => "JetBrains PyCharm",
                "windowsterminal"   => "Windows Terminal",
                "powershell"        => "PowerShell",
                "cmd"               => "Prompt de Comando",
                "wt"                => "Windows Terminal",
                "spotify"           => "Spotify",
                "postman"           => "Postman",
                "docker desktop"    => "Docker Desktop",
                "vpnui"             => "VPN Client",
                _                   => processName   // fallback: nome bruto do processo
            };
        }
    }


    internal class SessionContext
    {
        public string ForegroundProcessName        { get; set; } = "";
        public string ForegroundWindowTitle        { get; set; } = "";
        public string ForegroundExecutablePath     { get; set; } = "";
        public string ForegroundProcessDescription { get; set; } = "";
        public ProcessInfo[] TopProcesses          { get; set; } = Array.Empty<ProcessInfo>();
    }
}
