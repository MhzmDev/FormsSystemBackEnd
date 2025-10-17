using Microsoft.EntityFrameworkCore;
using DynamicForm.Data;
using DynamicForm.Models.DTOs;
using DynamicForm.Models;

namespace DynamicForm.Services
{
    public class SubmissionService : ISubmissionService
    {
        private readonly ApplicationDbContext _context;

        public SubmissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<FormSubmissionSummaryDto>> GetAllSubmissionsAsync(
            int page,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            string? status)
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

            // Get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var submissions = await query
                .OrderByDescending(s => s.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var summaryItems = submissions.Select(submission =>
            {
                // Extract mandatory field values
                // Add validation and filtering for empty field names
                var values = submission.FormSubmissionValues
                    .Where(v => !string.IsNullOrEmpty(v.FieldNameAtSubmission))
                    .GroupBy(v => v.FieldNameAtSubmission.ToLower())
                    .ToDictionary(g => g.Key, g => g.First().FieldValue);

                return new FormSubmissionSummaryDto
                {
                    SubmissionId = submission.SubmissionId,
                    FormId = submission.FormId,
                    FormName = submission.Form.Name,
                    SubmittedDate = submission.SubmittedDate,
                    Status = submission.Status,
                    SubmittedBy = submission.SubmittedBy,

                    // Mandatory fields for quick access
                    Id = values.ContainsKey("id") ? values["id"] : submission.SubmissionId.ToString(),
                    ReferenceNo = values.ContainsKey("referenceno") ? values["referenceno"] : "",
                    CustomerName = values.ContainsKey("customername") ? values["customername"] : "",
                    PhoneNumber = values.ContainsKey("phonenumber") ? values["phonenumber"] : "",
                    Salary = values.ContainsKey("salary") ? values["salary"] : "",
                    MonthlySpent = values.ContainsKey("monthlyspent") ? values["monthlyspent"] : "",
                    FormStatus = values.ContainsKey("status") ? values["status"] : submission.Status,
                    CreationDate = values.ContainsKey("creationdate") ? values["creationdate"] : submission.SubmittedDate.ToString("yyyy-MM-dd"),

                    Preview = CreateSubmissionPreview(submission.FormSubmissionValues)
                };
            });

            return new PagedResult<FormSubmissionSummaryDto>
            {
                Items = summaryItems,
                TotalCount = totalCount,
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
                Values = submission.FormSubmissionValues
                    .OrderBy(v => GetFieldDisplayOrder(v.FieldNameAtSubmission))
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
            var submission = await _context.FormSubmissions.FindAsync(submissionId);

            if (submission == null)
            {
                return false;
            }

            submission.Status = status;

            // Also update the status field in submission values if it exists
            var statusValue = await _context.FormSubmissionValues
                .FirstOrDefaultAsync(v => v.SubmissionId == submissionId &&
                                          v.FieldNameAtSubmission.ToLower() == "status");

            if (statusValue != null)
            {
                statusValue.FieldValue = status;
            }

            await _context.SaveChangesAsync();

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

            // Also update the status field in submission values if it exists
            var statusValue = await _context.FormSubmissionValues
                .FirstOrDefaultAsync(v => v.SubmissionId == submissionId &&
                                          v.FieldNameAtSubmission.ToLower() == "status");

            if (statusValue != null)
            {
                statusValue.FieldValue = "محذوف";
            }

            await _context.SaveChangesAsync();

            return true;
        }

        private string CreateSubmissionPreview(ICollection<FormSubmissionValue> values)
        {
            // Create a preview from mandatory fields
            var customerName = values.FirstOrDefault(v =>
                v.FieldNameAtSubmission.ToLower() == "customername")?.FieldValue ?? "";

            var referenceNo = values.FirstOrDefault(v =>
                v.FieldNameAtSubmission.ToLower() == "referenceno")?.FieldValue ?? "";

            var preview = new List<string>();

            if (!string.IsNullOrEmpty(customerName))
            {
                preview.Add(customerName);
            }

            if (!string.IsNullOrEmpty(referenceNo))
            {
                preview.Add($"المرجع: {referenceNo}");
            }

            return string.Join(" - ", preview.Take(2));
        }

        private int GetFieldDisplayOrder(string fieldName)
        {
            // Order mandatory fields first, then others
            return fieldName.ToLower() switch
            {
                "id" => 1,
                "referenceno" => 2,
                "customername" => 3,
                "phonenumber" => 4,
                "salary" => 5,
                "monthlyspent" => 6,
                "status" => 7,
                "creationdate" => 8,
                _ => 100 // Other fields come after mandatory ones
            };
        }
    }
}