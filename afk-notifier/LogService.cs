using System.IO;

namespace AfkNotifier;

internal static class LogService
{
    private const string LogsFolder = "logs";
    private const string AfkLogFile = "logs/afk-log.txt";
    private const string ProcessLogFile = "logs/processos-log.txt";

    public static void EnsureLogDirectory()
    {
        Directory.CreateDirectory(LogsFolder);
    }

    public static void AppendAfkLog(string text)
    {
        File.AppendAllText(AfkLogFile, text);
    }

    public static StreamWriter CreateProcessLogWriter()
    {
        return new StreamWriter(ProcessLogFile, append: true);
    }
}