using DynamicForm.Models;
using DynamicForm.Models.DTOs;

namespace DynamicForm.Services
{
    public class ApprovalService : IApprovalService
    {
        public async Task<ApprovalResult> ProcessApprovalAsync(ICollection<FormSubmissionValue> submissionValues)
        {
            var result = new ApprovalResult { IsApproved = true };
            var rejectionReasons = new List<string>();
            var rejectionReasonsEn = new List<string>();

            // Extract mandatory field values
            var citizenshipStatus = GetFieldValue(submissionValues, "citizenshipStatus");
            var hasMortgage = GetFieldValue(submissionValues, "hasMortgage");
            var salaryStr = GetFieldValue(submissionValues, "monthlySalary");
            var commitmentsStr = GetFieldValue(submissionValues, "monthlyCommitments");

            // Parse numeric values
            if (!decimal.TryParse(salaryStr, out decimal salary))
            {
                rejectionReasons.Add("الراتب الشهري غير صحيح");
                rejectionReasonsEn.Add("Invalid monthly salary");
                result.IsApproved = false;
            }

            if (!decimal.TryParse(commitmentsStr, out decimal commitments))
            {
                rejectionReasons.Add("الالتزامات الشهرية غير صحيحة");
                rejectionReasonsEn.Add("Invalid monthly commitments");
                result.IsApproved = false;
            }

            if (!result.IsApproved)
            {
                result.RejectionReason = string.Join(", ", rejectionReasons);
                result.RejectionReasonEn = string.Join(", ", rejectionReasonsEn);

                return result;
            }

            // Apply business rules

            // Rule 1: Salary less than 3000
            if (salary < 3000)
            {
                rejectionReasons.Add("الراتب أقل من 3000 ريال");
                rejectionReasonsEn.Add("Salary is less than 3000 SAR");
                result.IsApproved = false;
            }

            // Rule 2: Resident (مقيم)
            if (citizenshipStatus?.Equals("مقيم", StringComparison.OrdinalIgnoreCase) == true)
            {
                rejectionReasons.Add("مقدم الطلب مقيم وليس مواطن");
                rejectionReasonsEn.Add("Applicant is a resident, not a citizen");
                result.IsApproved = false;
            }

            // Rule 3: Commitment percentage rules
            if (salary > 0)
            {
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

            // Set final status and reasons
            if (result.IsApproved)
            {
                result.Status = "مقبول";
            }
            else
            {
                result.Status = "مرفوض";
                result.RejectionReason = string.Join(", ", rejectionReasons);
                result.RejectionReasonEn = string.Join(", ", rejectionReasonsEn);
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