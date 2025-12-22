using DynamicForm.BLL.DTOs.Submissions;
using DynamicForm.DAL.Models.Entities;

namespace DynamicForm.BLL.Contracts
{
    public interface IApprovalService
    {
        Task<ApprovalResult> ProcessApprovalAsync(ICollection<FormSubmissionValue> submissionValues);
    }
}