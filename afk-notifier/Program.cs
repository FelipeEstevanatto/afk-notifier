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
    private static readonly TimeSpan LimiteInatividade = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IntervaloVerificacao = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan IntervaloLogProcessos = TimeSpan.FromMinutes(1);

    private static bool estavaAfk = false;
    private static DateTime? inicioAfk = null;
    private static DateTime ultimoLogProcessos = DateTime.MinValue;

    private const string PastaLogs = "logs";
    private const string ArquivoAfkLog = "logs/afk-log.txt";
    private const string ArquivoProcessosLog = "logs/processos-log.txt";

    static void Main()
    {
        Env.Load();

        Directory.CreateDirectory(PastaLogs);

        Console.WriteLine("AFK Notifier iniciado.");
        Console.WriteLine($"Limite de inatividade: {LimiteInatividade.TotalSeconds} segundos.");
        Console.WriteLine("Pressione Ctrl + C para encerrar.");

        File.AppendAllText(ArquivoAfkLog, $"\n=== Sessão iniciada em {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");

        while (true)
        {
            TimeSpan tempoInativo = ObterTempoInatividade();

            Console.Clear();
            Console.WriteLine("AFK Notifier em execução");
            Console.WriteLine($"Tempo inativo atual: {tempoInativo.TotalSeconds:F0} segundos");
            Console.WriteLine($"Limite configurado: {LimiteInatividade.TotalSeconds:F0} segundos");

            VerificarInatividade(tempoInativo);
            RegistrarProcessosPeriodicamente();

            Thread.Sleep(IntervaloVerificacao);
        }
    }

    private static string ObterVariavelObrigatoria(string nome)
    {
        string? valor = Environment.GetEnvironmentVariable(nome);

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                $"A variável de ambiente '{nome}' não foi configurada."
            );
        }

        return valor;
    }

    private static void VerificarInatividade(TimeSpan tempoInativo)
    {
        if (tempoInativo >= LimiteInatividade && !estavaAfk)
        {
            estavaAfk = true;
            inicioAfk = DateTime.Now.Subtract(tempoInativo);

            string mensagem = $"Usuário ficou inativo desde {inicioAfk:yyyy-MM-dd HH:mm:ss}.";
            Console.Beep();
            Console.WriteLine(mensagem);

            File.AppendAllText(ArquivoAfkLog,
                $"[INÍCIO AFK] {inicioAfk:yyyy-MM-dd HH:mm:ss} | Tempo detectado: {tempoInativo.TotalSeconds:F0}s\n");

            // Etapa futura:
            // EnviarEmail("Alerta AFK", mensagem);
        }

        if (tempoInativo < LimiteInatividade && estavaAfk)
        {
            DateTime fimAfk = DateTime.Now;
            TimeSpan duracao = fimAfk - inicioAfk!.Value;

            File.AppendAllText(ArquivoAfkLog,
                $"[FIM AFK] {fimAfk:yyyy-MM-dd HH:mm:ss} | Duração aproximada: {duracao.TotalSeconds:F0}s\n");

            estavaAfk = false;
            inicioAfk = null;
        }
    }

    private static void RegistrarProcessosPeriodicamente()
    {
        if (DateTime.Now - ultimoLogProcessos < IntervaloLogProcessos)
            return;

        ultimoLogProcessos = DateTime.Now;

        var processos = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .ToList();

        using StreamWriter writer = new StreamWriter(ArquivoProcessosLog, append: true);

        writer.WriteLine($"\n=== Processos abertos em {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

        foreach (var processo in processos)
        {
            try
            {
                string nome = processo.ProcessName;
                DateTime inicio = processo.StartTime;

                writer.WriteLine($"{nome} | PID: {processo.Id} | Aberto desde: {inicio:yyyy-MM-dd HH:mm:ss}");
            }
            catch
            {
                // Alguns processos do sistema não permitem acesso ao StartTime.
                writer.WriteLine($"{processo.ProcessName} | PID: {processo.Id} | StartTime indisponível");
            }
        }
    }

    static void EnviarEmail(string assunto, string corpo)
    {
        string smtpHost = ObterVariavelObrigatoria("AFK_SMTP_HOST");
        int smtpPort = int.Parse(ObterVariavelObrigatoria("AFK_SMTP_PORT"));
        string smtpUser = ObterVariavelObrigatoria("AFK_SMTP_USER");
        string smtpPass = ObterVariavelObrigatoria("AFK_SMTP_PASS");
        string destinatario = ObterVariavelObrigatoria("AFK_EMAIL_TO");

        using SmtpClient smtp = new SmtpClient(smtpHost, smtpPort);
        smtp.EnableSsl = true;
        smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);

        using MailMessage mensagem = new MailMessage();
        mensagem.From = new MailAddress(smtpUser);
        mensagem.To.Add(destinatario);
        mensagem.Subject = assunto;
        mensagem.Body = corpo;

        smtp.Send(mensagem);
    }

    private static TimeSpan ObterTempoInatividade()
    {
        LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (!GetLastInputInfo(ref lastInputInfo))
            return TimeSpan.Zero;

        uint tickAtual = GetTickCount();
        uint tempoInativoMs = tickAtual - lastInputInfo.dwTime;

        return TimeSpan.FromMilliseconds(tempoInativoMs);
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