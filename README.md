# afk-notifier

## projeto 2 - Sistema de Notificação de Inatividade (AFK Notifier)


Ideia técnica do projeto

Você vai usar:

user32.dll com GetLastInputInfo para saber há quanto tempo o usuário não usa teclado/mouse.
System.Diagnostics.Process para listar programas abertos.
StreamWriter ou File.AppendAllText para criar logs.
MessageBox, Console.Beep ou texto no console para aviso.
SMTP para enviar e-mail ao próprio usuário, como etapa final.

Futura estrutura de arquivos:
AfkNotifier/
 ├─ Program.cs
 ├─ NativeMethods.cs
 ├─ IdleMonitor.cs
 ├─ ProcessLogger.cs
 ├─ EmailNotifier.cs
 ├─ LogService.cs
 ├─ logs/
 │   ├─ afk-log.txt
 │   └─ processos-log.txt

## Configuração do ambiente
dotnet add package DotNetEnv
copy .env.example .env