using DynamicForm.Data;
using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        public async Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetRejectedByServiceDuration90DaysAsync(
            int page,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            bool? olderThan3Months = null,
            bool sendEmail = false,
            string? recipientEmail = null)
        {
            var query = _context.FormSubmissions
                .Include(s => s.Form)
                .Include(s => s.FormSubmissionValues)
                .Where(s => s.Status == FormConstants.SubmissionStatus.Rejected)
                .Where(s => s.FormSubmissionValues.Any(v =>
                    v.FieldNameAtSubmission == "ServiceDuration" &&
                    v.FieldValue == "اقل من ٣ شهور"))
                .Where(s => s.RejectionReason == "عذراً، مدة الخدمة يجب أن تكون أكثر من 3 شهور")
                .AsQueryable();

            // If fromDate or toDate are provided, use them and ignore olderThan3Months
            if (fromDate.HasValue || toDate.HasValue)
            {
                if (fromDate.HasValue)
                {
                    query = query.Where(s => s.SubmittedDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(s => s.SubmittedDate <= toDate.Value);
                }
            }
            // Otherwise, apply olderThan3Months logic
            else if (olderThan3Months.HasValue)
            {
                var todayDate = DateTime.Now.Date;
                var exactDayBeforeThreeMonths = todayDate.AddMonths(-3).AddDays(-1);

                if (olderThan3Months.Value)
                {
                    // Get submissions from ONLY the day before 3 months ago (e.g., 14 September 2025 if today is 15 December 2025)
                    var startOfDay = exactDayBeforeThreeMonths.Date;
                    var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

                    query = query.Where(s => s.SubmittedDate >= startOfDay && s.SubmittedDate <= endOfDay);
                }
                else
                {
                    // Get submissions within the last 3 months (from 3 months ago until today)
                    var threeMonthsAgo = todayDate.AddMonths(-3);
                    query = query.Where(s => s.SubmittedDate >= threeMonthsAgo && s.SubmittedDate <= todayDate);
                }
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

            // Send email if requested - using new overload Email Service Method that accepts list of DTOs
            if (sendEmail && !string.IsNullOrWhiteSpace(recipientEmail))
            {
                // Get ALL submissions (not just paginated) for the email report
                var allSubmissionsForEmail = await query
                    .OrderByDescending(s => s.SubmittedDate)
                    .ToListAsync();

                // Determine report title based on filtering
                var reportTitle = (fromDate.HasValue, toDate.HasValue, olderThan3Months) switch
                {
                    (true, true, _) => $"مدة الخدمة أقل من 3 أشهر (من {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd})",
                    (true, false, _) => $"مدة الخدمة أقل من 3 أشهر (من {fromDate:yyyy-MM-dd})",
                    (false, true, _) => $"مدة الخدمة أقل من 3 أشهر (حتى {toDate:yyyy-MM-dd})",
                    (false, false, true) => "مدة الخدمة أقل من 3 أشهر (اليوم الذي يسبق 3 أشهر)",
                    (false, false, false) => "مدة الخدمة أقل من 3 أشهر (آخر 3 أشهر)",
                    _ => "مدة الخدمة أقل من 3 أشهر"
                };

                // Send email in background
                _ = Task.Run(async () =>
                    await _submissionService.ExportSubmissionsToCSVAndEmailAsync(
                        allSubmissionsForEmail,
                        reportTitle,
                        recipientEmail,
                        fromDate,
                        toDate));
            }

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