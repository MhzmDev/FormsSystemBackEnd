using DynamicForm.Models.DTOs;

namespace DynamicForm.Services
{
    public interface ISubmissionService
    {
        Task<PagedResult<FormSubmissionSummaryDto>> GetAllSubmissionsAsync(int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? status);
        Task<PagedResult<FormSubmissionSummaryDto>> GetSubmissionsByFormIdAsync(int formId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, string? status);
        Task<FormSubmissionResponseDto?> GetSubmissionByIdAsync(int submissionId);
        Task<bool> UpdateSubmissionStatusAsync(int submissionId, string status);
        Task<bool> DeleteSubmissionAsync(int submissionId);
    }
}