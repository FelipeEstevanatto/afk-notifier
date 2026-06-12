using System;
using System.Net;
using System.Net.Mail;

namespace AfkNotifier;

internal static class EmailNotifier
{
    public static void Send(string subject, string body)
    {
        string smtpHost = GetRequiredVariable("AFK_SMTP_HOST");
        int smtpPort = int.Parse(GetRequiredVariable("AFK_SMTP_PORT"));
        string smtpUser = GetRequiredVariable("AFK_SMTP_USER");
        string smtpPass = GetRequiredVariable("AFK_SMTP_PASS");
        string recipient = GetRequiredVariable("AFK_EMAIL_TO");

        using SmtpClient smtp = new SmtpClient(smtpHost, smtpPort);
        smtp.EnableSsl = true;
        smtp.Credentials = new NetworkCredential(smtpUser, smtpPass);

        using MailMessage message = new MailMessage();
        message.From = new MailAddress(smtpUser);
        message.To.Add(recipient);
        message.Subject = subject;
        message.Body = body;

        smtp.Send(message);
    }

    private static string GetRequiredVariable(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"A variável de ambiente '{name}' não foi configurada."
            );
        }

        return value;
    }
}