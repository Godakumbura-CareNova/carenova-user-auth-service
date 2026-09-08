using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using UserAuth.Api.Config;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _environment;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger, IWebHostEnvironment environment)
    {
        _settings = settings.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        // In Development, always log the link so the flow is testable without real SMTP
        // credentials configured. Never do this outside Development — it would leak reset
        // links into logs in a real environment.
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("[DEV] Password reset link for {Email}: {ResetLink}", toEmail, resetLink);
        }

        if (string.IsNullOrWhiteSpace(_settings.SmtpServer))
        {
            _logger.LogWarning("Email not sent to {Email} — no SMTP server configured.", toEmail);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = "Reset your CareNova password",
                Body = $"We received a request to reset your CareNova password.\n\n" +
                       $"Click the link below to choose a new password. This link expires in 30 minutes and can only be used once.\n\n" +
                       $"{resetLink}\n\n" +
                       $"If you didn't request this, you can safely ignore this email.",
                IsBodyHtml = false,
            };
            message.To.Add(toEmail);

            // Gmail displays App Passwords with spaces for readability (e.g. "abcd efgh ijkl
            // mnop"), but the real credential is the 16 characters with no spaces — sending it
            // with spaces embedded fails auth with a generic "Authentication Required" error
            // rather than a clear "wrong password" one, so strip them defensively.
            var smtpPassword = (_settings.Password ?? string.Empty).Replace(" ", string.Empty);

            using var client = new SmtpClient(_settings.SmtpServer, _settings.Port);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_settings.Username, smtpPassword);
            client.EnableSsl = true;

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Never let email delivery failure change the caller's response — the controller
            // always returns the same generic confirmation regardless (Scenario 5's requirement
            // not to leak whether an email exists extends to not leaking delivery failures either).
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
        }
    }
}
