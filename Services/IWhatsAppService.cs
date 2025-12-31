using DynamicForm.Models.DTOs.WhatsApp;

namespace DynamicForm.Services
{
    public interface IWhatsAppService
    {
        Task<bool> CreateSubscriberAsync(string phoneNumber, string fullName);
        Task<bool> SendApprovalMessageAsync(string phoneNumber, Dictionary<string, string> templateParams);
        Task<bool> SendRejectionMessageAsync(string phoneNumber, string rejectionReason);
        Task<bool> SendTemplateMessageAsync(string phoneNumber, List<string> parameters);
        string ValidateAndFormatPhoneNumber(string phoneNumber);
    }
}