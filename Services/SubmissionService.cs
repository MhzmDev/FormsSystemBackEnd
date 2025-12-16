using DynamicForm.Data;
using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Text;
using static Azure.Core.HttpHeader;

namespace DynamicForm.Services
{
    public class SubmissionService : ISubmissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<SubmissionService> _logger;
        private readonly IWhatsAppService _whatsAppService;

        public SubmissionService(ApplicationDbContext context, IWhatsAppService whatsAppService, IEmailService emailService,
            ILogger<SubmissionService> logger)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetSubmissionsByRejectionReasonAsync(string rejectionReason, int page,
            int pageSize, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.FormSubmissions
                .Include(s => s.Form)
                .Include(s => s.FormSubmissionValues)
                .Where(s =>
                    (s.RejectionReason != null && s.RejectionReason.Contains(rejectionReason)) ||
                    (s.RejectionReasonEn != null && s.RejectionReasonEn.Contains(rejectionReason)))
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

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Calculate today's submissions count
            var today = DateTime.UtcNow.Date;

            var todaySubmissionsCount = await query
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
                Values = CreateSubmissionValueSummary(submission.FormSubmissionValues)
            });

            return new PagedResultSubmission<FormSubmissionSummaryDto>
            {
                TotalCount = totalCount,
                TodaySubmissionsCount = todaySubmissionsCount,
                TodayApprovedSubmissionsCount = 0,
                TodayRejectedSubmissionsCount = todaySubmissionsCount,
                ApprovedSubmissionsCount = 0,
                RejectedSubmissionsCount = totalCount,
                Items = summaryItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<bool> ExportSubmissionsToCSVAndEmailAsync(string rejectionReason, DateTime? fromDate, DateTime? toDate,
            string recipientEmail)
        {
            try
            {
                _logger.LogInformation("Starting CSV export for rejection reason: {RejectionReason}", rejectionReason);

                // Get all submissions without pagination
                var query = _context.FormSubmissions
                    .Include(s => s.Form)
                    .Include(s => s.FormSubmissionValues)
                    .Where(s =>
                        (s.RejectionReason != null && s.RejectionReason.Contains(rejectionReason)) ||
                        (s.RejectionReasonEn != null && s.RejectionReasonEn.Contains(rejectionReason)))
                    .AsQueryable();

                if (fromDate.HasValue)
                {
                    query = query.Where(s => s.SubmittedDate >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(s => s.SubmittedDate <= toDate.Value);
                }

                var submissions = await query
                    .OrderByDescending(s => s.SubmittedDate)
                    .ToListAsync();

                if (!submissions.Any())
                {
                    _logger.LogWarning("No submissions found for rejection reason: {RejectionReason}", rejectionReason);

                    return false;
                }

                // Generate CSV content
                var csvContent = GenerateCSVContent(submissions);

                // Convert to byte array
                var csvBytes = Encoding.UTF8.GetBytes(csvContent);

                // Prepare email details
                var subject = $"تقرير الطلبات المرفوضة - {rejectionReason}";

                var body = $@"
                    <html dir='rtl'>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>تقرير الطلبات المرفوضة</h2>
                        <p>السبب: <strong>{rejectionReason}</strong></p>
                        <p>عدد الطلبات: <strong>{submissions.Count}</strong></p>
                        <p>التاريخ من: {fromDate?.ToString("yyyy-MM-dd") ?? "غير محدد"}</p>
                        <p>التاريخ إلى: {toDate?.ToString("yyyy-MM-dd") ?? "غير محدد"}</p>
                        <p>يرجى الاطلاع على الملف المرفق للحصول على التفاصيل الكاملة.</p>
                    </body>
                    </html>
                ";

                var fileName = $"rejected_submissions_{rejectionReason}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                // Send email with attachment
                await _emailService.SendEmailWithAttachmentAsync(recipientEmail, subject, body, csvBytes, fileName, "text/csv");

                _logger.LogInformation("CSV export email sent successfully to {Email}", recipientEmail);

                return true;
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting submissions to CSV and sending email");

                return false;
            }
        }

        // NEW: Overloaded method that accepts pre-filtered submissions
        public async Task<bool> ExportSubmissionsToCSVAndEmailAsync(
            List<FormSubmission> submissions,
            string reportTitle,
            string recipientEmail,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                _logger.LogInformation("Starting CSV export for {Count} pre-filtered submissions: {ReportTitle}",
                    submissions.Count, reportTitle);

                if (!submissions.Any())
                {
                    _logger.LogWarning("No submissions provided for export");
                    return false;
                }

                // Generate CSV content
                var csvContent = GenerateCSVContent(submissions);

                // Convert to byte array
                var csvBytes = Encoding.UTF8.GetBytes(csvContent);

                // Prepare email details
                var subject = $"تقرير الطلبات المرفوضة - {reportTitle}";

                var body = $@"
            <html dir='rtl'>
            <body style='font-family: Arial, sans-serif;'>
                <h2>تقرير الطلبات المرفوضة</h2>
                <p>السبب: <strong>{reportTitle}</strong></p>
                <p>عدد الطلبات: <strong>{submissions.Count}</strong></p>
                <p>التاريخ من: {fromDate?.ToString("yyyy-MM-dd") ?? "غير محدد"}</p>
                <p>التاريخ إلى: {toDate?.ToString("yyyy-MM-dd") ?? "غير محدد"}</p>
                <p>يرجى الاطلاع على الملف المرفق للحصول على التفاصيل الكاملة.</p>
            </body>
            </html>
        ";

                var fileName = $"rejected_submissions_{SanitizeFileName(reportTitle)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                // Send email with attachment
                await _emailService.SendEmailWithAttachmentAsync(recipientEmail, subject, body, csvBytes, fileName, "text/csv");

                _logger.LogInformation("CSV export email sent successfully to {Email}", recipientEmail);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting submissions to CSV and sending email");
                return false;
            }
        }

        // Helper method to sanitize file names
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
        public async Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetAllSubmissionsAsync(int page, int pageSize, DateTime? fromDate,
            DateTime? toDate, string? status, bool? isActive)
        {
            var query = _context.FormSubmissions
                .Include(s => s.Form)
                .Include(s => s.FormSubmissionValues)
                .AsQueryable();

            // Apply filters
            if (fromDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate <= toDate.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            // Filter by form active status
            if (isActive.HasValue)
            {
                query = query.Where(s => s.Form.IsActive == isActive.Value);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Calculate today's submissions count
            var today = DateTime.UtcNow.Date;

            var todaySubmissionsCount = await _context.FormSubmissions
                .Where(s => s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1))
                .CountAsync();

            // NEW: Calculate today's approved submissions count
            var todayApprovedSubmissionsCount = await query
                .Where(s => s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1) && s.Status == "مقبول")
                .CountAsync();

            // NEW: Calculate today's rejected submissions count
            var todayRejectedSubmissionsCount = await query
                .Where(s => s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1) && s.Status == "مرفوض")
                .CountAsync();

            // NEW: Calculate approved submissions count (within date range)
            var approvedSubmissionsCount = await query
                .Where(s => s.Status == "مقبول")
                .CountAsync();

            // NEW: Calculate rejected submissions count (within date range)
            var rejectedSubmissionsCount = await query
                .Where(s => s.Status == "مرفوض")
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
                Values = CreateSubmissionValueSummary(submission.FormSubmissionValues)
            });

            return new PagedResultSubmission<FormSubmissionSummaryDto>
            {
                TotalCount = totalCount,
                TodaySubmissionsCount = todaySubmissionsCount,
                TodayApprovedSubmissionsCount = todayApprovedSubmissionsCount, // NEW
                TodayRejectedSubmissionsCount = todayRejectedSubmissionsCount, // NEW
                ApprovedSubmissionsCount = approvedSubmissionsCount, // NEW
                RejectedSubmissionsCount = rejectedSubmissionsCount, // NEW
                Items = summaryItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetSubmissionsByFormIdAsync(int formId, int page, int pageSize,
            DateTime? fromDate, DateTime? toDate, string? status)
        {
            var query = _context.FormSubmissions
                .Include(s => s.Form)
                .Include(s => s.FormSubmissionValues)
                .Where(s => s.FormId == formId) // Filter by specific form
                .AsQueryable();

            // Apply additional filters
            if (fromDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate <= toDate.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Calculate today's submissions count for this specific form
            var today = DateTime.UtcNow.Date;

            var todaySubmissionsCount = await _context.FormSubmissions
                .Where(s => s.FormId == formId && s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1))
                .CountAsync();

            // NEW: Calculate today's approved submissions count
            var todayApprovedSubmissionsCount = await query
                .Where(s => s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1) && s.Status == "مقبول")
                .CountAsync();

            // NEW: Calculate today's rejected submissions count
            var todayRejectedSubmissionsCount = await query
                .Where(s => s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1) && s.Status == "مرفوض")
                .CountAsync();

            // NEW: Calculate approved submissions count (within date range)
            var approvedSubmissionsCount = await query
                .Where(s => s.Status == "مقبول")
                .CountAsync();

            // NEW: Calculate rejected submissions count (within date range)
            var rejectedSubmissionsCount = await query
                .Where(s => s.Status == "مرفوض")
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
                Values = CreateSubmissionValueSummary(submission.FormSubmissionValues)
            });

            return new PagedResultSubmission<FormSubmissionSummaryDto>
            {
                Items = summaryItems,
                TotalCount = totalCount,
                TodaySubmissionsCount = todaySubmissionsCount,
                TodayApprovedSubmissionsCount = todayApprovedSubmissionsCount, // NEW
                TodayRejectedSubmissionsCount = todayRejectedSubmissionsCount, // NEW
                ApprovedSubmissionsCount = approvedSubmissionsCount, // NEW
                RejectedSubmissionsCount = rejectedSubmissionsCount, // NEW
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PagedResultSubmission<FormSubmissionSummaryDto>> GetActiveFormSubmissionsAsync(int page, int pageSize, DateTime? fromDate,
            DateTime? toDate, string? status)
        {
            var query = _context.FormSubmissions
                .Include(s => s.Form)
                .Include(s => s.FormSubmissionValues)
                .Where(s => s.Form.IsActive) // Filter by active form only
                .AsQueryable();

            // Apply additional filters
            if (fromDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.SubmittedDate <= toDate.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.Status == status);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Calculate today's submissions count for active form only
            var today = DateTime.UtcNow.Date;

            var todaySubmissionsCount = await _context.FormSubmissions
                .Include(s => s.Form)
                .Where(s => s.Form.IsActive && s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1))
                .CountAsync();

            // NEW: Calculate today's approved submissions count
            var todayApprovedSubmissionsCount = await query
                .Where(s => s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1) && s.Status == "مقبول")
                .CountAsync();

            // NEW: Calculate today's rejected submissions count
            var todayRejectedSubmissionsCount = await query
                .Where(s => s.SubmittedDate >= today && s.SubmittedDate < today.AddDays(1) && s.Status == "مرفوض")
                .CountAsync();

            // NEW: Calculate approved submissions count(within date range)
            var approvedSubmissionsCount = await query
                .Where(s => s.Status == "مقبول")
                .CountAsync();

            // NEW: Calculate rejected submissions count (within date range)
            var rejectedSubmissionsCount = await query
                .Where(s => s.Status == "مرفوض")
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
                Values = CreateSubmissionValueSummary(submission.FormSubmissionValues)
            });

            return new PagedResultSubmission<FormSubmissionSummaryDto>
            {
                Items = summaryItems,
                TotalCount = totalCount,
                TodaySubmissionsCount = todaySubmissionsCount,
                TodayApprovedSubmissionsCount = todayApprovedSubmissionsCount, // NEW
                TodayRejectedSubmissionsCount = todayRejectedSubmissionsCount, // NEW
                ApprovedSubmissionsCount = approvedSubmissionsCount, // NEW
                RejectedSubmissionsCount = rejectedSubmissionsCount, // NEW
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<FormSubmissionResponseDto?> GetSubmissionByIdAsync(int submissionId)
        {
            var submission = await _context.FormSubmissions
                .Include(s => s.Form)
                .Include(s => s.FormSubmissionValues)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
            {
                return null;
            }

            return new FormSubmissionResponseDto
            {
                SubmissionId = submission.SubmissionId,
                FormName = submission.Form.Name,
                SubmittedDate = submission.SubmittedDate,
                Status = submission.Status,
                SubmittedBy = submission.SubmittedBy,
                PhoneNumber = submission.FormSubmissionValues
                    .FirstOrDefault(v => v.FieldNameAtSubmission.Contains("phone", StringComparison.OrdinalIgnoreCase))?.FieldValue,
                RejectionReason = submission.RejectionReason,
                RejectionReasonEn = submission.RejectionReasonEn,
                Values = submission.FormSubmissionValues
                    .OrderBy(v => v.FieldId) // Maintain some order
                    .Select(v => new FieldValueDto
                    {
                        FieldName = v.FieldNameAtSubmission,
                        Label = v.LabelAtSubmission,
                        Value = v.FieldValue,
                        FieldType = v.FieldTypeAtSubmission
                    }).ToList()
            };
        }

        public async Task<bool> UpdateSubmissionStatusAsync(int submissionId, string status)
        {
            var submission = await _context.FormSubmissions
                .Include(s => s.FormSubmissionValues)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
            {
                return false;
            }

            var oldStatus = submission.Status;
            submission.Status = status;
            await _context.SaveChangesAsync();

            // Send WhatsApp message if status changed to "معتمد"
            if (oldStatus != "معتمد" && status == "معتمد")
            {
                _ = Task.Run(async () => await SendApprovalWhatsAppMessageAsync(submission));
            }

            return true;
        }

        public async Task<bool> DeleteSubmissionAsync(int submissionId)
        {
            var submission = await _context.FormSubmissions.FindAsync(submissionId);

            if (submission == null)
            {
                return false;
            }

            // Soft delete - you could add IsDeleted field or just update status
            submission.Status = "محذوف";
            await _context.SaveChangesAsync();

            return true;
        }

        public List<SubmissionValueSummaryDto> CreateSubmissionValueSummary(ICollection<FormSubmissionValue> values)
        {
            return values.OrderBy(v => v.FieldId)
                .Select(v => new SubmissionValueSummaryDto
                {
                    ArLabel = v.LabelAtSubmission,
                    EnLabel = GenerateEnglishLabel(v.FieldNameAtSubmission),
                    Value = v.FieldValue
                }).ToList();
        }

        private string GenerateCSVContent(List<FormSubmission> submissions)
        {
            var csv = new StringBuilder();

            // Get all unique field names across all submissions
            var allFieldNames = submissions
                .SelectMany(s => s.FormSubmissionValues)
                .Select(v => v.FieldNameAtSubmission)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            // Build header
            var headers = new List<string>
            {
                "رقم الطلب",
                "اسم النموذج",
                "تاريخ التقديم",
                "الحالة",
                "مقدم الطلب",
                "رقم الهاتف",
                "سبب الرفض"
            };

            headers.AddRange(allFieldNames);
            csv.AppendLine(string.Join(",", headers.Select(h => EscapeCSVField(h))));

            // Build rows
            foreach (var submission in submissions)
            {
                var row = new List<string>
                {
                    submission.SubmissionId.ToString(),
                    EscapeCSVField(submission.Form.Name),
                    submission.SubmittedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    EscapeCSVField(submission.Status),
                    EscapeCSVField(submission.SubmittedBy ?? ""),
                    EscapeCSVField(submission.FormSubmissionValues
                        .FirstOrDefault(v => v.FieldNameAtSubmission.Contains("phone", StringComparison.OrdinalIgnoreCase))?.FieldValue ?? ""),
                    EscapeCSVField(submission.RejectionReason ?? "")
                };

                // Add field values in the same order as headers
                foreach (var fieldName in allFieldNames)
                {
                    var value = submission.FormSubmissionValues
                        .FirstOrDefault(v => v.FieldNameAtSubmission == fieldName)?.FieldValue ?? "";

                    row.Add(EscapeCSVField(value));
                }

                csv.AppendLine(string.Join(",", row));
            }

            return csv.ToString();
        }

        private string EscapeCSVField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return "\"\"";
            }

            // Escape quotes and wrap in quotes if contains comma, quote, or newline
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return $"\"{field}\"";
        }

        private async Task SendApprovalWhatsAppMessageAsync(FormSubmission submission)
        {
            try
            {
                _logger.LogInformation("Attempting to send WhatsApp message for approved submission {SubmissionId}", submission.SubmissionId);

                // Get submission data
                var phoneValue = submission.FormSubmissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("phone") ||
                    v.FieldNameAtSubmission.ToLower().Contains("mobile") ||
                    v.LabelAtSubmission.Contains("هاتف") ||
                    v.LabelAtSubmission.Contains("جوال"));

                var fullNameValue = submission.FormSubmissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("name") ||
                    v.FieldNameAtSubmission.ToLower().Contains("fullname") ||
                    v.LabelAtSubmission.Contains("اسم"));

                var nationalIdValue = submission.FormSubmissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("nationalid") ||
                    v.LabelAtSubmission.Contains("هوية"));

                var birthDateValue = submission.FormSubmissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("birth") ||
                    v.LabelAtSubmission.Contains("ميلاد"));

                var salaryValue = submission.FormSubmissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("salary") ||
                    v.LabelAtSubmission.Contains("راتب"));

                var commitmentsValue = submission.FormSubmissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("commitment") ||
                    v.LabelAtSubmission.Contains("التزام"));

                if (phoneValue == null || string.IsNullOrWhiteSpace(phoneValue.FieldValue))
                {
                    _logger.LogWarning("Phone number not found for submission {SubmissionId}", submission.SubmissionId);

                    return;
                }

                var phoneNumber = phoneValue.FieldValue;
                var fullName = fullNameValue?.FieldValue ?? "عميل محزم";

                // Create subscriber in Morasalaty
                var subscriberCreated = await _whatsAppService.CreateSubscriberAsync(phoneNumber, fullName);

                if (!subscriberCreated)
                {
                    _logger.LogWarning("Failed to create subscriber for {Phone}, but continuing with template send", phoneNumber);
                }

                // Prepare template parameters based on your template structure
                var templateParams = new Dictionary<string, string>
                {
                    ["BODY_1"] = submission.SubmissionId.ToString(), // رقم الطلب: {{1}}
                    ["BODY_2"] = fullNameValue?.FieldValue ?? "غير محدد", // الاسم الثلاثي: {{2}}
                    ["BODY_3"] = phoneValue.FieldValue, // رقم الجوال: {{3}}
                    ["BODY_4"] = nationalIdValue?.FieldValue ?? "غير محدد", // رقم الهوية: {{4}}
                    ["BODY_5"] = birthDateValue?.FieldValue ?? "غير محدد", // تاريخ الميلاد: {{5}}
                    ["BODY_6"] = salaryValue?.FieldValue ?? "غير محدد", // الراتب: {{6}}
                    ["BODY_7"] = commitmentsValue?.FieldValue ?? "غير محدد", // الالتزامات: {{7}}
                    ["BODY_8"] = "جديد" // مدة الخدمة: {{8}} - default for new customers
                };

                // Send WhatsApp template message
                var messageSent = await _whatsAppService.SendApprovalMessageAsync(phoneNumber, templateParams);

                if (messageSent)
                {
                    _logger.LogInformation("WhatsApp approval message sent successfully for submission {SubmissionId} to {Phone}",
                        submission.SubmissionId, phoneNumber);
                }
                else
                {
                    _logger.LogError("Failed to send WhatsApp approval message for submission {SubmissionId} to {Phone}",
                        submission.SubmissionId, phoneNumber);
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp approval message for submission {SubmissionId}", submission.SubmissionId);
            }
        }

        private string CreateSubmissionPreview(ICollection<FormSubmissionValue> values)
        {
            // Create a preview string from first few important fields
            var nameField = values.FirstOrDefault(v =>
                v.FieldNameAtSubmission.Contains("name", StringComparison.OrdinalIgnoreCase) ||
                v.LabelAtSubmission.Contains("اسم"));

            var emailField = values.FirstOrDefault(v => v.FieldTypeAtSubmission == "email");

            var preview = new List<string>();

            if (nameField != null)
            {
                preview.Add(nameField.FieldValue);
            }

            if (emailField != null)
            {
                preview.Add(emailField.FieldValue);
            }

            return string.Join(" - ", preview.Take(2));
        }

        private string GenerateEnglishLabel(string fieldName)
        {
            // Handle predefined field names first
            var predefinedLabels = fieldName.ToLower() switch
            {
                "name" or "fullname" => "Name",
                "email" => "Email",
                "phone" or "phonenumber" => "Phone Number",
                "address" => "Address",
                "birthdate" => "Birth Date",
                "gender" => "Gender",
                "nationality" => "Nationality",
                "occupation" => "Occupation",
                "company" => "Company",
                "position" => "Position",
                "experience" => "Experience",
                "education" => "Education",
                "skills" => "Skills",
                "comments" => "Comments",
                "notes" => "Notes",
                "nationalid" => "National ID",
                "nationalidtype" => "National ID Type",
                "governorate" => "Governorate",
                "maritalstatus" => "Marital Status",
                "referenceno" => "Reference No",
                "creationdate" => "Creation Date",
                // New Mandatory Fields
                "citizenshipstatus" => "Citizenship Status",
                "hasmortgage" => "Has Mortgage",
                "monthlysalary" => "Monthly Salary",
                "monthlycommitments" => "Monthly Commitments",
                _ => null
            };

            if (!string.IsNullOrEmpty(predefinedLabels))
            {
                return predefinedLabels;
            }

            // Handle camelCase by splitting on uppercase letters
            var result = Regex.Replace(fieldName, "([a-z])([A-Z])", "$1 $2");

            // Handle underscores and hyphens
            result = result.Replace("_", " ").Replace("-", " ");

            // Title case each word
            return string.Join(" ", result.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpper(word[0]) + word[1..].ToLower()));
        }
    }
}