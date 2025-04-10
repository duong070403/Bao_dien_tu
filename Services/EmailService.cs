using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WebBaoDienTu.Services
{
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderPassword;



        public EmailService(string smtpServer, int smtpPort, string senderEmail, string senderPassword)
        {
            _smtpServer = smtpServer ?? throw new ArgumentNullException(nameof(smtpServer));
            _smtpPort = smtpPort;
            _senderEmail = senderEmail ?? throw new ArgumentNullException(nameof(senderEmail));
            _senderPassword = senderPassword ?? throw new ArgumentNullException(nameof(senderPassword));
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_senderEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_senderEmail, _senderPassword);

                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                throw new Exception($"Không thể gửi email: {ex.Message}", ex);
            }
        }
    }
}