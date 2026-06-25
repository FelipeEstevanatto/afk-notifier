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

## Entregáveis do projeto
Cada grupo deverá entregar:
- a) Projeto de código-fonte, contendo todos os arquivos necessários para compilação e execução
(solução/projeto do Visual Studio ou projeto do NetBeans, conforme o caso).
- b) Relatório técnico (em PDF ou formato indicado pelo professor), contendo, no mínimo:
• identificação do grupo (nomes dos integrantes, RA, turma);
• descrição do projeto escolhido (1, 2 ou 3) e objetivo da aplicação;
• breve explicação dos recursos de sistema utilizados (ex.: chamadas a DLLs, bibliotecas de
captura de tela, uso de SMTP, etc.);
• descrição da arquitetura geral do código (principais classes, funções e responsabilidades);
• trechos de código comentados que ilustrem o uso das funções de sistema e/ou envio de
e-mails;
• descrição de como o programa foi testado (cenários de teste, exemplos de execução);
• considerações finais, incluindo possíveis melhorias futuras.
O projeto poderá ser desenvolvido integralmente em sala de aula, com organização em grupos, e será
avaliado de acordo com critérios como:
• correção do funcionamento básico;
• uso adequado das APIs/bibliotecas propostas;
• clareza e organização do código;
• qualidade e clareza do relatório técnico.