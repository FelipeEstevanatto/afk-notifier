using System;

namespace AfkNotifier;

internal sealed class AfkStateTracker
{
    private readonly TimeSpan inactivityLimit;

    private bool wasAfk = false;
    private DateTime? afkStart = null;

    public AfkStateTracker(TimeSpan inactivityLimit)
    {
        this.inactivityLimit = inactivityLimit;
    }

    public void Check(TimeSpan inactiveTime)
    {
        if (inactiveTime >= inactivityLimit && !wasAfk)
        {
            wasAfk = true;
            afkStart = DateTime.Now.Subtract(inactiveTime);

            string message = $"Usuário ficou inativo desde {afkStart:yyyy-MM-dd HH:mm:ss}.";
            Console.Beep();
            Console.WriteLine(message);

            LogService.AppendAfkLog(
                $"[INÍCIO AFK] {afkStart:yyyy-MM-dd HH:mm:ss} | Tempo detectado: {inactiveTime.TotalSeconds:F0}s\n"
            );

            // Future step:
            // EmailNotifier.Send("Alerta AFK", message);
        }

        if (inactiveTime < inactivityLimit && wasAfk)
        {
            DateTime afkEnd = DateTime.Now;
            TimeSpan duration = afkEnd - afkStart!.Value;

            LogService.AppendAfkLog(
                $"[FIM AFK] {afkEnd:yyyy-MM-dd HH:mm:ss} | Duração aproximada: {duration.TotalSeconds:F0}s\n"
            );

            wasAfk = false;
            afkStart = null;
        }
    }
}