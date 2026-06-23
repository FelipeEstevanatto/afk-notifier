using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace AfkNotifier
{
    /// <summary>
    /// Responsável pelo aviso local ao utilizador (som + janela na tela).
    /// A janela é exibida sempre no topo (TopMost) e forçada para o primeiro
    /// plano, para não ficar apenas piscando na barra de tarefas.
    /// </summary>
    internal sealed class AlertService
    {
        private readonly LogService _log;

        public AlertService(LogService log)
        {
            _log = log;
        }

        public void ShowAfkAlert(TimeSpan idleDuration)
        {
            string duration = FormatDuration(idleDuration);

            // 1. Som de aviso (user32.dll), não bloqueia
            try
            {
                NativeMethods.MessageBeep(NativeMethods.MB_BEEP_WARNING);
            }
            catch (Exception ex)
            {
                _log.Warn($"Falha ao emitir som de aviso: {ex.Message}");
            }

            // 2. Janela de aviso numa thread STA própria (não bloqueia o monitor)
            var thread = new Thread(() => RunNotificationWindow(duration))
            {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            _log.Info("Aviso local exibido (som + janela TopMost focada).");
        }

        private void RunNotificationWindow(string duration)
        {
            try
            {
                using var form = BuildForm(duration);
                Application.Run(form);
            }
            catch (Exception ex)
            {
                _log.Warn($"Falha ao exibir janela de aviso: {ex.Message}");
            }
        }

        private static Form BuildForm(string duration)
        {
            var form = new Form
            {
                Text = "AFK Notifier — Ausência detectada",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                Width = 420,
                Height = 200,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = true,
                TopMost = true,
                BackColor = Color.FromArgb(22, 33, 62)
            };

            var title = new Label
            {
                Text = "⚠  Inatividade detectada",
                ForeColor = Color.FromArgb(233, 69, 96),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var message = new Label
            {
                Text = $"Você está ausente há {duration}.\nUm e-mail de notificação foi disparado.",
                ForeColor = Color.FromArgb(220, 224, 235),
                Font = new Font("Segoe UI", 10F),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(233, 69, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += (_, _) => form.Close();

            form.Controls.Add(message);
            form.Controls.Add(title);
            form.Controls.Add(okButton);
            form.AcceptButton = okButton;

            // Fecha sozinho após 30s, caso o utilizador continue ausente
            var autoClose = new System.Windows.Forms.Timer { Interval = 30_000 };
            autoClose.Tick += (_, _) => { autoClose.Stop(); form.Close(); };
            form.Shown += (_, _) =>
            {
                autoClose.Start();
                form.Activate();
                NativeMethods.ForceForeground(form.Handle);
            };
            form.FormClosed += (_, _) => autoClose.Dispose();

            return form;
        }

        private static string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds:D2}s";
            return $"{ts.Seconds}s";
        }
    }
}
