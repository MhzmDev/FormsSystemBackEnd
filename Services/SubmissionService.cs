using DynamicForm.Data;
using DynamicForm.Models;
using DynamicForm.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using static Azure.Core.HttpHeader;

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
            string? status,
            bool? isActive)
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
                RejectionReason = submission.RejectionReason,
                RejectionReasonEn = submission.RejectionReasonEn,
                Preview = CreateSubmissionPreview(submission.FormSubmissionValues),
                Values = CreateSubmissionValueSummary(submission.FormSubmissionValues)
            });

            return new PagedResult<FormSubmissionSummaryDto>
            {
                TotalCount = totalCount,
                Items = summaryItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        public async Task<PagedResult<FormSubmissionSummaryDto>> GetSubmissionsByFormIdAsync(
            int formId,
            int page,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            string? status)
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
                RejectionReason = submission.RejectionReason,
                RejectionReasonEn = submission.RejectionReasonEn,
                Preview = CreateSubmissionPreview(submission.FormSubmissionValues),
                Values = CreateSubmissionValueSummary(submission.FormSubmissionValues)
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

        public async Task<PagedResult<FormSubmissionSummaryDto>> GetActiveFormSubmissionsAsync(
            int page,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            string? status)
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
                RejectionReason = submission.RejectionReason,
                RejectionReasonEn = submission.RejectionReasonEn,
                Preview = CreateSubmissionPreview(submission.FormSubmissionValues),
                Values = CreateSubmissionValueSummary(submission.FormSubmissionValues)
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
            var submission = await _context.FormSubmissions.FindAsync(submissionId);

            if (submission == null)
            {
                return false;
            }

            submission.Status = status;
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
            await _context.SaveChangesAsync();

            return true;
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
                // New Mandatroy Fields
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