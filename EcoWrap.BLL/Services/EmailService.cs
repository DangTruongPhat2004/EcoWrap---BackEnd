using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using Microsoft.Extensions.Configuration;

namespace EcoWrap.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpHost = _config["Smtp:Host"];         // smtp.gmail.com
            var smtpPort = int.Parse(_config["Smtp:Port"]); // 587
            var smtpUser = _config["Smtp:User"];
            var smtpPass = _config["Smtp:Pass"];

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(smtpUser, "EcoWrap Support");
                    mail.To.Add(toEmail);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;

                    await client.SendMailAsync(mail).ConfigureAwait(false);
                }
            }
        }
    }
}