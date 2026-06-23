using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AfkNotifier
{
    internal class EmailNotifier
    {
        private readonly LogService _log;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromAddress;
        private readonly string _toAddress;

        public EmailNotifier(LogService log)
        {
            _log = log;

            _smtpHost = Env("AFK_SMTP_HOST", "smtp.gmail.com");
            _smtpPort = int.Parse(Env("AFK_SMTP_PORT", "587"));
            _smtpUser = Env("AFK_SMTP_USER", "");
            _smtpPass = Env("AFK_SMTP_PASS", "");
            _fromAddress = Env("AFK_FROM_ADDRESS", _smtpUser);
            _toAddress = Env("AFK_EMAIL_TO", "");
        }

        public void SendAfkAlert(AfkSnapshot snapshot)
        {
            string subject = $"[AFK-Notifier] Ausência detectada — {snapshot.MachineName}";
            string body = BuildHtmlBody(snapshot);
            Send(subject, body);
        }

        public void SendReturnAlert(AfkSnapshot snapshot)
        {
            string subject = $"[AFK-Notifier] Usuário retornou — {snapshot.MachineName}";
            string body = BuildReturnHtmlBody(snapshot);
            Send(subject, body);
        }

        private void Send(string subject, string htmlBody)
        {
            if (string.IsNullOrEmpty(_toAddress))
            {
                _log.Warn("TO_ADDRESS não configurado — e-mail não enviado.");
                return;
            }

            // O envio SMTP roda numa thread separada (fire-and-forget) para não
            // bloquear o loop de monitorização de inatividade durante a conexão.
            _ = Task.Run(() => SendInternal(subject, htmlBody));
        }

        private void SendInternal(string subject, string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 10_000
                };

                using var message = new MailMessage(_fromAddress, _toAddress, subject, htmlBody)
                {
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8
                };

                client.Send(message);
                _log.Info($"E-mail enviado para {_toAddress}: {subject}");
            }
            catch (Exception ex)
            {
                _log.Error($"Falha ao enviar e-mail: {ex.Message}");
            }
        }

        private static string BuildHtmlBody(AfkSnapshot snap)
        {
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "afk-alert.html");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template não encontrado: {templatePath}");
            }

            string html = File.ReadAllText(templatePath);
            string duration = FormatDuration(snap.IdleDuration);
            string rows = BuildProcessRows(snap.TopProcesses);

            return html
                .Replace("{{DetectedAt}}", snap.DetectedAt.ToString("dd/MM/yyyy 'às' HH:mm:ss"))
                .Replace("{{Duration}}", duration)
                .Replace("{{MachineName}}", Truncate(snap.MachineName, 12))
                .Replace("{{UserName}}", Truncate(snap.UserName, 12))
                .Replace("{{LastForegroundProcess}}", snap.LastForegroundProcess)
                .Replace("{{LastWindowTitle}}", snap.LastWindowTitle)
                .Replace("{{LastExecutablePath}}", snap.LastExecutablePath)
                .Replace("{{TopCount}}", snap.TopProcesses.Length.ToString())
                .Replace("{{TopProcesses}}", rows)
                .Replace("{{Year}}", snap.DetectedAt.ToString("yyyy"));
        }

        private static string BuildReturnHtmlBody(AfkSnapshot snap)
        {
            string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "afk-return.html");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template não encontrado: {templatePath}");
            }

            string html = File.ReadAllText(templatePath);
            string absent = FormatDuration(snap.IdleDuration);

            return html
                .Replace("{{ReturnedAt}}", snap.ReturnedAt.ToString("dd/MM/yyyy 'às' HH:mm:ss"))
                .Replace("{{UserName}}", snap.UserName)
                .Replace("{{MachineName}}", snap.MachineName)
                .Replace("{{AbsentDuration}}", absent)
                .Replace("{{DetectedAtHour}}", snap.DetectedAt.ToString("HH:mm"))
                .Replace("{{ReturnedAtHour}}", snap.ReturnedAt.ToString("HH:mm"))
                .Replace("{{Year}}", snap.DetectedAt.ToString("yyyy"));
        }

        private static string BuildProcessRows(ProcessInfo[] procs)
        {
            if (procs == null || procs.Length == 0)
                return "<tr><td colspan=\"6\" style=\"color:#9ca3af;\">Nenhum processo capturado.</td></tr>";

            var sb = new StringBuilder();
            for (int i = 0; i < procs.Length; i++)
            {
                var p = procs[i];
                string since = p.StartTime.HasValue
                    ? p.StartTime.Value.ToString("dd/MM HH:mm")
                    : "n/d";

                sb.AppendLine($@"<tr>
  <td style=""color:#9ca3af;"">{i + 1}</td>
  <td><span style=""display:inline-block;width:8px;height:8px;border-radius:50%;background:#0f62fe;margin-right:8px;""></span>{WebEncode(p.Name)}</td>
  <td style=""color:#6b7280;"">{WebEncode(p.Description)}</td>
  <td style=""color:#6b7280;"">{since}</td>
  <td style=""text-align:right;"">{p.CpuPercent:F1}</td>
  <td style=""text-align:right;"">{p.MemoryMb:F0}</td>
</tr>");
            }
            return sb.ToString();
        }

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds:D2}s";
            return $"{ts.Seconds}s";
        }

        private static string Truncate(string s, int max) =>
            s != null && s.Length > max ? s.Substring(0, max) + "…" : (s ?? "");

        private static string WebEncode(string s) =>
            System.Net.WebUtility.HtmlEncode(s ?? "");

        private static string Env(string key, string fallback) =>
            Environment.GetEnvironmentVariable(key) ?? fallback;
    }

    internal class AfkSnapshot
    {
        public string MachineName { get; set; } = Environment.MachineName;
        public string UserName { get; set; } = Environment.UserName;
        public DateTime DetectedAt { get; set; } = DateTime.Now;
        public DateTime ReturnedAt { get; set; }
        public TimeSpan IdleDuration { get; set; }
        public string LastForegroundProcess { get; set; } = "";
        public string LastWindowTitle { get; set; } = "";
        public string LastExecutablePath { get; set; } = "";
        public ProcessInfo[] TopProcesses { get; set; } = Array.Empty<ProcessInfo>();
    }

    internal class ProcessInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double CpuPercent { get; set; }
        public double MemoryMb { get; set; }

        /// <summary>Momento em que o processo foi iniciado (desde quando está aberto).</summary>
        public DateTime? StartTime { get; set; }
    }
}