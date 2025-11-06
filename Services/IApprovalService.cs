using DynamicForm.Models;
using DynamicForm.Models.DTOs;

namespace DynamicForm.Services
{
    public interface IApprovalService
    {
        Task<ApprovalResult> ProcessApprovalAsync(ICollection<FormSubmissionValue> submissionValues);
    }
}