using System.Net.Mail;
using System.Net;
using Rentality.Scheduler.API.Utils;

namespace Rentality.Scheduler.API.Services;

internal class EmailService(EnvReader envReader)
{
    private readonly string _smtpHost = envReader.GetEnvString("SMTP_HOST");
    private readonly string _smtpUser = envReader.GetEnvString("SMTP_USER");
    private readonly string _smtpPassword = envReader.GetEnvString("SMTP_PASSWORD");

    public async Task SendLowBalanceAlert(string from, string to, string walletAddress, decimal balance)
    {
        var message = new MailMessage(from, to)
        {
            Subject = "🚨 Low Wallet Balance Alert",
            Body = $"Rentality.PriceUpdater alert: Wallet {walletAddress} balance is low: {balance} ETH"
        };

        using var smtp = new SmtpClient(_smtpHost)
        {
            Port = 587, 
            Credentials = new NetworkCredential(_smtpUser, _smtpPassword),
            EnableSsl = true
        };

        await smtp.SendMailAsync(message);
    }
}
