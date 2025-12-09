using DynamicForm.Data;
using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DynamicForm.Services
{
    public class RejectionReasonCatalogService : IRejectionReasonCatalogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RejectionReasonCatalogService> _logger;

        public RejectionReasonCatalogService(
            ApplicationDbContext context,
            ILogger<RejectionReasonCatalogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FormRejectionReasonsDto> GetPossibleRejectionReasonsForActiveFormAsync()
        {
            var activeForm = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .FirstOrDefaultAsync(f => f.IsActive);

            if (activeForm == null)
            {
                return new FormRejectionReasonsDto
                {
                    FormId = 0,
                    FormName = "لا يوجد نموذج نشط",
                    PossibleReasons = new List<PossibleRejectionReasonDto>()
                };
            }

            return await BuildRejectionReasonsCatalogAsync(activeForm);
        }

        public async Task<FormRejectionReasonsDto> GetPossibleRejectionReasonsForFormAsync(int formId)
        {
            var form = await _context.Forms
                .Include(f => f.FormFields.Where(ff => ff.IsActive))
                .FirstOrDefaultAsync(f => f.FormId == formId);

            if (form == null)
            {
                throw new ArgumentException($"النموذج رقم {formId} غير موجود");
            }

            return await BuildRejectionReasonsCatalogAsync(form);
        }

        private async Task<FormRejectionReasonsDto> BuildRejectionReasonsCatalogAsync(Form form)
        {
            var reasons = new List<PossibleRejectionReasonDto>();

            // 1. Hard-coded validation reasons (from FormService.ValidateSubmissionDataAsync)
            reasons.AddRange(GetHardCodedValidationReasons(form));

            // 2. Dynamic field validation rules
            reasons.AddRange(GetDynamicFieldValidationReasons(form.FormFields.ToList()));

            // 3. Business rule reasons (from ApprovalService)
            reasons.AddRange(GetBusinessRuleReasons(form));

            return new FormRejectionReasonsDto
            {
                FormId = form.FormId,
                FormName = form.Name,
                PossibleReasons = reasons
                    .GroupBy(r => new { r.ReasonTextAr, r.ReasonTextEn }) // Remove duplicates
                    .Select(g => g.First())
                    .OrderBy(r => r.Category)
                    .ThenBy(r => r.ReasonTextAr)
                    .ToList()
            };
        }

        private List<PossibleRejectionReasonDto> GetHardCodedValidationReasons(Form form)
        {
            var reasons = new List<PossibleRejectionReasonDto>();

            var hasPhoneNumber = form.FormFields.Any(f => f.FieldName == "phoneNumber");
            var hasBirthDate = form.FormFields.Any(f => f.FieldName == "birthDate");
            var hasMonthlySalary = form.FormFields.Any(f => f.FieldName == "monthlySalary");
            var hasMonthlyCommitments = form.FormFields.Any(f => f.FieldName == "monthlyCommitments");
            var hasServiceDuration = form.FormFields.Any(f => f.FieldName == "ServiceDuration");
            var hasJobSector = form.FormFields.Any(f => f.FieldName == "jobSector");

            if (hasPhoneNumber)
            {
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "رقم الجوال غير صحيح: برجاء ادخال الرقم الصحيح مثال (966-5xxxxxxxx)",
                    ReasonTextEn = "Invalid phone number format",
                    SearchPatternAr = "رقم الجوال غير صحيح", // ✅ Template for searching
                    SearchPatternEn = "Invalid phone number",
                    Category = "Validation",
                    FieldName = "phoneNumber"
                });
            }

            if (hasBirthDate)
            {
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "العمر أقل من الحد الأدنى المطلوب (20 سنة)",
                    ReasonTextEn = "Age is below the minimum required (20 years)",
                    SearchPatternAr = "العمر أقل من الحد الأدنى", // ✅ Template
                    SearchPatternEn = "Age is below the minimum",
                    Category = "Validation",
                    FieldName = "birthDate"
                });
            }

            if (hasMonthlySalary)
            {
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "الراتب الشهري يجب أن يكون رقم صحيح أكبر من صفر",
                    ReasonTextEn = "Monthly salary must be a valid number greater than zero",
                    SearchPatternAr = "الراتب الشهري يجب أن يكون رقم صحيح", // ✅ Template
                    SearchPatternEn = "Monthly salary must be a valid number",
                    Category = "Validation",
                    FieldName = "monthlySalary"
                });

                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "عذراً، الراتب الشهري يجب أن يكون 3000 ريال أو أكثر",
                    ReasonTextEn = "Sorry, monthly salary must be 3,000 SAR or more",
                    SearchPatternAr = "الراتب الشهري يجب أن يكون 3000 ريال", // ✅ Template (ignores dynamic part)
                    SearchPatternEn = "monthly salary must be 3,000 SAR",
                    Category = "Validation",
                    FieldName = "monthlySalary"
                });
            }

            if (hasMonthlyCommitments)
            {
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "الالتزامات الشهرية يجب أن تكون رقم صحيح لا يقل عن صفر",
                    ReasonTextEn = "Monthly commitments must be a valid number not less than zero",
                    SearchPatternAr = "الالتزامات الشهرية يجب أن تكون رقم صحيح", // ✅ Template
                    SearchPatternEn = "Monthly commitments must be a valid number",
                    Category = "Validation",
                    FieldName = "monthlyCommitments"
                });
            }

            if (hasServiceDuration)
            {
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "عذراً، مدة الخدمة يجب أن تكون أكثر من 3 شهور",
                    ReasonTextEn = "Sorry, service duration must be more than 3 months",
                    SearchPatternAr = "مدة الخدمة يجب أن تكون أكثر من 3", // ✅ Template
                    SearchPatternEn = "service duration must be more than 3",
                    Category = "Validation",
                    FieldName = "ServiceDuration"
                });
            }

            if (hasJobSector)
            {
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "عذراً، هذا النموذج غير متاح للمتقاعدين",
                    ReasonTextEn = "Sorry, this form is not available for retirees",
                    SearchPatternAr = "غير متاح للمتقاعدين", // ✅ Template
                    SearchPatternEn = "not available for retirees",
                    Category = "Validation",
                    FieldName = "jobSector"
                });
            }

            return reasons;
        }

        private List<PossibleRejectionReasonDto> GetDynamicFieldValidationReasons(List<FormField> fields)
        {
            var reasons = new List<PossibleRejectionReasonDto>();

            foreach (var field in fields.Where(f => !string.IsNullOrEmpty(f.ValidationRules)))
            {
                try
                {
                    var validationRule = JsonSerializer.Deserialize<ValidationRuleDto>(field.ValidationRules!);

                    if (validationRule != null &&
                        !string.IsNullOrEmpty(validationRule.ErrorMessageAr))
                    {
                        reasons.Add(new PossibleRejectionReasonDto
                        {
                            ReasonTextAr = validationRule.ErrorMessageAr,
                            ReasonTextEn = validationRule.ErrorMessageEn ?? validationRule.ErrorMessageAr,
                            Category = "DynamicField",
                            FieldName = field.FieldName,
                            SearchPatternAr = validationRule.ErrorMessageAr,
                            SearchPatternEn = validationRule.ErrorMessageEn ?? validationRule.ErrorMessageAr
                        });
                    }
                } catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to parse validation rules for field {FieldName}", field.FieldName);
                }
            }

            return reasons;
        }

        private List<PossibleRejectionReasonDto> GetBusinessRuleReasons(Form form)
        {
            var reasons = new List<PossibleRejectionReasonDto>();

            var hasCitizenshipStatus = form.FormFields.Any(f => f.FieldName == "citizenshipStatus");
            var hasMortgage = form.FormFields.Any(f => f.FieldName == "hasMortgage");

            var hasSalaryAndCommitments =
                form.FormFields.Any(f => f.FieldName == "monthlySalary") &&
                form.FormFields.Any(f => f.FieldName == "monthlyCommitments");

            if (hasCitizenshipStatus)
            {
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "مقدم الطلب مقيم وليس مواطن",
                    ReasonTextEn = "Applicant is a resident, not a citizen",
                    SearchPatternAr = "مقدم الطلب مقيم", // ✅ Template
                    SearchPatternEn = "Applicant is a resident",
                    Category = "BusinessRule",
                    FieldName = "citizenshipStatus"
                });
            }

            if (hasMortgage)
            {
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "لديك قرض عقاري حالي",
                    ReasonTextEn = "You have an existing mortgage",
                    SearchPatternAr = "لديك قرض عقاري", // ✅ Template
                    SearchPatternEn = "existing mortgage",
                    Category = "BusinessRule",
                    FieldName = "hasMortgage"
                });
            }

            if (hasSalaryAndCommitments)
            {
                // ✅ This one has TWO dynamic patterns!
                reasons.Add(new PossibleRejectionReasonDto
                {
                    ReasonTextAr = "نسبة الالتزامات تتجاوز الحد المسموح (43% أو 55%)",
                    ReasonTextEn = "Commitment ratio exceeds the allowed limit",
                    SearchPatternAr = "نسبة الالتزامات", // ✅ Template (matches both 43% and 55% cases)
                    SearchPatternEn = "Commitment ratio",
                    Category = "BusinessRule",
                    FieldName = "monthlyCommitments"
                });
            }

            return reasons;
        }
    }
}