using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AfkNotifier;

internal sealed class ProcessLogger
{
    private readonly TimeSpan logInterval;
    private DateTime lastLog = DateTime.MinValue;

    public ProcessLogger(TimeSpan logInterval)
    {
        this.logInterval = logInterval;
    }

    public void LogPeriodically()
    {
        if (DateTime.Now - lastLog < logInterval)
            return;

        lastLog = DateTime.Now;

        var processes = Process.GetProcesses()
            .OrderBy(p => p.ProcessName)
            .ToList();

        using StreamWriter writer = LogService.CreateProcessLogWriter();

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
}