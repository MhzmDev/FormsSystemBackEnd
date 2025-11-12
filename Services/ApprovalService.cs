using DynamicForm.Models;
using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DynamicForm.Services
{
    public class ApprovalService : IApprovalService
    {
        private readonly ILogger<ApprovalService> _logger;
        private readonly IWhatsAppService _whatsAppService;

        public ApprovalService(ILogger<ApprovalService> logger, IWhatsAppService whatsAppService)
        {
            _logger = logger;
            _whatsAppService = whatsAppService;
        }

        public async Task<ApprovalResult> ProcessApprovalAsync(ICollection<FormSubmissionValue> submissionValues)
        {
            var result = new ApprovalResult { IsApproved = true };
            var rejectionReasons = new List<string>();
            var rejectionReasonsEn = new List<string>();

            try
            {
                // Get field values
                var citizenshipStatus = GetFieldValue(submissionValues, "citizenshipStatus");
                var hasMortgage = GetFieldValue(submissionValues, "hasMortgage");
                var salaryStr = GetFieldValue(submissionValues, "monthlySalary");
                var commitmentsStr = GetFieldValue(submissionValues, "monthlyCommitments");
                var phoneNumber = GetFieldValue(submissionValues, "phoneNumber");
                var birthDateStr = GetFieldValue(submissionValues, "birthDate");
                var ageStr = GetFieldValue(submissionValues, "age");

                // Rule 1: Phone number validation
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    try
                    {
                        var validatedPhone = _whatsAppService.ValidateAndFormatPhoneNumber(phoneNumber);
                        _logger.LogInformation("Phone number validated successfully: {Phone}", validatedPhone);
                    }
                    catch (ArgumentException ex)
                    {
                        rejectionReasons.Add($"رقم الجوال غير صحيح: {ex.Message}");
                        rejectionReasonsEn.Add($"Invalid phone number: {ex.Message}");
                        result.IsApproved = false;
                        _logger.LogWarning("Phone validation failed: {Phone} - {Error}", phoneNumber, ex.Message);
                    }
                }
                else
                {
                    rejectionReasons.Add("رقم الجوال مطلوب");
                    rejectionReasonsEn.Add("Phone number is required");
                    result.IsApproved = false;
                }

                // Rule 2: Birth date validation (must be 20 years or older)
                if (!string.IsNullOrWhiteSpace(birthDateStr))
                {
                    if (DateTime.TryParseExact(birthDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthDate))
                    {
                        var age = CalculateAge(birthDate);

                        if (age < 20)
                        {
                            rejectionReasons.Add($"العمر ({age} سنة) أقل من الحد الأدنى المطلوب (20 سنة)");
                            rejectionReasonsEn.Add($"Age ({age} years) is below the minimum required (20 years)");
                            result.IsApproved = false;
                        }

                        // Rule 3: Age consistency validation (if age field exists)
                        if (!string.IsNullOrWhiteSpace(ageStr) && int.TryParse(ageStr, out var providedAge))
                        {
                            // Allow 1 year tolerance for age calculation differences
                            if (Math.Abs(age - providedAge) > 1)
                            {
                                rejectionReasons.Add($"العمر المدخل ({providedAge} سنة) لا يتطابق مع تاريخ الميلاد (العمر الفعلي: {age} سنة)");
                                rejectionReasonsEn.Add($"Provided age ({providedAge} years) does not match birth date (actual age: {age} years)");
                                result.IsApproved = false;
                            }
                        }
                    }
                    else
                    {
                        rejectionReasons.Add("تاريخ الميلاد غير صحيح. يجب أن يكون بالصيغة: YYYY-MM-DD");
                        rejectionReasonsEn.Add("Invalid birth date format. Must be in format: YYYY-MM-DD");
                        result.IsApproved = false;
                    }
                }
                else
                {
                    rejectionReasons.Add("تاريخ الميلاد مطلوب");
                    rejectionReasonsEn.Add("Birth date is required");
                    result.IsApproved = false;
                }

                // Rule 4: Citizenship validation
                if (citizenshipStatus?.Equals("مقيم", StringComparison.OrdinalIgnoreCase) == true)
                {
                    rejectionReasons.Add("مقدم الطلب مقيم وليس مواطن");
                    rejectionReasonsEn.Add("Applicant is a resident, not a citizen");
                    result.IsApproved = false;
                }

                // Rule 5: Salary and commitments validation
                if (decimal.TryParse(salaryStr, out var salary) && decimal.TryParse(commitmentsStr, out var commitments))
                {
                    if (salary <= 0)
                    {
                        rejectionReasons.Add("الراتب الشهري يجب أن يكون أكبر من صفر");
                        rejectionReasonsEn.Add("Monthly salary must be greater than zero");
                        result.IsApproved = false;
                    }
                    else
                    {
                        // Rule 6: Commitment percentage rules
                        decimal commitmentPercentage = (commitments / salary) * 100;
                        bool hasMortgageLoan = hasMortgage?.Equals("نعم", StringComparison.OrdinalIgnoreCase) == true;

                        if (hasMortgageLoan)
                        {
                            // Allow up to 55% for those with mortgage
                            if (commitmentPercentage > 55)
                            {
                                rejectionReasons.Add($"نسبة الالتزامات ({commitmentPercentage:F1}%) تتجاوز الحد المسموح (55%) للأشخاص الذين لديهم قرض عقاري");
                                rejectionReasonsEn.Add($"Commitment ratio ({commitmentPercentage:F1}%) exceeds the allowed limit (55%) for those with mortgage loans");
                                result.IsApproved = false;
                            }
                        }
                        else
                        {
                            // Allow up to 43% for those without mortgage
                            if (commitmentPercentage > 43)
                            {
                                rejectionReasons.Add($"نسبة الالتزامات ({commitmentPercentage:F1}%) تتجاوز الحد المسموح (43%)");
                                rejectionReasonsEn.Add($"Commitment ratio ({commitmentPercentage:F1}%) exceeds the allowed limit (43%)");
                                result.IsApproved = false;
                            }
                        }
                    }
                }
                else
                {
                    rejectionReasons.Add("الراتب الشهري والالتزامات الشهرية يجب أن تكون أرقام صحيحة");
                    rejectionReasonsEn.Add("Monthly salary and commitments must be valid numbers");
                    result.IsApproved = false;
                }

                // Set final status and reasons
                if (result.IsApproved)
                {
                    result.Status = "مقبول";
                    _logger.LogInformation("Application approved successfully");
                }
                else
                {
                    result.Status = "مرفوض";
                    result.RejectionReason = string.Join(", ", rejectionReasons);
                    result.RejectionReasonEn = string.Join(", ", rejectionReasonsEn);
                    _logger.LogWarning("Application rejected: {Reasons}", result.RejectionReason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval");
                result.IsApproved = false;
                result.Status = "خطأ في المعالجة";
                result.RejectionReason = "حدث خطأ أثناء معالجة الطلب";
                result.RejectionReasonEn = "An error occurred while processing the application";
            }

            return await Task.FromResult(result);
        }

        private string? GetFieldValue(ICollection<FormSubmissionValue> values, string fieldName)
        {
            return values.FirstOrDefault(v =>
                v.FieldNameAtSubmission.Equals(fieldName, StringComparison.OrdinalIgnoreCase))?.FieldValue;
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;

            // Adjust age if birthday hasn't occurred this year
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }
}