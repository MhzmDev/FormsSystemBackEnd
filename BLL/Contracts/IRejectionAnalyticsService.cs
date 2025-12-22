using DynamicForm.BLL.DTOs.Form;
using DynamicForm.BLL.DTOs.General;
using DynamicForm.BLL.DTOs.Submissions.RejectionReasons;

namespace DynamicForm.BLL.Contracts
{
    public interface IRejectionAnalyticsService
    {
        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetRejectedByServiceDurationAsync(int page, int pageSize, DateTime? fromDate,
            DateTime? toDate);

        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetRejectedByServiceDuration90DaysAsync(int page, int pageSize, DateTime? fromDate,
            DateTime? toDate, bool? olderThan3Months = null, bool sendEmail = false, string? recipientEmail = null);

        Task<List<RejectionStatisticsDto>> GetRejectionStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    }
}