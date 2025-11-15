using Azure;
using DynamicForm.Data;
using DynamicForm.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using DynamicForm.Models.Entities;

namespace DynamicForm.Services
{
    public class FormService : IFormService
    {
        private const string ActiveFormCacheKey = "ActiveForm";
        private readonly IApprovalService _approvalService;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _context;
        private readonly IFieldValidationService _fieldValidationService;
        private readonly ILogger<FormService> _logger;
        private readonly INotificationService _notificationService; // NEW
        private readonly ISubmissionService _submissionService;
        private readonly IWhatsAppService _whatsAppService;

        public FormService(ApplicationDbContext context, IApprovalService approvalService,
            IWhatsAppService whatsAppService, ILogger<FormService> logger, ISubmissionService submissionService,
            IFieldValidationService fieldValidationService, INotificationService notificationService, // NEW
            IMemoryCache cache) // ✅ Add cache
        {
            _context = context;
            _approvalService = approvalService;
            _whatsAppService = whatsAppService;
            _logger = logger;
            _submissionService = submissionService;
            _fieldValidationService = fieldValidationService;
            _notificationService = notificationService; // NEW
            _cache = cache;
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
                    Options = !string.IsNullOrEmpty(f.Options)
                        ? JsonSerializer.Deserialize<List<string>>(f.Options)
                        : null,
                    ValidationRules = !string.IsNullOrEmpty(f.ValidationRules)
                        ? JsonSerializer.Deserialize<ValidationRuleDto>(f.ValidationRules)
                        : null
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
                    Options = !string.IsNullOrEmpty(f.Options)
                        ? JsonSerializer.Deserialize<List<string>>(f.Options)
                        : null,
                    ValidationRules = !string.IsNullOrEmpty(f.ValidationRules)
                        ? JsonSerializer.Deserialize<ValidationRuleDto>(f.ValidationRules)
                        : null
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
                var maxDisplayOrder = 8;

                // Add mandatory fields first
                var mandatoryFields = await CreateMandatoryFieldsAsync(form.FormId);
                _context.FormFields.AddRange(mandatoryFields);

                foreach (var fieldDto in createFormDto.Fields ?? Enumerable.Empty<CreateFormFieldDto>())
                {
                    var field = new FormField
                    {
                        FormId = form.FormId,
                        FieldName = fieldDto.FieldName,
                        FieldType = fieldDto.FieldType,
                        Label = fieldDto.Label,
                        IsRequired = fieldDto.IsRequired,
                        DisplayOrder = maxDisplayOrder++,
                        Options = fieldDto.Options != null ? JsonSerializer.Serialize(fieldDto.Options) : null,
                        IsActive = true,
                        ValidationRules = fieldDto.ValidationRules != null
                            ? JsonSerializer.Serialize(fieldDto.ValidationRules)
                            : null
                    };

                    _context.FormFields.Add(field);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _cache.Remove(ActiveFormCacheKey); // ✅ Clear cache

                return await GetFormByIdAsync(form.FormId) ??
                       throw new InvalidOperationException("فشل في إنشاء النموذج");
            } catch
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
                var updateFieldNames = updateFormDto.Fields.Select(f => f.FieldName).ToHashSet();

                // ✅ Define protected mandatory fields
                var mandatoryFieldNames = new HashSet<string>
                {
                    "fullName", "phoneNumber", "birthDate", "citizenshipStatus",
                    "hasMortgage", "monthlySalary", "monthlyCommitments"
                };

                // Update or reactivate existing fields
                foreach (var existingField in existingFields)
                {
                    var updatedField = updateFormDto.Fields.FirstOrDefault(f => f.FieldName == existingField.FieldName);

                    if (updatedField != null)
                    {
                        // ✅ Protect mandatory fields from destructive changes
                         if (mandatoryFieldNames.Contains(existingField.FieldName))
                        {
                            // Only allow label updates for mandatory fields
                            existingField.Label = updatedField.Label;

                            _logger.LogWarning(
                                "Attempted to modify mandatory field {FieldName}, only label was updated",
                                existingField.FieldName);
                        }
                        else
                        {
                            // ✅ Validate field type changes for non-mandatory fields
                            if (existingField.FieldType != updatedField.FieldType)
                            {
                                // Check if field has submissions
                                var hasSubmissions = await _context.FormSubmissionValues
                                    .AnyAsync(sv => sv.FieldId == existingField.FieldId);

                                if (hasSubmissions)
                                {
                                    _logger.LogWarning(
                                        "Field {FieldName} has existing submissions, type change from {OldType} to {NewType} may cause data inconsistency",
                                        existingField.FieldName, existingField.FieldType, updatedField.FieldType);
                                }
                            }

                            existingField.Label = updatedField.Label;
                            existingField.FieldType = updatedField.FieldType;
                            existingField.IsRequired = updatedField.IsRequired;
                            existingField.DisplayOrder = updatedField.DisplayOrder;

                            existingField.Options = updatedField.Options != null
                                ? JsonSerializer.Serialize(updatedField.Options)
                                : null;

                            existingField.ValidationRules = updatedField.ValidationRules != null
                                ? JsonSerializer.Serialize(updatedField.ValidationRules)
                                : null;
                        }

                        existingField.IsActive = true;
                        existingField.ValidationRules = updatedField.ValidationRules != null ? JsonSerializer.Serialize(updatedField.ValidationRules) : null;
                    }
                    else
                    {
                        // ✅ Prevent deactivating mandatory fields
                        if (mandatoryFieldNames.Contains(existingField.FieldName))
                        {
                            _logger.LogError(
                                "Attempted to deactivate mandatory field {FieldName}, update rejected",
                                existingField.FieldName);

                            throw new InvalidOperationException(
                                $"لا يمكن حذف أو تعطيل الحقل الإلزامي: {existingField.Label}");
                        }

                        existingField.IsActive = false;
                    }
                }

                // ✅ Validate display order uniqueness
                var displayOrders = updateFormDto.Fields
                    .GroupBy(f => f.DisplayOrder)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (displayOrders.Any())
                {
                    throw new InvalidOperationException(
                        $"خطأ: ترتيب العرض مكرر للأرقام التالية: {string.Join(", ", displayOrders)}");
                }

                // Add only truly new fields
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
                        Options = newFieldDto.Options != null
                            ? JsonSerializer.Serialize(newFieldDto.Options)
                            : null,
                        ValidationRules = newFieldDto.ValidationRules != null
                            ? JsonSerializer.Serialize(newFieldDto.ValidationRules)
                            : null,
                        IsActive = true
                    };

                    _context.FormFields.Add(newField);
                }

                // ✅ Final validation before commit
                var activeFieldsCount = form.FormFields.Count(f => f.IsActive);

                if (activeFieldsCount < mandatoryFieldNames.Count)
                {
                    throw new InvalidOperationException(
                        $"خطأ: يجب أن يحتوي النموذج على {mandatoryFieldNames.Count} حقول إلزامية على الأقل");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Form {FormId} updated successfully with {FieldCount} active fields",
                    formId, activeFieldsCount);

                return await GetFormByIdAsync(formId);
            } catch
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
            var form = await GetActiveFormOrThrowAsync(formId);
            ValidateRequiredFields(form, submitFormDto.Values);

            var validationResult = await ValidateSubmissionDataAsync(submitFormDto.Values);

            var submission = await CreateSubmissionAsync(formId, submitFormDto);

            var submissionValues = await CreateSubmissionValuesAsync(submission.SubmissionId, form, submitFormDto.Values);

            await ProcessSubmissionApprovalAsync(submission, submissionValues, validationResult);
            await _context.SaveChangesAsync();

            if (submission.Status == FormConstants.SubmissionStatus.Approved)
            {
                await _notificationService.SendApprovalNotificationAsync(submission, submissionValues);
            }

            return await _submissionService.GetSubmissionByIdAsync(submission.SubmissionId)
                   ?? throw new InvalidOperationException("فشل في حفظ البيانات");
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
            } catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        public async Task<FormDto?> GetActiveFormAsync()
        {
            return await _cache.GetOrCreateAsync(ActiveFormCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

                var form = await _context.Forms
                    .Include(f => f.FormFields.Where(ff => ff.IsActive))
                    .FirstOrDefaultAsync(f => f.IsActive);

                return form == null ? null : MapToFormDto(form);
            });
        }

        private async Task<Form> GetActiveFormOrThrowAsync(int formId)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields)
                .FirstOrDefaultAsync(f => f.FormId == formId && f.IsActive);

            return form ?? throw new ArgumentException("النموذج غير موجود أو غير نشط");
        }

        private void ValidateRequiredFields(Form form, Dictionary<string, string> values)
        {
            var missingFields = form.FormFields
                .Where(f => f.IsRequired && f.IsActive)
                .Where(f => !values.ContainsKey(f.FieldName) || string.IsNullOrWhiteSpace(values[f.FieldName]))
                .Select(f => f.Label)
                .ToList();

            if (missingFields.Any())
            {
                throw new ArgumentException($"الحقول التالية مطلوبة: {string.Join(", ", missingFields)}");
            }
        }

        private async Task<FormSubmission> CreateSubmissionAsync(int formId, SubmitFormDto submitFormDto)
        {
            var submission = new FormSubmission
            {
                FormId = formId,
                SubmittedBy = submitFormDto.SubmittedBy,
                SubmittedDate = DateTime.UtcNow,
                Status = FormConstants.SubmissionStatus.UnderReview
            };

            _context.FormSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            return submission;
        }

        private async Task<List<FormSubmissionValue>> CreateSubmissionValuesAsync(
            int submissionId, Form form, Dictionary<string, string> values)
        {
            var submissionValues = values
                .Select(kvp => form.FormFields.FirstOrDefault(f => f.FieldName == kvp.Key))
                .Where(field => field != null)
                .Select(field => new FormSubmissionValue
                {
                    SubmissionId = submissionId,
                    FieldId = field!.FieldId,
                    FieldValue = values[field.FieldName],
                    FieldNameAtSubmission = field.FieldName,
                    FieldTypeAtSubmission = field.FieldType,
                    LabelAtSubmission = field.Label,
                    OptionsAtSubmission = field.Options,
                    CreatedDate = DateTime.UtcNow
                })
                .ToList();

            _context.FormSubmissionValues.AddRange(submissionValues);
            await _context.SaveChangesAsync();

            return submissionValues;
        }

        private async Task ProcessSubmissionApprovalAsync(FormSubmission submission,
            List<FormSubmissionValue> submissionValues, ValidationResult validationResult)
        {
            var allErrors = new List<string>(validationResult.AllErrors);
            var allErrorsEn = new List<string>(validationResult.AllErrorsEn);

            try
            {
                var approvalResult = await _approvalService.ProcessApprovalAsync(submissionValues);

                if (!approvalResult.IsApproved && !string.IsNullOrEmpty(approvalResult.RejectionReason))
                {
                    allErrors.AddRange(
                        approvalResult.RejectionReason.Split(", ", StringSplitOptions.RemoveEmptyEntries));

                    allErrorsEn.AddRange(
                        approvalResult.RejectionReasonEn?.Split(", ", StringSplitOptions.RemoveEmptyEntries) ??
                        Array.Empty<string>());
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Approval service failed for submission {SubmissionId}", submission.SubmissionId);
                allErrors.Add("خطأ في نظام المراجعة");
                allErrorsEn.Add("System review error");
            }

            if (allErrors.Any())
            {
                submission.Status = FormConstants.SubmissionStatus.Rejected;
                submission.RejectionReason = string.Join(", ", allErrors);
                submission.RejectionReasonEn = string.Join(", ", allErrorsEn);

                _logger.LogWarning("Submission {SubmissionId} rejected: {Errors}", submission.SubmissionId,
                    string.Join(", ", allErrors));
            }
            else
            {
                submission.Status = FormConstants.SubmissionStatus.Approved;
                _logger.LogInformation("Submission {SubmissionId} approved", submission.SubmissionId);
            }
        }

        private async Task<ValidationResult> ValidateSubmissionDataAsync(Dictionary<string, string> values)
        {
            var result = new ValidationResult();

            var form = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .FirstOrDefaultAsync(f => f.IsActive);

            if (form != null)
            {
                // use the validation service for dynamic field validations
                var dynamicValidationResult =
                    await _fieldValidationService.ValidateDynamicRulesAsync(values, form.FormFields.ToList());

                result.AllErrors.AddRange(dynamicValidationResult.AllErrors);
                result.AllErrorsEn.AddRange(dynamicValidationResult.AllErrorsEn);
            }

            // Validate phone number format (now non-critical - saved to database)
            if (values.TryGetValue("phoneNumber", out var phoneNumber))
            {
                try
                {
                    _whatsAppService.ValidateAndFormatPhoneNumber(phoneNumber);
                } catch (ArgumentException ex)
                {
                    result.AllErrors.Add($"رقم الجوال غير صحيح: برجاء ادخال الرقم الصحيح مثال (966-5xxxxxxxx)");
                    result.AllErrorsEn.Add($"Invalid phone number: {ex.Message}");
                }
            }

            // MODIFIED: Age validation using dropdown selection instead of birth date calculation
            if (values.TryGetValue("birthDate", out var ageSelection)) // Still using "birthDate" key for compatibility
            {
                if (ageSelection == "أصغر من 20 سنة")
                {
                    result.AllErrors.Add("العمر أقل من الحد الأدنى المطلوب (20 سنة)");
                    result.AllErrorsEn.Add("Age is below the minimum required (20 years)");
                }
            }

            // Validate numeric fields (now saved to database instead of throwing)
            if (values.TryGetValue("monthlySalary", out var salaryStr))
            {
                if (!decimal.TryParse(salaryStr, out var salary) || salary <= 0)
                {
                    result.AllErrors.Add("الراتب الشهري يجب أن يكون رقم صحيح أكبر من صفر");
                    result.AllErrorsEn.Add("Monthly salary must be a valid number greater than zero");
                }
                // NEW: Add minimum salary validation
                else if (salary < 3000)
                {
                    result.AllErrors.Add(
                        $"عذراً، الراتب الشهري يجب أن يكون 3000 ريال أو أكثر (الراتب المدخل: {salary:N0} ريال)");

                    result.AllErrorsEn.Add(
                        $"Sorry, monthly salary must be 3,000 SAR or more (provided salary: {salary:N0} SAR)");
                }
            }

            if (values.TryGetValue("monthlyCommitments", out var commitmentsStr))
            {
                if (!decimal.TryParse(commitmentsStr, out var commitments) || commitments < 0)
                {
                    result.AllErrors.Add("الالتزامات الشهرية يجب أن تكون رقم صحيح لا يقل عن صفر");
                    result.AllErrorsEn.Add("Monthly commitments must be a valid number not less than zero");
                }
            }

            // NEW: Service duration validation
            if (values.TryGetValue("ServiceDuration", out var serviceDuration))
            {
                if (serviceDuration == "اقل من ٣ شهور")
                {
                    result.AllErrors.Add("عذراً، مدة الخدمة يجب أن تكون أكثر من 3 شهور");
                    result.AllErrorsEn.Add("Sorry, service duration must be more than 3 months");
                }
            }

            // NEW: Job sector validation
            if (values.TryGetValue("jobSector", out var jobSector))
            {
                if (jobSector == "متقاعد")
                {
                    result.AllErrors.Add("عذراً، هذا النموذج غير متاح للمتقاعدين");
                    result.AllErrorsEn.Add("Sorry, this form is not available for retirees");
                }
            }

            return await Task.FromResult(result);
        }

        private async Task SendApprovalWhatsAppMessageAsync(FormSubmission submission,
            List<FormSubmissionValue> submissionValues)
        {
            try
            {
                _logger.LogInformation("Attempting to send WhatsApp message for approved submission {SubmissionId}",
                    submission.SubmissionId);

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
                    _logger.LogWarning("Failed to create subscriber for {Phone}, but continuing with template send",
                        phoneNumber);
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

                var serviceDurationValue = submissionValues.FirstOrDefault(v =>
                    v.FieldNameAtSubmission.ToLower().Contains("serviceduration") ||
                    v.LabelAtSubmission.Contains("مدة خدمة"));

                var templateParams = new Dictionary<string, string>
                {
                    ["BODY_1"] = submission.SubmissionId.ToString(), // رقم الطلب: {{1}}
                    ["BODY_2"] = fullNameValue?.FieldValue ?? "غير محدد", // الاسم الثلاثي: {{2}}
                    ["BODY_3"] = phoneValue.FieldValue, // رقم الجوال: {{3}}
                    ["BODY_4"] = nationalIdValue?.FieldValue ?? "غير محدد", // رقم الهوية: {{4}}
                    ["BODY_5"] = birthDateValue?.FieldValue ?? "غير محدد", // تاريخ الميلاد: {{5}}
                    ["BODY_6"] = salaryValue?.FieldValue ?? "غير محدد", // الراتب: {{6}}
                    ["BODY_7"] = commitmentsValue?.FieldValue ?? "غير محدد", // الالتزامات: {{7}}
                    ["BODY_8"] =
                        serviceDurationValue?.FieldValue ?? "جديد" // مدة الخدمة: {{8}} - default for new customers
                };

                // Send WhatsApp template message
                var messageSent = await _whatsAppService.SendApprovalMessageAsync(phoneNumber, templateParams);

                if (messageSent)
                {
                    _logger.LogInformation(
                        "WhatsApp approval message sent successfully for submission {SubmissionId} to {Phone}",
                        submission.SubmissionId, phoneNumber);
                }
                else
                {
                    _logger.LogError(
                        "Failed to send WhatsApp approval message for submission {SubmissionId} to {Phone}",
                        submission.SubmissionId, phoneNumber);
                }
            } catch (OperationCanceledException)
            {
                _logger.LogError("WhatsApp message sending timed out for submission {SubmissionId}",
                    submission.SubmissionId);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp approval message for submission {SubmissionId}",
                    submission.SubmissionId);
            }
        }

        private async Task<List<FormField>> CreateMandatoryFieldsAsync(int formId)
        {
            var citizenshipOptions = new List<string> { "مواطن", "مقيم" };
            var mortgageOptions = new List<string> { "نعم", "لا" };
            var ageOptions = new List<string> { "أكبر من 20 سنة", "أصغر من 20 سنة" }; // NEW: Age options

            var mandatoryFields = new List<FormField>
            {
                new FormField
                {
                    FormId = formId,
                    FieldName = "fullName",
                    FieldType = "text",
                    Label = "الاسم الثلاثي",
                    IsRequired = true,
                    DisplayOrder = 1,
                    IsActive = true
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "phoneNumber",
                    FieldType = "number",
                    Label = "رقم الجوال",
                    IsRequired = true,
                    DisplayOrder = 2,
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
                    FieldType = "dropdown", // changed from date to dropdown
                    Label = "العمر",
                    IsRequired = true,
                    DisplayOrder = 3,
                    IsActive = true,
                    Options = JsonSerializer.Serialize(ageOptions) // NEW: Age options
                },
                new FormField
                {
                    FormId = formId,
                    FieldName = "citizenshipStatus",
                    FieldType = "dropdown",
                    Label = "مواطن أو مقيم",
                    IsRequired = true,
                    DisplayOrder = 4,
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
                    DisplayOrder = 5,
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
                    DisplayOrder = 6,
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
                    DisplayOrder = 7,
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

        private FormDto MapToFormDto(Form form)
        {
            return new FormDto
            {
                FormId = form.FormId,
                Name = form.Name,
                Description = form.Description,
                IsActive = form.IsActive,
                CreatedDate = form.CreatedDate,
                Fields = form.FormFields
                    .OrderBy(f => f.DisplayOrder)
                    .Select(MapToFormFieldDto)
                    .ToList()
            };
        }

        private FormFieldDto MapToFormFieldDto(FormField field)
        {
            return new FormFieldDto
            {
                FieldId = field.FieldId,
                FieldName = field.FieldName,
                FieldType = field.FieldType,
                Label = field.Label,
                IsRequired = field.IsRequired,
                DisplayOrder = field.DisplayOrder,
                Options = !string.IsNullOrEmpty(field.Options)
                    ? JsonSerializer.Deserialize<List<string>>(field.Options)
                    : null,
                ValidationRules = !string.IsNullOrEmpty(field.ValidationRules)
                    ? JsonSerializer.Deserialize<ValidationRuleDto>(field.ValidationRules)
                    : null
            };
        }
    }
}