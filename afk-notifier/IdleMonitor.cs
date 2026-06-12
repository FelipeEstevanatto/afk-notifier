using System;
using System.Runtime.InteropServices;

namespace AfkNotifier;

internal sealed class IdleMonitor
{
    public TimeSpan GetInactivityTime()
    {
        NativeMethods.LASTINPUTINFO lastInputInfo = new NativeMethods.LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (!NativeMethods.GetLastInputInfo(ref lastInputInfo))
            return TimeSpan.Zero;

        uint currentTick = NativeMethods.GetTickCount();
        uint inactiveTimeMs = currentTick - lastInputInfo.dwTime;

        return TimeSpan.FromMilliseconds(inactiveTimeMs);
    }
}