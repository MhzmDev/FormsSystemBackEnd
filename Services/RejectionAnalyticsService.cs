using DynamicForm.Data;
using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DynamicForm.Services
{
    public class RejectionAnalyticsService : IRejectionAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RejectionAnalyticsService> _logger;
        private readonly ISubmissionService _submissionService;

        public RejectionAnalyticsService(
            ApplicationDbContext context,
            ILogger<RejectionAnalyticsService> logger,
            ISubmissionService submissionService)
        {
            _context = context;
            _logger = logger;
            _submissionService = submissionService;
        }

        public async Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetRejectedByServiceDurationAsync(
            int page,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.FormSubmissions
                .Include(s => s.Form)
                .Include(s => s.FormSubmissionValues)
                .Where(s => s.Status == FormConstants.SubmissionStatus.Rejected)
                .Where(s => s.FormSubmissionValues.Any(v =>
                    v.FieldNameAtSubmission == "ServiceDuration" &&
                    v.FieldValue == "اقل من ٣ شهور"))
                .AsQueryable();

            // Apply date filters
            if (fromDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate <= toDate.Value);
            }

            // Get total count
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Calculate today's count
            var today = DateTime.UtcNow.Date;

            var todayCount = await query
                .Where(s => s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1))
                .CountAsync();

            // Apply pagination
            var submissions = await query
                .OrderByDescending(s => s.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var summaryItems = submissions.Select(submission => new FormSubmissionSummaryDto
            {
                SubmissionId = submission.SubmissionId,
                FormId = submission.FormId,
                FormName = submission.Form.Name,
                SubmittedDate = submission.SubmittedDate,
                Status = submission.Status,
                SubmittedBy = submission.SubmittedBy,
                PhoneNumber = submission.FormSubmissionValues
                    .FirstOrDefault(v => v.FieldNameAtSubmission.Contains("phone", StringComparison.OrdinalIgnoreCase))?.FieldValue,
                RejectionReason = submission.RejectionReason,
                RejectionReasonEn = submission.RejectionReasonEn,
                Preview = CreateSubmissionPreview(submission.FormSubmissionValues),
                Values = _submissionService.CreateSubmissionValueSummary(submission.FormSubmissionValues)
            });

            return new PagedResultSubmission<FormSubmissionSummaryDto>
            {
                Items = summaryItems,
                TotalCount = totalCount,
                TodaySubmissionsCount = todayCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<List<RejectionStatisticsDto>> GetRejectionStatisticsAsync(
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _context.FormSubmissions
                .Where(s => s.Status == FormConstants.SubmissionStatus.Rejected)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate <= toDate.Value);
            }

            var statistics = await query
                .GroupBy(s => s.RejectionReason)
                .Select(g => new RejectionStatisticsDto
                {
                    RejectionReason = g.Key ?? "غير محدد",
                    Count = g.Count(),
                    Percentage = 0 // Will calculate after getting total
                })
                .OrderByDescending(s => s.Count)
                .ToListAsync();

            var totalRejections = statistics.Sum(s => s.Count);

            foreach (var stat in statistics)
            {
                stat.Percentage = totalRejections > 0
                    ? Math.Round((stat.Count / (double)totalRejections) * 100, 2)
                    : 0;
            }

            return statistics;
        }

        private string CreateSubmissionPreview(ICollection<FormSubmissionValue> values)
        {
            var nameField = values.FirstOrDefault(v =>
                v.FieldNameAtSubmission.Contains("name", StringComparison.OrdinalIgnoreCase) ||
                v.LabelAtSubmission.Contains("اسم"));

            var phoneField = values.FirstOrDefault(v =>
                v.FieldNameAtSubmission.Contains("phone", StringComparison.OrdinalIgnoreCase));

            var preview = new List<string>();

            if (nameField != null)
            {
                preview.Add(nameField.FieldValue);
            }

            if (phoneField != null)
            {
                preview.Add(phoneField.FieldValue);
            }

            return string.Join(" - ", preview.Take(2));
        }
    }
}