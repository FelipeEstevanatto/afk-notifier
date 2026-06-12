using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DotNetEnv;
using System.Net;
using System.Net.Mail;

class Program
{
    private static readonly TimeSpan InactivityLimit = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessLogInterval = TimeSpan.FromMinutes(1);

    private static bool wasAfk = false;
    private static DateTime? afkStart = null;
    private static DateTime lastProcessLog = DateTime.MinValue;

    private const string LogsFolder = "logs";
    private const string AfkLogFile = "logs/afk-log.txt";
    private const string ProcessLogFile = "logs/processos-log.txt";

    static void Main()
    {
        Env.Load();

        Directory.CreateDirectory(LogsFolder);

        Console.WriteLine("AFK Notifier iniciado.");
        Console.WriteLine($"Limite de inatividade: {InactivityLimit.TotalSeconds} segundos.");
        Console.WriteLine("Pressione Ctrl + C para encerrar.");

        File.AppendAllText(AfkLogFile, $"\n=== Sessão iniciada em {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");

        while (true)
        {
            TimeSpan inactiveTime = GetInactivityTime();

            Console.Clear();
            Console.WriteLine("AFK Notifier em execução");
            Console.WriteLine($"Tempo inativo atual: {inactiveTime.TotalSeconds:F0} segundos");
            Console.WriteLine($"Limite configurado: {InactivityLimit.TotalSeconds:F0} segundos");

            CheckInactivity(inactiveTime);
            LogProcessesPeriodically();

            Thread.Sleep(CheckInterval);
        }
    }

    private static string GetRequiredVariable(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"A variável de ambiente '{name}' não foi configurada."
            );
        }

        return value;
    }

    private static void CheckInactivity(TimeSpan inactiveTime)
    {
        if (inactiveTime >= InactivityLimit && !wasAfk)
        {
            wasAfk = true;
            afkStart = DateTime.Now.Subtract(inactiveTime);

            string message = $"Usuário ficou inativo desde {afkStart:yyyy-MM-dd HH:mm:ss}.";
            Console.Beep();
            Console.WriteLine(message);

            File.AppendAllText(AfkLogFile,
                $"[INÍCIO AFK] {afkStart:yyyy-MM-dd HH:mm:ss} | Tempo detectado: {inactiveTime.TotalSeconds:F0}s\n");

            // Future step:
            // SendEmail("Alerta AFK", message);
        }

        if (inactiveTime < InactivityLimit && wasAfk)
        {
            DateTime afkEnd = DateTime.Now;
            TimeSpan duration = afkEnd - afkStart!.Value;

            File.AppendAllText(AfkLogFile,
                $"[FIM AFK] {afkEnd:yyyy-MM-dd HH:mm:ss} | Duração aproximada: {duration.TotalSeconds:F0}s\n");

            wasAfk = false;
            afkStart = null;
        }
    }

    private static void LogProcessesPeriodically()
    {
        if (DateTime.Now - lastProcessLog < ProcessLogInterval)
            return;

        lastProcessLog = DateTime.Now;

        var processes = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .ToList();

        using StreamWriter writer = new StreamWriter(ProcessLogFile, append: true);

        writer.WriteLine($"\n=== Processos abertos em {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

        foreach (var process in processes)
        {
            try
            {
                string name = process.ProcessName;
                DateTime start = process.StartTime;

                writer.WriteLine($"{name} | PID: {process.Id} | Aberto desde: {start:yyyy-MM-dd HH:mm:ss}");
            }
            catch
            {
                // Some system processes do not allow access to StartTime.
                writer.WriteLine($"{process.ProcessName} | PID: {process.Id} | StartTime indisponível");
            }
        }
    }

    static void SendEmail(string subject, string body)
    {
        string smtpHost = GetRequiredVariable("AFK_SMTP_HOST");
        int smtpPort = int.Parse(GetRequiredVariable("AFK_SMTP_PORT"));
        string smtpUser = GetRequiredVariable("AFK_SMTP_USER");
        string smtpPass = GetRequiredVariable("AFK_SMTP_PASS");
        string recipient = GetRequiredVariable("AFK_EMAIL_TO");

        using SmtpClient smtp = new SmtpClient(smtpHost, smtpPort);
        smtp.EnableSsl = true;
        smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);

        using MailMessage message = new MailMessage();
        message.From = new MailAddress(smtpUser);
        message.To.Add(recipient);
        message.Subject = subject;
        message.Body = body;

        smtp.Send(message);
    }

    private static TimeSpan GetInactivityTime()
    {
        LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (!GetLastInputInfo(ref lastInputInfo))
            return TimeSpan.Zero;

        uint currentTick = GetTickCount();
        uint inactiveTimeMs = currentTick - lastInputInfo.dwTime;

        return TimeSpan.FromMilliseconds(inactiveTimeMs);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();
}