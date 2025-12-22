using DynamicForm.DAL.Models.Entities;

namespace DynamicForm.BLL.Contracts
{
    public interface INotificationService
    {
        Task SendApprovalNotificationAsync(FormSubmission submission, List<FormSubmissionValue> values);
    }
}