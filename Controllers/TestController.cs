using Microsoft.AspNetCore.Mvc;
using UserManagementApp.Services;

namespace UserManagementApp.Controllers;

/// <summary>
/// IMPORTANT: Test controller for email verification setup.
/// Remove or disable this in production.
/// Only available in Development environment.
/// </summary>
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly ILogger<TestController> _logger;
    private readonly IWebHostEnvironment _env;

    public TestController(EmailService emailService, ILogger<TestController> logger, IWebHostEnvironment env)
    {
        _emailService = emailService;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// NOTA BENE: Send a test email to verify Gmail SMTP is configured correctly.
    /// Only works in Development environment.
    /// 
    /// Usage: GET /api/test/send-test-email?to=your-email@example.com
    /// </summary>
    [HttpGet("send-test-email")]
    public async Task<IActionResult> SendTestEmail([FromQuery] string to)
    {
        // IMPORTANT: Only allow in development
        if (!_env.IsDevelopment())
            return BadRequest("Test endpoint only available in Development");

        if (string.IsNullOrWhiteSpace(to))
            return BadRequest("Please provide 'to' parameter with email address");

        try
        {
            _logger.LogInformation("Sending test email to {Email}", to);

            // NOTE: Call the verification email method with test data
            var testUrl = Url.Action("Verify", "Account", 
                new { token = "test-token-12345" }, 
                Request.Scheme, Request.Host.ToString());

            await _emailService.SendVerificationEmailAsync(to, "Test User", testUrl!);

            return Ok(new
            {
                success = true,
                message = "Test email sent successfully!",
                email = to,
                note = "Check your inbox (may take a few seconds). If you don't receive it, check the app logs for SMTP errors."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to send email",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// NOTA BENE: Check if Gmail SMTP configuration is valid.
    /// </summary>
    [HttpGet("check-email-config")]
    public IActionResult CheckEmailConfig()
    {
        if (!_env.IsDevelopment())
            return BadRequest("Test endpoint only available in Development");

        var config = new
        {
            smtpHost = Environment.GetEnvironmentVariable("Email__SmtpHost") 
                ?? "Not set (check appsettings.json)",
            smtpPort = Environment.GetEnvironmentVariable("Email__SmtpPort") 
                ?? "Not set (check appsettings.json)",
            smtpUser = Environment.GetEnvironmentVariable("Email__SmtpUser") 
                ?? "Not set (check appsettings.json)",
            smtpUserConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Email__SmtpUser")),
            smtpPassConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Email__SmtpPass")),
            fromName = Environment.GetEnvironmentVariable("Email__FromName") 
                ?? "Not set (check appsettings.json)",
            environment = _env.EnvironmentName
        };

        if (!config.smtpUserConfigured || !config.smtpPassConfigured)
        {
            return BadRequest(new
            {
                configured = false,
                message = "Email credentials not configured. Check appsettings.json or environment variables.",
                config
            });
        }

        return Ok(new
        {
            configured = true,
            message = "Email configuration looks good!",
            config
        });
    }
}
