using DynamicForm.Models.DTOs.WhatsApp;

namespace DynamicForm.Services
{
    public interface IWhatsAppService
    {
        Task<bool> CreateSubscriberAsync(string phoneNumber, string fullName);
        Task<bool> SendApprovalMessageAsync(string phoneNumber, Dictionary<string, string> templateParams);
        string ValidateAndFormatPhoneNumber(string phoneNumber);
    }
}