# afk-notifier

## Projeto 2 — Sistema de Notificação de Inatividade (AFK Notifier)

Aplicação em **C# (.NET, WinExe)** que monitora o tempo de inatividade do usuário
(período sem uso de teclado/mouse) e, ao ultrapassar um limite configurável:

- emite um **aviso local** (som via `MessageBeep` + janela `TopMost` focada);
- **registra em log** os intervalos de inatividade detectados;
- **registra quais programas estão abertos** e desde quando;
- **envia um e-mail** de notificação ao próprio usuário com um relatório do estado da máquina.

O foco é o uso ético de recursos do sistema operacional para monitoramento do
**próprio** usuário — não há qualquer captura oculta de dados de terceiros.

## Recursos de sistema utilizados

- `user32.dll` — `GetLastInputInfo` (tempo desde a última entrada de teclado/mouse),
  `GetForegroundWindow`/`GetWindowText`/`GetWindowThreadProcessId` (janela ativa),
  `MessageBox`/`MessageBeep` (aviso), `RegisterHotKey` (atalho de encerramento),
  `SetForegroundWindow`/`AttachThreadInput` (forçar foco da janela de aviso).
- `kernel32.dll` — `GetTickCount`, `GlobalMemoryStatusEx` (memória),
  `GetSystemPowerStatus` (bateria/energia).
- `System.Diagnostics.Process` — lista de processos abertos (CPU, RAM, PID, horário de início).
- `System.Net.Mail.SmtpClient` — envio de e-mail por SMTP (Gmail, porta 587, STARTTLS).
- `Microsoft.Win32.Registry` — leitura do modelo do equipamento.

## Estrutura de arquivos

```
afk-notifier/
 ├─ Program.cs           # ponto de entrada + EnvLoader (carrega o .env)
 ├─ NativeMethods.cs     # declarações P/Invoke ([DllImport]) e structs
 ├─ IdleMonitor.cs       # cálculo do tempo de inatividade
 ├─ AfkStateTracker.cs   # laço de verificação e transições ativo/ausente
 ├─ AlertService.cs      # aviso local (som + janela)
 ├─ ProcessLogger.cs     # captura de processos e log de programas abertos
 ├─ EmailNotifier.cs     # envio de e-mail (SMTP) + AfkSnapshot/ProcessInfo
 ├─ SystemInfo.cs        # memória, energia e modelo do equipamento
 ├─ HotkeyService.cs     # atalho global Ctrl+Shift+K para encerrar
 ├─ LogService.cs        # log com nível e timestamp
 ├─ templates/
 │   ├─ afk-alert.html    # e-mail de ausência
 │   └─ afk-return.html   # e-mail de retorno
 └─ logs/                 # gerado em runtime
     ├─ app-log.txt       # eventos gerais (INFO/WARN/ERROR) e intervalos AFK
     └─ processos-log.txt # programas abertos e desde quando
```

## Configuração do ambiente

A configuração é lida de um arquivo `.env` por um `EnvLoader` próprio (sem dependências
externas). Copie o modelo e preencha os valores:

```powershell
copy afk-notifier\.env.example afk-notifier\.env
```

Variáveis disponíveis:

| Variável | Descrição | Padrão |
|---|---|---|
| `AFK_SMTP_HOST` | Servidor SMTP | `smtp.gmail.com` |
| `AFK_SMTP_PORT` | Porta SMTP (587 = STARTTLS) | `587` |
| `AFK_SMTP_USER` | Conta que realiza o envio | — |
| `AFK_SMTP_PASS` | Senha de App do Google (não a senha normal) | — |
| `AFK_EMAIL_TO` | Destinatário das notificações (o próprio usuário) | — |
| `AFK_LIMIT_SECONDS` | Tempo de inatividade que dispara o aviso | `30` |
| `AFK_CHECK_INTERVAL_MS` | Intervalo entre verificações | `1000` |

> A Senha de App é gerada em https://myaccount.google.com/apppasswords e exige
> verificação em duas etapas ativada. O `.env` fica fora do controle de versão
> (`.gitignore`); o repositório inclui apenas o `.env.example`.

## Pré-requisitos

- Windows (a aplicação usa APIs exclusivas do Windows).
- .NET SDK 10.0 ou superior (`net10.0-windows`).
- Visual Studio Community (opcional, para abrir a solução `.slnx`).

## Compilação e execução

Via linha de comando:

```powershell
dotnet run --project afk-notifier
```

Ou abra `afk-notifier.slnx` no Visual Studio e pressione F5.

Para testar rapidamente, defina um limite pequeno no `.env`
(ex.: `AFK_LIMIT_SECONDS=5`) e deixe de interagir com o computador por esse período.
Para encerrar a aplicação (que roda em segundo plano), use o atalho **Ctrl+Shift+K**.

## Entregáveis do projeto

- **a)** Projeto de código-fonte (solução `.slnx` + projeto do Visual Studio).
- **b)** Relatório técnico (PDF) com identificação do grupo, descrição do projeto,
  recursos de sistema, arquitetura, trechos de código comentados, testes e
  considerações finais.
