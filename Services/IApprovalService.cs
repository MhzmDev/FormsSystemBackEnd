using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;

namespace DynamicForm.Services
{
    public interface IApprovalService
    {
        Task<ApprovalResult> ProcessApprovalAsync(ICollection<FormSubmissionValue> submissionValues);
    }
}