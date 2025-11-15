using DynamicForm.Models.Entities;

namespace DynamicForm.Services
{
    public interface INotificationService
    {
        Task SendApprovalNotificationAsync(FormSubmission submission, List<FormSubmissionValue> values);
    }
}