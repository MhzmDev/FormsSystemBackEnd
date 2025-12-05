namespace DynamicForm.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailWithAttachmentAsync(string recipientEmail, string subject, string body, byte[] attachmentContent, 
            string attachmentFileName, string attachmentContentType);
    }
}