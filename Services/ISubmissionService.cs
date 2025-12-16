using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;

namespace DynamicForm.Services
{
    public interface ISubmissionService
    {
        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetAllSubmissionsAsync(int page, int pageSize, DateTime? fromDate, DateTime? toDate,
            string? status, bool? isActive, bool sendEmail = false, string? recipientEmail = null);

        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetSubmissionsByFormIdAsync(int formId, int page, int pageSize, DateTime? fromDate,
            DateTime? toDate, string? status, bool sendEmail = false, string? recipientEmail = null);

        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetActiveFormSubmissionsAsync(int page, int pageSize, DateTime? fromDate,
            DateTime? toDate, string? status, bool sendEmail = false, string? recipientEmail = null);

        Task<FormSubmissionResponseDto?> GetSubmissionByIdAsync(int submissionId);
        Task<bool> UpdateSubmissionStatusAsync(int submissionId, string status);
        Task<bool> DeleteSubmissionAsync(int submissionId);
        List<SubmissionValueSummaryDto> CreateSubmissionValueSummary(ICollection<FormSubmissionValue> values);

        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetSubmissionsByRejectionReasonAsync(string rejectionReason, int page, int pageSize,
            DateTime? fromDate, DateTime? toDate);

        Task<bool> ExportSubmissionsToCSVAndEmailAsync(string rejectionReason, DateTime? fromDate, DateTime? toDate, string recipientEmail);

        // NEW: Overload that accepts pre-filtered submissions
        Task<bool> ExportSubmissionsToCSVAndEmailAsync(List<FormSubmission> submissions, string reportTitle, string recipientEmail,
            DateTime? fromDate = null, DateTime? toDate = null);
    }
}