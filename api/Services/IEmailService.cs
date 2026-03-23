namespace PerformanceReviewApi.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toAddress, string toName, string subject, string body);
}
