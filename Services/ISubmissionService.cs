using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;

namespace DynamicForm.Services
{
    public interface ISubmissionService
    {
        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetAllSubmissionsAsync(int page, int pageSize, DateTime? fromDate, DateTime? toDate,
            string? status, bool? isActive);

        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetSubmissionsByFormIdAsync(int formId, int page, int pageSize, DateTime? fromDate,
            DateTime? toDate, string? status);

        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetActiveFormSubmissionsAsync(int page, int pageSize, DateTime? fromDate,
            DateTime? toDate, string? status);

        Task<FormSubmissionResponseDto?> GetSubmissionByIdAsync(int submissionId);
        Task<bool> UpdateSubmissionStatusAsync(int submissionId, string status);
        Task<bool> DeleteSubmissionAsync(int submissionId);
        List<SubmissionValueSummaryDto> CreateSubmissionValueSummary(ICollection<FormSubmissionValue> values);

        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetSubmissionsByRejectionReasonAsync(string rejectionReason, int page, int pageSize,
            DateTime? fromDate, DateTime? toDate);

        Task<bool> ExportSubmissionsToCSVAndEmailAsync(string rejectionReason, DateTime? fromDate, DateTime? toDate, string recipientEmail);
    }
}