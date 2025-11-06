using Azure;
using DynamicForm.Data;
using DynamicForm.Models;
using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Text.Json;

namespace DynamicForm.Services
{
    public class FormService : IFormService
    {
        private readonly IApprovalService _approvalService;
        private readonly ApplicationDbContext _context;

        public FormService(ApplicationDbContext context, IApprovalService approvalService)
        {
            _context = context;
            _approvalService = approvalService;
        }

        public async Task<FormDto?> GetFormByIdAsync(int formId)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            if (form == null)
            {
                return null;
            }

            return new FormDto
            {
                FormId = form.FormId,
                Name = form.Name,
                Description = form.Description,
                IsActive = form.IsActive,
                CreatedDate = form.CreatedDate,
                Fields = form.FormFields.OrderBy(f => f.DisplayOrder).Select(f => new FormFieldDto
                {
                    FieldId = f.FieldId,
                    FieldName = f.FieldName,
                    FieldType = f.FieldType,
                    Label = f.Label,
                    IsRequired = f.IsRequired,
                    DisplayOrder = f.DisplayOrder,
                    Options = !string.IsNullOrEmpty(f.Options) ? JsonSerializer.Deserialize<List<string>>(f.Options) : null
                }).ToList()
            };
        }

        public async Task<PagedResult<FormDto>> GetAllFormsAsync(int pageIndex, int pageSize, bool? isActive)
        {
            var query = _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .AsQueryable();

            // apply active filter if specified.
            if (isActive.HasValue)
            {
                query = query.Where(f => f.IsActive == isActive.Value);
            }

            // get total count for pagination
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // apply pagination
            var forms = await query
                .OrderByDescending(f => f.CreatedDate)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            //var forms = await _context.Forms
            //    .Include(f => f.FormFields.Where(ff => ff.IsActive))
            //    .OrderByDescending(f => f.CreatedDate)
            //    .ToListAsync();

            var formDtos = forms.Select(form => new FormDto
            {
                FormId = form.FormId,
                Name = form.Name,
                Description = form.Description,
                IsActive = form.IsActive,
                CreatedDate = form.CreatedDate,
                Fields = form.FormFields.OrderBy(f => f.DisplayOrder).Select(f => new FormFieldDto
                {
                    FieldId = f.FieldId,
                    FieldName = f.FieldName,
                    FieldType = f.FieldType,
                    Label = f.Label,
                    IsRequired = f.IsRequired,
                    DisplayOrder = f.DisplayOrder,
                    Options = !string.IsNullOrEmpty(f.Options) ? JsonSerializer.Deserialize<List<string>>(f.Options) : null
                }).ToList()
            });

            return new PagedResult<FormDto>
            {
                Items = formDtos,
                TotalCount = totalCount,
                Page = pageIndex,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = pageIndex < totalPages,
                HasPreviousPage = pageIndex > 1
            };
        }

        public async Task<FormDto> CreateFormAsync(CreateFormDto createFormDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Deactivate all existing forms
                var existingForms = await _context.Forms.Where(f => f.IsActive).ToListAsync();

                foreach (var existingForm in existingForms)
                {
                    existingForm.IsActive = false;
                    existingForm.ModifiedDate = DateTime.UtcNow;
                }

                // Create new active form
                var form = new Form
                {
                    Name = createFormDto.Name,
                    Description = createFormDto.Description,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "المستخدم",
                    IsActive = true
                };

                _context.Forms.Add(form);
                await _context.SaveChangesAsync();

                // Calculate starting display order for mandatory fields
                var maxDisplayOrder = createFormDto.Fields?.Max(f => f.DisplayOrder) ?? 0;

                // Add mandatory fields first
                var mandatoryFields = await CreateMandatoryFieldsAsync(form.FormId, maxDisplayOrder + 1);
                _context.FormFields.AddRange(mandatoryFields);

                foreach (var fieldDto in createFormDto.Fields!)
                {
                    var field = new FormField
                    {
                        FormId = form.FormId,
                        FieldName = fieldDto.FieldName,
                        FieldType = fieldDto.FieldType,
                        Label = fieldDto.Label,
                        IsRequired = fieldDto.IsRequired,
                        DisplayOrder = fieldDto.DisplayOrder,
                        Options = fieldDto.Options != null ? JsonSerializer.Serialize(fieldDto.Options) : null,
                        IsActive = true
                    };

                    _context.FormFields.Add(field);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetFormByIdAsync(form.FormId) ?? throw new InvalidOperationException("فشل في إنشاء النموذج");
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        public async Task<FormDto?> UpdateFormAsync(int formId, UpdateFormDto updateFormDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var form = await _context.Forms
                    .Include(f => f.FormFields)
                    .FirstOrDefaultAsync(f => f.FormId == formId);

                if (form == null)
                {
                    return null;
                }

                form.Name = updateFormDto.Name;
                form.Description = updateFormDto.Description;
                form.ModifiedDate = DateTime.UtcNow;

                var existingFields = form.FormFields.ToList();

                foreach (var existingField in existingFields)
                {
                    var updatedField = updateFormDto.Fields.FirstOrDefault(f => f.FieldName == existingField.FieldName);

                    if (updatedField != null)
                    {
                        existingField.Label = updatedField.Label;
                        existingField.FieldType = updatedField.FieldType;
                        existingField.IsRequired = updatedField.IsRequired;
                        existingField.DisplayOrder = updatedField.DisplayOrder;
                        existingField.Options = updatedField.Options != null ? JsonSerializer.Serialize(updatedField.Options) : null;
                        existingField.IsActive = true;
                    }
                    else
                    {
                        existingField.IsActive = false;
                    }
                }

                var existingFieldNames = existingFields.Select(f => f.FieldName).ToHashSet();
                var newFields = updateFormDto.Fields.Where(f => !existingFieldNames.Contains(f.FieldName));

                foreach (var newFieldDto in newFields)
                {
                    var newField = new FormField
                    {
                        FormId = form.FormId,
                        FieldName = newFieldDto.FieldName,
                        FieldType = newFieldDto.FieldType,
                        Label = newFieldDto.Label,
                        IsRequired = newFieldDto.IsRequired,
                        DisplayOrder = newFieldDto.DisplayOrder,
                        Options = newFieldDto.Options != null ? JsonSerializer.Serialize(newFieldDto.Options) : null,
                        IsActive = true
                    };

                    _context.FormFields.Add(newField);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetFormByIdAsync(formId);
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        public async Task<bool> DeleteFormAsync(int formId)
        {
            var form = await _context.Forms.FindAsync(formId);

            if (form == null)
            {
                return false;
            }

            form.IsActive = false;
            form.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<FormSubmissionResponseDto> SubmitFormAsync(int formId, SubmitFormDto submitFormDto)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            if (form == null)
            {
                throw new ArgumentException("النموذج غير موجود أو غير نشط");
            }

            var requiredFields = form.FormFields.Where(f => f.IsRequired && f.IsActive).ToList();

            var missingFields = requiredFields
                .Where(f => !submitFormDto.Values.ContainsKey(f.FieldName) ||
                            string.IsNullOrWhiteSpace(submitFormDto.Values[f.FieldName]))
                .Select(f => f.Label)
                .ToList();

            if (missingFields.Any())
            {
                throw new ArgumentException($"الحقول التالية مطلوبة: {string.Join(", ", missingFields)}");
            }

            var submission = new FormSubmission
            {
                FormId = formId,
                SubmittedBy = submitFormDto.SubmittedBy,
                SubmittedDate = DateTime.UtcNow,
                Status = "مُرسل" // Will be updated by the approval service later
            };

            _context.FormSubmissions.Add(submission);
            await _context.SaveChangesAsync(); // Save first to get the SubmissionId

            // Now create submission values with the correct SubmissionId
            var submissionValues = new List<FormSubmissionValue>();

            foreach (var value in submitFormDto.Values)
            {
                var field = form.FormFields.FirstOrDefault(f => f.FieldName == value.Key);

                if (field != null)
                {
                    var submissionValue = new FormSubmissionValue
                    {
                        SubmissionId = submission.SubmissionId, // Now this has a valid value
                        FieldId = field.FieldId,
                        FieldValue = value.Value,
                        FieldNameAtSubmission = field.FieldName,
                        FieldTypeAtSubmission = field.FieldType,
                        LabelAtSubmission = field.Label,
                        OptionsAtSubmission = field.Options,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.FormSubmissionValues.Add(submissionValue);
                    submissionValues.Add(submissionValue);
                }
            }

            await _context.SaveChangesAsync(); // Save the submission values

            // Process approval automatically
            var approvalResult = await _approvalService.ProcessApprovalAsync(submissionValues);

            // Update submission with approval result
            submission.Status = approvalResult.Status;
            submission.RejectionReason = approvalResult.RejectionReason;
            submission.RejectionReasonEn = approvalResult.RejectionReasonEn;

            await _context.SaveChangesAsync(); // Save the updated status

            var submissionService = new SubmissionService(_context);

            return await submissionService.GetSubmissionByIdAsync(submission.SubmissionId) ??
                   throw new InvalidOperationException("فشل في حفظ البيانات");
        }

        public async Task<FormDto?> ActivateFormAsync(int formId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Deactivate all existing forms
                var existingForms = await _context.Forms.Where(f => f.IsActive).ToListAsync();

                foreach (var existingForm in existingForms)
                {
                    existingForm.IsActive = false;
                    existingForm.ModifiedDate = DateTime.UtcNow;
                }

                // Activate the specified form
                var form = await _context.Forms.FindAsync(formId);

                if (form == null)
                {
                    return null;
                }

                form.IsActive = true;
                form.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetFormByIdAsync(formId);
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        public async Task<FormDto?> GetActiveFormAsync()
        {
            var form = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .FirstOrDefaultAsync(f => f.IsActive);

            if (form == null)
            {
                return null;
            }

            return new FormDto
            {
                FormId = form.FormId,
                Name = form.Name,
                Description = form.Description,
                IsActive = form.IsActive,
                CreatedDate = form.CreatedDate,
                Fields = form.FormFields.OrderBy(f => f.DisplayOrder).Select(f => new FormFieldDto
                {
                    FieldId = f.FieldId,
                    FieldName = f.FieldName,
                    FieldType = f.FieldType,
                    Label = f.Label,
                    IsRequired = f.IsRequired,
                    DisplayOrder = f.DisplayOrder,
                    Options = !string.IsNullOrEmpty(f.Options) ? JsonSerializer.Deserialize<List<string>>(f.Options) : null
                }).ToList()
            };
        }

        private async Task<List<FormField>> CreateMandatoryFieldsAsync(int formId, int startingDisplayOrder)
        {
            var citizenshipOptions = new List<string> { "مواطن", "مقيم" };
            var mortgageOptions = new List<string> { "نعم", "لا" };

            var mandatoryFields = new List<FormField>
            {
                new FormField
                {
                    FormId = formId,
                    FieldName = "citizenshipStatus",
                    FieldType = "dropdown",
                    Label = "مواطن أو مقيم",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder,
                    IsActive = true,
                    Options = JsonSerializer.Serialize(citizenshipOptions)
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "hasMortgage",
                    FieldType = "dropdown",
                    Label = "قرض عقاري",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 1,
                    IsActive = true,
                    Options = JsonSerializer.Serialize(mortgageOptions)
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "monthlySalary",
                    FieldType = "text",
                    Label = "الراتب الشهري",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 2,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { type = "number", min = 0 })
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "monthlyCommitments",
                    FieldType = "text",
                    Label = "الالتزامات الشهرية",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 3,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new { type = "number", min = 0 })
                }
            };

            return mandatoryFields;
        }
    }
}