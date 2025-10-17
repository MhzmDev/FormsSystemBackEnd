using Microsoft.EntityFrameworkCore;
using DynamicForm.Data;
using DynamicForm.Models;
using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using System.Text.Json;

namespace DynamicForm.Services
{
    public class FormService : IFormService
    {
        private readonly ApplicationDbContext _context;

        public FormService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FormDto?> GetFormByIdAsync(int formId)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .FirstOrDefaultAsync(f => f.FormId == formId);

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
                    IsActive = f.IsActive,
                    DisplayOrder = f.DisplayOrder,
                    Options = !string.IsNullOrEmpty(f.Options) ? JsonSerializer.Deserialize<List<string>>(f.Options) : null
                }).ToList()
            };
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
                    IsActive = f.IsActive,
                    DisplayOrder = f.DisplayOrder,
                    Options = !string.IsNullOrEmpty(f.Options) ? JsonSerializer.Deserialize<List<string>>(f.Options) : null
                }).ToList()
            };
        }

        public async Task<FormDto> CreateFormAsync(CreateFormDto createFormDto)
        {
            // Deactivate all existing forms (only one can be active)
            var existingForms = await _context.Forms.Where(f => f.IsActive).ToListAsync();

            foreach (var existingForm in existingForms)
            {
                existingForm.IsActive = false;
                existingForm.ModifiedDate = DateTime.UtcNow;
            }

            var form = new Form
            {
                Name = createFormDto.Name,
                Description = createFormDto.Description,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Forms.Add(form);
            await _context.SaveChangesAsync();

            // Add mandatory fields first
            var mandatoryFields = GetMandatoryFields();

            foreach (var field in mandatoryFields)
            {
                field.FormId = form.FormId;
                _context.FormFields.Add(field);
            }

            // Add custom fields
            foreach (var fieldDto in createFormDto.Fields)
            {
                var field = new FormField
                {
                    FormId = form.FormId,
                    FieldName = fieldDto.FieldName,
                    FieldType = fieldDto.FieldType,
                    Label = fieldDto.Label,
                    IsRequired = fieldDto.IsRequired,
                    DisplayOrder = fieldDto.DisplayOrder + 8, // After mandatory fields
                    Options = fieldDto.Options != null ? JsonSerializer.Serialize(fieldDto.Options) : null,
                    IsActive = true
                };

                _context.FormFields.Add(field);
            }

            await _context.SaveChangesAsync();

            return await GetFormByIdAsync(form.FormId) ?? throw new InvalidOperationException("Failed to retrieve created form");
        }

        public async Task<FormDto?> UpdateFormAsync(int formId, UpdateFormDto updateFormDto)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            if (form == null)
            {
                return null;
            }

            // Update form details
            form.Name = updateFormDto.Name;
            form.Description = updateFormDto.Description;
            form.ModifiedDate = DateTime.UtcNow;

            // Get mandatory field names
            var mandatoryFieldNames = GetMandatoryFieldNames();

            // Only update non-mandatory fields
            var customFields = form.FormFields.Where(f => !mandatoryFieldNames.Contains(f.FieldName)).ToList();

            // Deactivate existing custom fields
            foreach (var field in customFields)
            {
                field.IsActive = false;
            }

            // Add updated custom fields
            foreach (var fieldDto in updateFormDto.Fields)
            {
                if (!mandatoryFieldNames.Contains(fieldDto.FieldName))
                {
                    var field = new FormField
                    {
                        FormId = form.FormId,
                        FieldName = fieldDto.FieldName,
                        FieldType = fieldDto.FieldType,
                        Label = fieldDto.Label,
                        IsRequired = fieldDto.IsRequired,
                        DisplayOrder = fieldDto.DisplayOrder + 8,
                        Options = fieldDto.Options != null ? JsonSerializer.Serialize(fieldDto.Options) : null,
                        IsActive = true
                    };

                    _context.FormFields.Add(field);
                }
            }

            await _context.SaveChangesAsync();

            return await GetFormByIdAsync(formId);
        }

        public async Task<bool> DeleteFormAsync(int formId)
        {
            var form = await _context.Forms.FindAsync(formId);

            if (form == null)
            {
                return false;
            }

            // Soft delete
            form.IsActive = false;
            form.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<FormSubmissionResponseDto> SubmitFormAsync(int formId, SubmitFormDto submitFormDto)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            if (form == null)
            {
                throw new ArgumentException("Form not found or not active");
            }

            var submission = new FormSubmission
            {
                FormId = formId,
                SubmittedDate = DateTime.UtcNow,
                SubmittedBy = submitFormDto.SubmittedBy,
                Status = "مرسل"
            };

            _context.FormSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            // Auto-generate mandatory values if not provided
            var values = new Dictionary<string, string>(submitFormDto.Values);

            if (!values.ContainsKey("id"))
            {
                values["id"] = submission.SubmissionId.ToString();
            }

            if (!values.ContainsKey("referenceNo"))
            {
                values["referenceNo"] = $"REF-{DateTime.UtcNow:yyyyMMdd}-{submission.SubmissionId:D6}";
            }

            if (!values.ContainsKey("creationDate"))
            {
                values["creationDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }

            if (!values.ContainsKey("status"))
            {
                values["status"] = "مرسل";
            }

            // Create submission values
            foreach (var field in form.FormFields)
            {
                var fieldValue = values.ContainsKey(field.FieldName) ? values[field.FieldName] : "";

                var submissionValue = new FormSubmissionValue
                {
                    SubmissionId = submission.SubmissionId,
                    FieldId = field.FieldId,
                    FieldValue = fieldValue,
                    FieldNameAtSubmission = field.FieldName,
                    FieldTypeAtSubmission = field.FieldType,
                    LabelAtSubmission = field.Label,
                    OptionsAtSubmission = field.Options,
                    CreatedDate = DateTime.UtcNow
                };

                _context.FormSubmissionValues.Add(submissionValue);
            }

            await _context.SaveChangesAsync();

            // Return the submission
            return new FormSubmissionResponseDto
            {
                SubmissionId = submission.SubmissionId,
                FormName = form.Name,
                SubmittedDate = submission.SubmittedDate,
                Status = submission.Status,
                SubmittedBy = submission.SubmittedBy,
                Values = form.FormFields.OrderBy(f => f.DisplayOrder).Select(f =>
                {
                    var value = values.ContainsKey(f.FieldName) ? values[f.FieldName] : "";

                    return new FieldValueDto
                    {
                        FieldName = f.FieldName,
                        Label = f.Label,
                        Value = value,
                        FieldType = f.FieldType
                    };
                }).ToList()
            };
        }

        // New method: Activate a specific form (deactivates all others)
        public async Task<bool> ActivateFormAsync(int formId)
        {
            var targetForm = await _context.Forms.FindAsync(formId);

            if (targetForm == null)
            {
                return false;
            }

            // Deactivate all forms
            var allForms = await _context.Forms.ToListAsync();

            foreach (var form in allForms)
            {
                form.IsActive = false;
                form.ModifiedDate = DateTime.UtcNow;
            }

            // Activate the target form
            targetForm.IsActive = true;
            targetForm.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        // Fixed: Now returns ALL forms (both active and inactive)
        public async Task<IEnumerable<FormDto>> GetAllFormsAsync()
        {
            var forms = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            return forms.Select(form => new FormDto
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
        }

        public async Task<FormFieldDto?> UpdateFormFieldAsync(int formId, int fieldId, UpdateFormFieldDto updateFieldDto)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormId == formId );

            if (form == null)
            {
                return null;
            }

            var field = form.FormFields.FirstOrDefault(f => f.FieldId == fieldId );

            if (field == null)
            {
                return null;
            }

            // Check if this is a mandatory field - cannot be updated
            var mandatoryFieldNames = GetMandatoryFieldNames();

            if (mandatoryFieldNames.Contains(field.FieldName))
            {
                throw new InvalidOperationException($"Cannot update mandatory field: {field.FieldName}");
            }

            // Update the field properties
            field.Label = updateFieldDto.Label;
            field.IsRequired = updateFieldDto.IsRequired;
            field.DisplayOrder = updateFieldDto.DisplayOrder + 8; // Keep after mandatory fields
            field.Options = updateFieldDto.Options != null ? JsonSerializer.Serialize(updateFieldDto.Options) : null;
            field.ValidationRules = updateFieldDto.ValidationRules;

            await _context.SaveChangesAsync();

            return new FormFieldDto
            {
                FieldId = field.FieldId,
                FieldName = field.FieldName,
                FieldType = field.FieldType,
                Label = field.Label,
                IsRequired = field.IsRequired,
                DisplayOrder = field.DisplayOrder,
                Options = !string.IsNullOrEmpty(field.Options) ? JsonSerializer.Deserialize<List<string>>(field.Options) : null
            };
        }

        public async Task<bool> DeleteFormFieldAsync(int formId, int fieldId)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            if (form == null)
            {
                return false;
            }

            var field = form.FormFields.FirstOrDefault(f => f.FieldId == fieldId && f.IsActive);

            if (field == null)
            {
                return false;
            }

            // Check if this is a mandatory field - cannot be deleted
            var mandatoryFieldNames = GetMandatoryFieldNames();

            if (mandatoryFieldNames.Contains(field.FieldName))
            {
                throw new InvalidOperationException($"Cannot delete mandatory field: {field.FieldName}");
            }

            // Soft delete
            field.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<FormFieldDto> AddFormFieldAsync(int formId, CreateFormFieldDto createFieldDto)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            if (form == null)
            {
                throw new ArgumentException("Form not found or not active");
            }

            // Check if field name already exists
            if (form.FormFields.Any(f => f.FieldName == createFieldDto.FieldName && f.IsActive))
            {
                throw new InvalidOperationException($"Field with name '{createFieldDto.FieldName}' already exists");
            }

            // Check if this is trying to add a mandatory field name
            var mandatoryFieldNames = GetMandatoryFieldNames();

            if (mandatoryFieldNames.Contains(createFieldDto.FieldName))
            {
                throw new InvalidOperationException($"Cannot add field with mandatory field name: {createFieldDto.FieldName}");
            }

            var field = new FormField
            {
                FormId = formId,
                FieldName = createFieldDto.FieldName,
                FieldType = createFieldDto.FieldType,
                Label = createFieldDto.Label,
                IsRequired = createFieldDto.IsRequired,
                DisplayOrder = createFieldDto.DisplayOrder + 8, // After mandatory fields
                Options = createFieldDto.Options != null ? JsonSerializer.Serialize(createFieldDto.Options) : null,
                IsActive = true
            };

            _context.FormFields.Add(field);
            await _context.SaveChangesAsync();

            return new FormFieldDto
            {
                FieldId = field.FieldId,
                FieldName = field.FieldName,
                FieldType = field.FieldType,
                Label = field.Label,
                IsRequired = field.IsRequired,
                DisplayOrder = field.DisplayOrder,
                Options = !string.IsNullOrEmpty(field.Options) ? JsonSerializer.Deserialize<List<string>>(field.Options) : null
            };
        }

        private List<FormField> GetMandatoryFields()
        {
            return new List<FormField>
            {
                new() { FieldName = "id", FieldType = "number", Label = "المعرف", IsRequired = true, DisplayOrder = 1, IsActive = true },
                new() { FieldName = "referenceNo", FieldType = "text", Label = "رقم المرجع", IsRequired = true, DisplayOrder = 2, IsActive = true },
                new() { FieldName = "customerName", FieldType = "text", Label = "اسم العميل", IsRequired = true, DisplayOrder = 3, IsActive = true },
                new() { FieldName = "phoneNumber", FieldType = "text", Label = "رقم الهاتف", IsRequired = true, DisplayOrder = 4, IsActive = true },
                new() { FieldName = "salary", FieldType = "text", Label = "الراتب", IsRequired = true, DisplayOrder = 5, IsActive = true },
                new() { FieldName = "monthlySpent", FieldType = "text", Label = "الالتزامات الشهريه", IsRequired = true, DisplayOrder = 6, IsActive = true },
                new() { FieldName = "status", FieldType = "dropdown", Label = "الحالة", IsRequired = true, DisplayOrder = 7, IsActive = true, Options = JsonSerializer.Serialize(new[] { "جديد", "قيد المراجعة", "مقبول", "مرفوض", "مكتمل" }) },
                new() { FieldName = "creationDate", FieldType = "date", Label = "تاريخ الإنشاء", IsRequired = true, DisplayOrder = 8, IsActive = true }
            };
        }

        private HashSet<string> GetMandatoryFieldNames()
        {
            return new HashSet<string> { "id", "referenceNo", "customerName", "phoneNumber", "salary", "monthlySpent", "status", "creationDate" };
        }
    }
}