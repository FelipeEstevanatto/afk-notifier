using System;
using System.Windows.Forms;

namespace AfkNotifier
{
    /// <summary>
    /// Registra um atalho global (Ctrl+Shift+K) para encerrar a aplicação, que
    /// normalmente roda sem janela visível. Usa uma janela "message-only"
    /// (HWND_MESSAGE) — invisível e sem entrada na barra de tarefas — apenas
    /// para receber a mensagem WM_HOTKEY do Windows.
    ///
    /// Importante: RegisterHotKey NÃO captura digitação — o sistema apenas avisa
    /// quando a combinação específica é pressionada (uso ético, sem keylogging).
    /// </summary>
    internal sealed class HotkeyService : NativeWindow, IDisposable
    {
        private const int HotkeyId = 0xA001;
        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        private readonly LogService _log;
        private bool _registered;

        public HotkeyService(LogService log)
        {
            _log = log;

            // Cria uma janela message-only (sem UI) para receber WM_HOTKEY
            CreateHandle(new CreateParams { Parent = HWND_MESSAGE });

            _registered = NativeMethods.RegisterHotKey(
                Handle,
                HotkeyId,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
                NativeMethods.VK_K);

            if (_registered)
                _log.Info("Atalho de encerramento registado: Ctrl+Shift+K.");
            else
                _log.Warn("Não foi possível registar o atalho Ctrl+Shift+K (talvez já esteja em uso).");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && (int)m.WParam == HotkeyId)
            {
                Shutdown();
                return;
            }

            base.WndProc(ref m);
        }

        private void Shutdown()
        {
            _log.Info("Encerramento solicitado pelo utilizador via Ctrl+Shift+K.");

            // Avisa o utilizador (som + mensagem na tela) que o programa foi fechado
            try { NativeMethods.MessageBeep(NativeMethods.MB_BEEP_WARNING); } catch { }

            NativeMethods.MessageBox(
                IntPtr.Zero,
                "O AFK Notifier foi encerrado.\n\nO monitoramento de inatividade está agora desativado.",
                "AFK Notifier — Programa encerrado",
                NativeMethods.MB_OK
                    | NativeMethods.MB_ICONINFORMATION
                    | NativeMethods.MB_TOPMOST
                    | NativeMethods.MB_SETFOREGROUND);

            Dispose();
            Application.Exit();
        }

        public void Dispose()
        {
            if (_registered)
            {
                NativeMethods.UnregisterHotKey(Handle, HotkeyId);
                _registered = false;
            }

            if (Handle != IntPtr.Zero)
                DestroyHandle();
        }
    }
}
