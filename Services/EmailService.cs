using MailKit.Net.Smtp;
using MimeKit;

namespace UserManagementApp.Services;

// NOTE: Sends verification emails asynchronously (fire-and-forget from controller).
// Uses MailKit which wraps SMTP — configure via appsettings.json or environment variables
public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // IMPORTANT: This method is intentionally fire-and-forget (called with _ = SendAsync(...))
    // so registration completes immediately without waiting for the email to send.
    public async Task SendVerificationEmailAsync(string toEmail, string toName, string verificationUrl)
    {
        try
        {
            // NOTA BENE: Read credentials from environment variables first (production),
            // fall back to appsettings.json (development)
            var smtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTPHOST") 
                ?? _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("EMAIL_SMTPPORT") 
                ?? _config["Email:SmtpPort"] ?? "587");
            var smtpUser = Environment.GetEnvironmentVariable("EMAIL_SMTPUSER") 
                ?? _config["Email:SmtpUser"] ?? "";
            var smtpPass = Environment.GetEnvironmentVariable("EMAIL_SMTPPASS") 
                ?? _config["Email:SmtpPass"] ?? "";
            var fromName = Environment.GetEnvironmentVariable("EMAIL_FROMNAME")
                ?? _config["Email:FromName"] ?? "User Management App";

            // NOTE: Skip sending if SMTP credentials not configured (dev mode)
            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                _logger.LogWarning("Email not sent: SMTP credentials not configured. Verification URL: {Url}", verificationUrl);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, smtpUser));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "Verify your email address";

            message.Body = new TextPart("html")
            {
                Text = $"""
                    <p>Hello {toName},</p>
                    <p>Please verify your email address by clicking the link below:</p>
                    <p><a href="{verificationUrl}">Verify Email</a></p>
                    <p>If you did not register, you can ignore this email.</p>
                """
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Verification email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            // NOTA BENE: Email failures are logged but do NOT fail the registration —
            // user is already registered and can still log in; email is best-effort
            _logger.LogError(ex, "Failed to send verification email to {Email}", toEmail);
        }
    }
}
