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

                // Note: Format validations (phone, birthdate, age, numeric formats) are now handled in FormService
                // This service focuses only on business logic validations

                // Rule 1: Citizenship validation
                if (citizenshipStatus?.Equals("مقيم", StringComparison.OrdinalIgnoreCase) == true)
                {
                    rejectionReasons.Add("مقدم الطلب مقيم وليس مواطن");
                    rejectionReasonsEn.Add("Applicant is a resident, not a citizen");
                    result.IsApproved = false;
                }

                // Rule 2: Salary and commitments business logic validation
                if (decimal.TryParse(salaryStr, out var salary) && decimal.TryParse(commitmentsStr, out var commitments))
                {
                    // Rule 3: Commitment percentage rules
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
                else
                {
                    // This should not happen as format validation is done in FormService, but keeping as fallback
                    rejectionReasons.Add("خطأ في قراءة بيانات الراتب والالتزامات");
                    rejectionReasonsEn.Add("Error reading salary and commitments data");
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
    }
}