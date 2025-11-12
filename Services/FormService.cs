using Azure;
using DynamicForm.Data;
using DynamicForm.Models;
using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DynamicForm.Services
{
    public class FormService : IFormService
    {
        private readonly IApprovalService _approvalService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormService> _logger;
        private readonly ISubmissionService _submissionService;
        private readonly IWhatsAppService _whatsAppService;

        public FormService(ApplicationDbContext context, IApprovalService approvalService, IWhatsAppService whatsAppService, ILogger<FormService> logger, ISubmissionService submissionService)
        {
            _context = context;
            _approvalService = approvalService;
            _whatsAppService = whatsAppService;
            _logger = logger;
            _submissionService = submissionService;
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

            // Pre-validation before saving to database
            await ValidateSubmissionDataAsync(submitFormDto.Values);

            var submission = new FormSubmission
            {
                FormId = formId,
                SubmittedBy = submitFormDto.SubmittedBy,
                SubmittedDate = DateTime.UtcNow,
                Status = "قيد المراجعة" // Initial status before approval processing
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
                        SubmissionId = submission.SubmissionId,
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

            // Process approval with comprehensive validation
            var approvalResult = await _approvalService.ProcessApprovalAsync(submissionValues);

            // Update submission with approval result
            submission.Status = approvalResult.Status;
            submission.RejectionReason = approvalResult.RejectionReason;
            submission.RejectionReasonEn = approvalResult.RejectionReasonEn;

            await _context.SaveChangesAsync(); // Save the updated status

            // Send WhatsApp message only if approved
            if (approvalResult.Status == "مقبول")
            {
                try
                {
                    await SendApprovalWhatsAppMessageAsync(submission, submissionValues);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send WhatsApp message for submission {SubmissionId}", submission.SubmissionId);
                }
            }

            return await _submissionService.GetSubmissionByIdAsync(submission.SubmissionId) ??
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

        private async Task ValidateSubmissionDataAsync(Dictionary<string, string> values)
        {
            var validationErrors = new List<string>();

            // Validate phone number format
            if (values.TryGetValue("phoneNumber", out var phoneNumber))
            {
                try
                {
                    _whatsAppService.ValidateAndFormatPhoneNumber(phoneNumber);
                }
                catch (ArgumentException ex)
                {
                    validationErrors.Add($"رقم الجوال غير صحيح: {ex.Message}");
                }
            }

            // Validate birth date and age
            if (values.TryGetValue("birthDate", out var birthDateStr))
            {
                if (DateTime.TryParseExact(birthDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
                {
                    var age = CalculateAge(birthDate);

                    if (age < 20)
                    {
                        validationErrors.Add($"العمر ({age} سنة) أقل من الحد الأدنى المطلوب (20 سنة)");
                    }

                    // Check age consistency if age field exists
                    if (values.TryGetValue("age", out var ageStr) && int.TryParse(ageStr, out var providedAge))
                    {
                        if (Math.Abs(age - providedAge) > 1)
                        {
                            validationErrors.Add($"العمر المدخل ({providedAge} سنة) لا يتطابق مع تاريخ الميلاد (العمر الفعلي: {age} سنة)");
                        }
                    }
                }
                else
                {
                    validationErrors.Add("تاريخ الميلاد غير صحيح. يجب أن يكون بالصيغة: YYYY-MM-DD");
                }
            }

            // Validate numeric fields
            if (values.TryGetValue("monthlySalary", out var salaryStr))
            {
                if (!decimal.TryParse(salaryStr, out var salary) || salary <= 0)
                {
                    validationErrors.Add("الراتب الشهري يجب أن يكون رقم صحيح أكبر من صفر");
                }
            }

            if (values.TryGetValue("monthlyCommitments", out var commitmentsStr))
            {
                if (!decimal.TryParse(commitmentsStr, out var commitments) || commitments < 0)
                {
                    validationErrors.Add("الالتزامات الشهرية يجب أن تكون رقم صحيح لا يقل عن صفر");
                }
            }

            if (validationErrors.Any())
            {
                throw new ArgumentException($"أخطاء في البيانات المدخلة: {string.Join(", ", validationErrors)}");
            }

            await Task.CompletedTask;
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;

            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        private async Task SendApprovalWhatsAppMessageAsync(FormSubmission submission, List<FormSubmissionValue> submissionValues)
        {
            try
            {
                _logger.LogInformation("Attempting to send WhatsApp message for approved submission {SubmissionId}", submission.SubmissionId);

                // Get submission data
                var phoneValue = submissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("phone") ||
                    v.FieldNameAtSubmission.ToLower().Contains("mobile") ||
                    v.LabelAtSubmission.Contains("هاتف") ||
                    v.LabelAtSubmission.Contains("جوال"));

                var fullNameValue = submissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("name") ||
                    v.FieldNameAtSubmission.ToLower().Contains("fullname") ||
                    v.LabelAtSubmission.Contains("اسم"));

                if (phoneValue == null || string.IsNullOrWhiteSpace(phoneValue.FieldValue))
                {
                    _logger.LogWarning("Phone number not found for submission {SubmissionId}", submission.SubmissionId);

                    return;
                }

                var phoneNumber = phoneValue.FieldValue;
                var fullName = fullNameValue?.FieldValue ?? "عميل محزم";

                // Create subscriber in Morasalaty with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var subscriberCreated = await _whatsAppService.CreateSubscriberAsync(phoneNumber, fullName);

                if (!subscriberCreated)
                {
                    _logger.LogWarning("Failed to create subscriber for {Phone}, but continuing with template send", phoneNumber);
                }

                //// Add small delay between API calls
                //await Task.Delay(1000, cts.Token);

                // Prepare template parameters
                var nationalIdValue = submissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("nationalid") ||
                    v.LabelAtSubmission.Contains("هوية"));

                var birthDateValue = submissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("birth") ||
                    v.LabelAtSubmission.Contains("ميلاد"));

                var salaryValue = submissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("salary") ||
                    v.LabelAtSubmission.Contains("راتب"));

                var commitmentsValue = submissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("commitment") ||
                    v.LabelAtSubmission.Contains("التزام"));

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
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("WhatsApp message sending timed out for submission {SubmissionId}", submission.SubmissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp approval message for submission {SubmissionId}", submission.SubmissionId);
            }
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
                    FieldName = "fullName",
                    FieldType = "text",
                    Label = "الاسم الثلاثي",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 1,
                    IsActive = true
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "phoneNumber",
                    FieldType = "number",
                    Label = "رقم الجوال",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 2,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new
                    {
                        type = "phone",
                        pattern = @"^(966[5][0-9]{8}|20[1][0-9]{8,9})$",
                        message = "يجب إدخال رقم جوال صحيح (السعودية: 966xxxxxxxxx أو مصر: 20xxxxxxxxxx)"
                    })
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "birthDate",
                    FieldType = "date",
                    Label = "تاريخ الميلاد",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 3,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new
                    {
                        type = "date",
                        maxDate = DateTime.Now.AddYears(-20).ToString("yyyy-MM-dd"),
                        message = "يجب أن يكون العمر 20 سنة أو أكثر"
                    })
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "citizenshipStatus",
                    FieldType = "dropdown",
                    Label = "مواطن أو مقيم",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 4,
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
                    DisplayOrder = startingDisplayOrder + 5,
                    IsActive = true,
                    Options = JsonSerializer.Serialize(mortgageOptions)
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "monthlySalary",
                    FieldType = "number",
                    Label = "الراتب الشهري",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 6,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new
                    {
                        type = "number",
                        min = 1,
                        step = 1,
                        message = "يجب إدخال راتب شهري صحيح (أرقام فقط)"
                    })
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "monthlyCommitments",
                    FieldType = "number",
                    Label = "الالتزامات الشهرية",
                    IsRequired = true,
                    DisplayOrder = startingDisplayOrder + 7,
                    IsActive = true,
                    ValidationRules = JsonSerializer.Serialize(new
                    {
                        type = "number",
                        min = 0,
                        step = 1,
                        message = "يجب إدخال التزامات شهرية صحيحة (أرقام فقط)"
                    })
                }
            };

            return mandatoryFields;
        }
    }
}