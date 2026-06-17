using System;
using System.Runtime.InteropServices;

namespace AfkNotifier
{
    internal sealed class IdleMonitor
    {
        private readonly NativeMethods _nativeMethods;
        private readonly LogService _log;

        public IdleMonitor(NativeMethods nativeMethods, LogService log)
        {
            _nativeMethods = nativeMethods;
            _log = log;
        }

        public TimeSpan GetInactivityTime()
        {
            NativeMethods.LASTINPUTINFO lastInputInfo = new NativeMethods.LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

            if (!NativeMethods.GetLastInputInfo(ref lastInputInfo))
            {
                _log.Warn("Falha ao obter GetLastInputInfo da API do Windows.");
                return TimeSpan.Zero;
            }

            uint currentTick = NativeMethods.GetTickCount();
            uint inactiveTimeMs = currentTick - lastInputInfo.dwTime;

            return TimeSpan.FromMilliseconds(inactiveTimeMs);
        }
    }
}