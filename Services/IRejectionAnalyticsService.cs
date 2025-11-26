using DynamicForm.Models.DTOs;

namespace DynamicForm.Services
{
    public interface IRejectionAnalyticsService
    {
        Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetRejectedByServiceDurationAsync(int page, int pageSize, DateTime? fromDate,
            DateTime? toDate);

        Task<List<RejectionStatisticsDto>> GetRejectionStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    }
}