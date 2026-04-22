using System.Net;
using System.Net.Mail;
using HealthCareMS.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Infrastructure.Notifications;

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task<DeliveryResult> SendAsync(
        string destination,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return DeliveryResult.Failed("Email destination is empty.");
        }

        var smtpOptions = options.Value;
        if (!smtpOptions.Enabled)
        {
            logger.LogInformation("SMTP disabled; simulated email to {Destination}: {Subject}", destination, subject);
            return DeliveryResult.Sent;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(smtpOptions.FromEmail, smtpOptions.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(destination);

        using var client = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
        {
            EnableSsl = smtpOptions.UseSsl
        };

        if (!string.IsNullOrWhiteSpace(smtpOptions.Username))
        {
            client.Credentials = new NetworkCredential(smtpOptions.Username, smtpOptions.Password);
        }

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            return DeliveryResult.Sent;
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            logger.LogWarning(ex, "SMTP email delivery failed for {Destination}.", destination);
            return DeliveryResult.Failed(ex.Message);
        }
    }
}

