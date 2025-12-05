using Microsoft.AspNetCore.Mvc;
using DynamicForm.Services;
using DynamicForm.Models.DTOs;

namespace DynamicForm.Controllers;

//#if DEBUG
//[ApiExplorerSettings(IgnoreApi = true)]
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly IFormService _formService;
    private readonly ILogger<TestController> _logger;

    public TestController(IFormService formService, ILogger<TestController> logger, IEmailService emailService)
    {
        _formService = formService;
        _logger = logger;
        _emailService = emailService;
    }

    [HttpPost("send-test-email")]
    public async Task<IActionResult> SendTestEmail([FromQuery] string recipientEmail)
    {
        try
        {
            var testContent = System.Text.Encoding.UTF8.GetBytes("This is a test attachment");

            var result = await _emailService.SendEmailWithAttachmentAsync(
                recipientEmail,
                "اختبار النظام - Test Email",
                "<h2>مرحباً!</h2><p>هذا بريد اختباري من نظام النماذج الديناميكية</p>",
                testContent,
                "test.txt",
                "text/plain"
            );

            if (result)
            {
                return Ok(new { success = true, message = "Email sent successfully!" });
            }

            return BadRequest(new { success = false, message = "Failed to send email" });
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Test email failed");

            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("test-validation-errors/{formId}")]
    public async Task<IActionResult> TestValidationErrors(int formId)
    {
        var results = new List<object>();

        // Test 1: Valid submission (should be approved or go through normal process)
        try
        {
            var validSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Valid User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار صحيح",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234567",
                    ["birthDate"] = DateTime.Now.AddYears(-25).ToString("yyyy-MM-dd"),
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "8000",
                    ["monthlyCommitments"] = "2000",
                    ["age"] = "25",
                    ["nationalId"] = "1234567890",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var validResult = await _formService.SubmitFormAsync(formId, validSubmission);

            results.Add(new
            {
                Test = "Valid Submission",
                Status = "Success",
                SubmissionId = validResult.SubmissionId,
                ApprovalStatus = validResult.Status,
                RejectionReason = validResult.RejectionReason,
                RejectionReasonEn = validResult.RejectionReasonEn
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Valid Submission",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 2: Invalid age (under 20 - should be saved with rejection)
        try
        {
            var invalidAgeSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Invalid Age User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار عمر غير صحيح",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234568",
                    ["birthDate"] = DateTime.Now.AddYears(-18).ToString("yyyy-MM-dd"), // 18 years old
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "6000",
                    ["monthlyCommitments"] = "1500",
                    ["age"] = "18",
                    ["nationalId"] = "1234567891",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var invalidResult = await _formService.SubmitFormAsync(formId, invalidAgeSubmission);

            results.Add(new
            {
                Test = "Invalid Age (18 years)",
                Status = "SavedWithRejection",
                SubmissionId = invalidResult.SubmissionId,
                ApprovalStatus = invalidResult.Status,
                RejectionReason = invalidResult.RejectionReason,
                RejectionReasonEn = invalidResult.RejectionReasonEn
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Invalid Age (18 years)",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 3: Invalid phone number (should be saved with rejection)
        try
        {
            var invalidPhoneSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Invalid Phone User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار رقم جوال خطأ",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "123456789", // Invalid phone format
                    ["birthDate"] = DateTime.Now.AddYears(-25).ToString("yyyy-MM-dd"),
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "7000",
                    ["monthlyCommitments"] = "1800",
                    ["age"] = "25",
                    ["nationalId"] = "1234567892",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var invalidPhoneResult = await _formService.SubmitFormAsync(formId, invalidPhoneSubmission);

            results.Add(new
            {
                Test = "Invalid Phone Number",
                Status = "SavedWithRejection",
                SubmissionId = invalidPhoneResult.SubmissionId,
                ApprovalStatus = invalidPhoneResult.Status,
                RejectionReason = invalidPhoneResult.RejectionReason,
                RejectionReasonEn = invalidPhoneResult.RejectionReasonEn
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Invalid Phone Number",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 4: Invalid birth date format (should be saved with rejection)
        try
        {
            var invalidBirthDateSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Invalid Birth Date User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار تاريخ ميلاد خطأ",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234570",
                    ["birthDate"] = "1990/05/15", // Invalid date format
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "7500",
                    ["monthlyCommitments"] = "2000",
                    ["age"] = "30",
                    ["nationalId"] = "1234567893",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var invalidBirthDateResult = await _formService.SubmitFormAsync(formId, invalidBirthDateSubmission);

            results.Add(new
            {
                Test = "Invalid Birth Date Format",
                Status = "SavedWithRejection",
                SubmissionId = invalidBirthDateResult.SubmissionId,
                ApprovalStatus = invalidBirthDateResult.Status,
                RejectionReason = invalidBirthDateResult.RejectionReason,
                RejectionReasonEn = invalidBirthDateResult.RejectionReasonEn
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Invalid Birth Date Format",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 5: Invalid salary (should be saved with rejection)
        try
        {
            var invalidSalarySubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Invalid Salary User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار راتب خطأ",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234571",
                    ["birthDate"] = DateTime.Now.AddYears(-30).ToString("yyyy-MM-dd"),
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "-1000", // Invalid negative salary
                    ["monthlyCommitments"] = "1500",
                    ["age"] = "30",
                    ["nationalId"] = "1234567894",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var invalidSalaryResult = await _formService.SubmitFormAsync(formId, invalidSalarySubmission);

            results.Add(new
            {
                Test = "Invalid Salary (Negative)",
                Status = "SavedWithRejection",
                SubmissionId = invalidSalaryResult.SubmissionId,
                ApprovalStatus = invalidSalaryResult.Status,
                RejectionReason = invalidSalaryResult.RejectionReason,
                RejectionReasonEn = invalidSalaryResult.RejectionReasonEn
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Invalid Salary (Negative)",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 6: Multiple validation errors (should be saved with all errors)
        try
        {
            var multipleErrorsSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Multiple Errors User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار أخطاء متعددة",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "invalid-phone", // Invalid phone
                    ["birthDate"] = DateTime.Now.AddYears(-15).ToString("yyyy-MM-dd"), // Invalid age (15 years)
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "abc", // Invalid salary format
                    ["monthlyCommitments"] = "-500", // Invalid negative commitments
                    ["age"] = "25", // Inconsistent with birth date
                    ["nationalId"] = "1234567895",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var multipleErrorsResult = await _formService.SubmitFormAsync(formId, multipleErrorsSubmission);

            results.Add(new
            {
                Test = "Multiple Validation Errors",
                Status = "SavedWithRejection",
                SubmissionId = multipleErrorsResult.SubmissionId,
                ApprovalStatus = multipleErrorsResult.Status,
                RejectionReason = multipleErrorsResult.RejectionReason,
                RejectionReasonEn = multipleErrorsResult.RejectionReasonEn
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Multiple Validation Errors",
                Status = "Error",
                Error = ex.Message
            });
        }

        return Ok(new
        {
            Message = "All validation error tests completed",
            Note = "All validation errors (age, phone, format, etc.) are now saved to database instead of throwing exceptions",
            Results = results
        });
    }

    [HttpPost("test-age-validation/{formId}")]
    public async Task<IActionResult> TestAgeValidation(int formId)
    {
        var results = new List<object>();

        // Test 1: Valid age (should be approved or go through normal process)
        try
        {
            var validSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Valid Age User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار عمر صحيح",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234567",
                    ["birthDate"] = DateTime.Now.AddYears(-25).ToString("yyyy-MM-dd"),
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "8000",
                    ["monthlyCommitments"] = "2000",
                    ["age"] = "25",
                    ["nationalId"] = "1234567890",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var validResult = await _formService.SubmitFormAsync(formId, validSubmission);

            results.Add(new
            {
                Test = "Valid Age (25 years)",
                Status = "Success",
                SubmissionId = validResult.SubmissionId,
                ApprovalStatus = validResult.Status,
                RejectionReason = validResult.RejectionReason
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Valid Age (25 years)",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 2: Invalid age (under 20 - should be saved with rejection)
        try
        {
            var invalidAgeSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Invalid Age User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار عمر غير صحيح",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234568",
                    ["birthDate"] = DateTime.Now.AddYears(-18).ToString("yyyy-MM-dd"), // 18 years old
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "6000",
                    ["monthlyCommitments"] = "1500",
                    ["age"] = "18",
                    ["nationalId"] = "1234567890",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var invalidResult = await _formService.SubmitFormAsync(formId, invalidAgeSubmission);

            results.Add(new
            {
                Test = "Invalid Age (18 years)",
                Status = "SavedWithRejection",
                SubmissionId = invalidResult.SubmissionId,
                ApprovalStatus = invalidResult.Status,
                RejectionReason = invalidResult.RejectionReason
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Invalid Age (18 years)",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 3: Inconsistent age (should be saved with rejection)
        try
        {
            var birthDate = DateTime.Now.AddYears(-30);

            var inconsistentAgeSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Inconsistent Age User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار عمر غير متطابق",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234569",
                    ["birthDate"] = birthDate.ToString("yyyy-MM-dd"), // 30 years old
                    ["age"] = "25", // But claims to be 25
                    ["citizenshipStatus"] = "مواطن",
                    ["hasMortgage"] = "لا",
                    ["monthlySalary"] = "7000",
                    ["monthlyCommitments"] = "1800",
                    ["nationalId"] = "1234567890",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var inconsistentResult = await _formService.SubmitFormAsync(formId, inconsistentAgeSubmission);

            results.Add(new
            {
                Test = "Inconsistent Age (birth date: 30, claimed: 25)",
                Status = "SavedWithRejection",
                SubmissionId = inconsistentResult.SubmissionId,
                ApprovalStatus = inconsistentResult.Status,
                RejectionReason = inconsistentResult.RejectionReason
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Inconsistent Age",
                Status = "Error",
                Error = ex.Message
            });
        }

        return Ok(new
        {
            Message = "Age validation tests completed",
            Note = "All validation errors are now saved to database instead of throwing exceptions",
            Results = results
        });
    }

    [HttpPost("test-comprehensive-errors/{formId}")]
    public async Task<IActionResult> TestComprehensiveErrors(int formId)
    {
        var results = new List<object>();

        // Test 1: Validation errors only (format/age issues - should still be saved)
        try
        {
            var validationOnlySubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Validation Only User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار أخطاء تحقق فقط",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "invalid-phone", // Invalid phone format
                    ["birthDate"] = DateTime.Now.AddYears(-18).ToString("yyyy-MM-dd"), // Invalid age (18 years)
                    ["citizenshipStatus"] = "مواطن", // Valid citizenship
                    ["hasMortgage"] = "لا", // Valid mortgage status
                    ["monthlySalary"] = "8000", // Valid salary
                    ["monthlyCommitments"] = "2000", // Valid commitments (25% ratio - under limit)
                    ["age"] = "18",
                    ["nationalId"] = "1234567890",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var validationResult = await _formService.SubmitFormAsync(formId, validationOnlySubmission);

            results.Add(new
            {
                Test = "Validation Errors Only",
                Description = "Invalid phone + age, but valid business logic",
                Status = "SavedWithRejection",
                SubmissionId = validationResult.SubmissionId,
                ApprovalStatus = validationResult.Status,
                RejectionReason = validationResult.RejectionReason,
                RejectionReasonEn = validationResult.RejectionReasonEn,
                Note = "Should have validation errors but NO approval errors"
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Validation Errors Only",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 2: Approval errors only (citizenship/ratio issues - should be saved)
        try
        {
            var approvalOnlySubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Approval Only User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار أخطاء موافقة فقط",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234567", // Valid phone
                    ["birthDate"] = DateTime.Now.AddYears(-25).ToString("yyyy-MM-dd"), // Valid age (25 years)
                    ["citizenshipStatus"] = "مقيم", // Invalid citizenship (resident)
                    ["hasMortgage"] = "لا", // No mortgage
                    ["monthlySalary"] = "5000", // Valid salary
                    ["monthlyCommitments"] = "2500", // High commitments (50% ratio - over 43% limit)
                    ["age"] = "25",
                    ["nationalId"] = "1234567891",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var approvalResult = await _formService.SubmitFormAsync(formId, approvalOnlySubmission);

            results.Add(new
            {
                Test = "Approval Errors Only",
                Description = "Valid format but invalid citizenship + high commitment ratio",
                Status = "SavedWithRejection",
                SubmissionId = approvalResult.SubmissionId,
                ApprovalStatus = approvalResult.Status,
                RejectionReason = approvalResult.RejectionReason,
                RejectionReasonEn = approvalResult.RejectionReasonEn,
                Note = "Should have approval errors but NO validation errors"
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Approval Errors Only",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 3: Both validation AND approval errors (comprehensive test)
        try
        {
            var bothErrorsSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Both Errors User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار كلا الأخطاء",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "123-invalid", // Invalid phone format (validation error)
                    ["birthDate"] = "invalid-date", // Invalid date format (validation error)
                    ["citizenshipStatus"] = "مقيم", // Invalid citizenship (approval error)
                    ["hasMortgage"] = "نعم", // Has mortgage
                    ["monthlySalary"] = "abc", // Invalid salary format (validation error)
                    ["monthlyCommitments"] = "4000", // Would cause high ratio if salary was valid (approval error)
                    ["age"] = "30",
                    ["nationalId"] = "1234567892",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var bothResult = await _formService.SubmitFormAsync(formId, bothErrorsSubmission);

            results.Add(new
            {
                Test = "Both Validation AND Approval Errors",
                Description = "Invalid formats + invalid business logic - ALL errors should be captured",
                Status = "SavedWithRejection",
                SubmissionId = bothResult.SubmissionId,
                ApprovalStatus = bothResult.Status,
                RejectionReason = bothResult.RejectionReason,
                RejectionReasonEn = bothResult.RejectionReasonEn,
                Note = "Should have BOTH validation AND approval errors combined"
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Both Validation AND Approval Errors",
                Status = "Error",
                Error = ex.Message
            });
        }

        // Test 4: Valid submission (should be approved)
        try
        {
            var validSubmission = new SubmitFormDto
            {
                SubmittedBy = "Test Valid User",
                Values = new Dictionary<string, string>
                {
                    ["fullName"] = "اختبار صحيح تماما",
                    ["governorate"] = "الرياض",
                    ["maritalStatus"] = "متزوج",
                    ["phoneNumber"] = "966501234567", // Valid phone
                    ["birthDate"] = DateTime.Now.AddYears(-30).ToString("yyyy-MM-dd"), // Valid age (30 years)
                    ["citizenshipStatus"] = "مواطن", // Valid citizenship
                    ["hasMortgage"] = "لا", // No mortgage
                    ["monthlySalary"] = "10000", // Valid salary
                    ["monthlyCommitments"] = "3000", // Valid commitments (30% ratio - under limit)
                    ["age"] = "30",
                    ["nationalId"] = "1234567893",
                    ["nationalIdType"] = "بطاقة هوية وطنية"
                }
            };

            var validResult = await _formService.SubmitFormAsync(formId, validSubmission);

            results.Add(new
            {
                Test = "Valid Submission",
                Description = "All data is valid - should be approved",
                Status = "Approved",
                SubmissionId = validResult.SubmissionId,
                ApprovalStatus = validResult.Status,
                RejectionReason = validResult.RejectionReason,
                RejectionReasonEn = validResult.RejectionReasonEn,
                Note = "Should be approved with no errors"
            });
        } catch (Exception ex)
        {
            results.Add(new
            {
                Test = "Valid Submission",
                Status = "Error",
                Error = ex.Message
            });
        }

        return Ok(new
        {
            Message = "Comprehensive error handling test completed",
            Summary = "This test demonstrates that the system now captures ALL errors (validation + approval) and saves them to the database",
            FixExplanation = new
            {
                Problem =
                    "Previously, if there were validation errors, approval service was never called, so approval errors were never saved to database",
                Solution = "Now BOTH validation and approval services always run, and ALL errors are collected and saved to database together",
                Benefits = new[]
                {
                    "All errors are captured and saved to database",
                    "Users get complete feedback on all issues",
                    "No approval errors are lost due to validation errors",
                    "System provides comprehensive error reporting"
                }
            },
            Results = results
        });
    }
}
//#endif