using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PerformanceReviewApi.Services;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        _settings = configuration.GetSection("EmailSettings").Get<EmailSettings>()
                    ?? throw new InvalidOperationException(
                        "EmailSettings section is missing from application configuration.");

        if (string.IsNullOrWhiteSpace(_settings.Host))
            throw new InvalidOperationException("EmailSettings.Host must be configured.");
        if (string.IsNullOrWhiteSpace(_settings.FromAddress))
            throw new InvalidOperationException("EmailSettings.FromAddress must be configured.");
    }

    public async Task SendEmailAsync(string toAddress, string toName, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(new MailboxAddress(toName, toAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            _logger.LogInformation("Email sent to {ToAddress} with subject '{Subject}'", toAddress, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress} with subject '{Subject}'", toAddress, subject);
            throw;
        }
    }
}
