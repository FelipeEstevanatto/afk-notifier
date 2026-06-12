using System;
using System.Threading;
using DotNetEnv;

namespace AfkNotifier;

class Program
{
    private static readonly TimeSpan InactivityLimit = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessLogInterval = TimeSpan.FromMinutes(1);

    static void Main()
    {
        Env.Load();

        LogService.EnsureLogDirectory();

        Console.WriteLine("AFK Notifier iniciado.");
        Console.WriteLine($"Limite de inatividade: {InactivityLimit.TotalSeconds} segundos.");
        Console.WriteLine("Pressione Ctrl + C para encerrar.");

        LogService.AppendAfkLog($"\n=== Sessão iniciada em {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");

        IdleMonitor idleMonitor = new IdleMonitor();
        AfkStateTracker afkStateTracker = new AfkStateTracker(InactivityLimit);
        ProcessLogger processLogger = new ProcessLogger(ProcessLogInterval);

        while (true)
        {
            TimeSpan inactiveTime = idleMonitor.GetInactivityTime();

            Console.Clear();
            Console.WriteLine("AFK Notifier em execução");
            Console.WriteLine($"Tempo inativo atual: {inactiveTime.TotalSeconds:F0} segundos");
            Console.WriteLine($"Limite configurado: {InactivityLimit.TotalSeconds:F0} segundos");

            afkStateTracker.Check(inactiveTime);
            processLogger.LogPeriodically();

            Thread.Sleep(CheckInterval);
        }
    }
}