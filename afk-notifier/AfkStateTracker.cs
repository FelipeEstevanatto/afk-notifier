using System;
using System.Threading;
using System.Threading.Tasks;

namespace AfkNotifier
{
    internal sealed class AfkStateTracker
    {
        private readonly IdleMonitor _idleMonitor;
        private readonly EmailNotifier _emailNotifier;
        private readonly ProcessLogger _processLogger;
        private readonly LogService _log;

        private readonly TimeSpan _inactivityLimit;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

        private bool _wasAfk = false;
        private DateTime? _afkStart = null;
        private AfkSnapshot _lastSnapshot = null;

        public AfkStateTracker(IdleMonitor idleMonitor, EmailNotifier emailNotifier, ProcessLogger processLogger, LogService log)
        {
            _idleMonitor = idleMonitor;
            _emailNotifier = emailNotifier;
            _processLogger = processLogger;
            _log = log;

            // Busca o limite no .env. Se não existir, assume 30 segundos por defeito.
            int limitSeconds = int.Parse(Environment.GetEnvironmentVariable("AFK_LIMIT_SECONDS") ?? "30");
            _inactivityLimit = TimeSpan.FromSeconds(limitSeconds);
        }

        public void Start()
        {
            _log.Info($"Iniciando monitorização AFK. Limite de inatividade: {_inactivityLimit.TotalSeconds} segundos.");

            // Roda o loop de verificação em background
            Task.Run(() =>
            {
                while (true)
                {
                    TimeSpan inactiveTime = _idleMonitor.GetInactivityTime();
                    Check(inactiveTime);
                    Thread.Sleep(_checkInterval);
                }
            });
        }

        private void Check(TimeSpan inactiveTime)
        {
           
            if (inactiveTime >= _inactivityLimit && !_wasAfk)
            {
                _wasAfk = true;
                _afkStart = DateTime.Now.Subtract(inactiveTime);

                _log.Info($"[INÍCIO AFK] Utilizador inativo desde as {_afkStart:HH:mm:ss}");

                var context = _processLogger.CaptureContext(topN: 10);

                _lastSnapshot = new AfkSnapshot
                {
                    DetectedAt = _afkStart.Value,
                    IdleDuration = inactiveTime,
                    LastForegroundProcess = context.ForegroundProcessName,
                    LastWindowTitle = context.ForegroundWindowTitle,
                    LastExecutablePath = context.ForegroundExecutablePath,
                    TopProcesses = context.TopProcesses
                };

                _emailNotifier.SendAfkAlert(_lastSnapshot);
            }

            if (inactiveTime < _inactivityLimit && _wasAfk)
            {
                DateTime afkEnd = DateTime.Now;
                TimeSpan duration = afkEnd - _afkStart!.Value;

                _log.Info($"[FIM AFK] Utilizador regressou. Duração da ausência: {duration.TotalSeconds:F0}s");

                if (_lastSnapshot != null)
                {
                    _lastSnapshot.ReturnedAt = afkEnd;
                    _lastSnapshot.IdleDuration = duration;
                    _emailNotifier.SendReturnAlert(_lastSnapshot);
                    _lastSnapshot = null;
                }

                _wasAfk = false;
                _afkStart = null;
            }
        }
    }
}