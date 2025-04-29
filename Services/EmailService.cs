using System.Net.Mail;
using System.Net;

namespace Rentality.PriceUpdater.Services;

internal class EmailService(string host, string user, string password)
{
    public async Task SendLowBalanceAlert(string from, string to, string walletAddress, decimal balance)
    {
        var message = new MailMessage(from, to)
        {
            Subject = "🚨 Low Wallet Balance Alert",
            Body = $"Rentality.PriceUpdater alert: Wallet {walletAddress} balance is low: {balance} ETH"
        };

        using var smtp = new SmtpClient(host)
        {
            Port = 443,
            Credentials = new NetworkCredential(user, password),
            EnableSsl = true
        };

        await smtp.SendMailAsync(message);
    }
}
