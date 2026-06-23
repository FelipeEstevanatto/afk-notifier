using System;
using System.Runtime.InteropServices;

namespace AfkNotifier
{
    internal class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("kernel32.dll")]
        internal static extern uint GetTickCount();

        // ── Aviso visual e sonoro (user32.dll) ────────────────────────────────

        // Tipos de ícone/botões aceites por MessageBox (dwType)
        internal const uint MB_OK = 0x00000000;
        internal const uint MB_ICONWARNING = 0x00000030;
        internal const uint MB_TOPMOST = 0x00040000;
        internal const uint MB_SETFOREGROUND = 0x00010000;

        // Tipos de som aceites por MessageBeep (uType)
        internal const uint MB_BEEP_WARNING = 0x00000030;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MessageBeep(uint uType);

        // ── Forçar a janela de aviso para o primeiro plano ────────────────────
        // Um processo em segundo plano não consegue "roubar" o foco diretamente
        // (Windows só faz piscar a barra de tarefas). O truque AttachThreadInput
        // anexa a fila de entrada da thread em foco à nossa, permitindo o foco.

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        /// <summary>Traz a janela <paramref name="hWnd"/> para o primeiro plano de forma confiável.</summary>
        internal static void ForceForeground(IntPtr hWnd)
        {
            IntPtr foreground = GetForegroundWindow();
            uint foreThread = GetWindowThreadProcessId(foreground, out _);
            uint appThread = GetCurrentThreadId();

            if (foreThread != appThread)
            {
                AttachThreadInput(foreThread, appThread, true);
                SetForegroundWindow(hWnd);
                AttachThreadInput(foreThread, appThread, false);
            }
            else
            {
                SetForegroundWindow(hWnd);
            }
        }
    }
}