using System.Net;
using System.Net.Mail;
using DynamicForm.BLL.Contracts;
using DynamicForm.DAL.Models.Configuration;
using Microsoft.Extensions.Options;

namespace DynamicForm.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _emailSettings.Validate(); // Validate configuration on startup
            _logger = logger;
        }

        public async Task<bool> SendEmailWithAttachmentAsync(string recipientEmail, string subject, string body, 
            byte[] attachmentContent, string attachmentFileName, string attachmentContentType)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                message.To.Add(recipientEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                // Add attachment
                using var memoryStream = new MemoryStream(attachmentContent);
                var attachment = new Attachment(memoryStream, attachmentFileName, attachmentContentType);
                message.Attachments.Add(attachment);

                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort);
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword);

                await smtpClient.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {Email} with attachment {FileName}", 
                    recipientEmail, attachmentFileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", recipientEmail);
                return false;
            }
        }
    }
}