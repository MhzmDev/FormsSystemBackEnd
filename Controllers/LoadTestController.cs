using Microsoft.AspNetCore.Mvc;
using DynamicForm.Services;
using DynamicForm.Models.DTOs;

namespace DynamicForm.Controllers;

#if DEBUG
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class LoadTestController : ControllerBase
{
    private readonly IFormService _formService;
    private readonly ILogger<LoadTestController> _logger;

    public LoadTestController(IFormService formService, ILogger<LoadTestController> logger)
    {
        _formService = formService;
        _logger = logger;
    }

    [HttpPost("simulate-submissions/{formId}")]
    public async Task<IActionResult> SimulateSubmissions(
        int formId,
        [FromQuery] int durationMinutes = 1,
        [FromQuery] int intervalSeconds = 5,
        [FromQuery] bool includeAgeErrors = false)
    {
        var totalRequests = (durationMinutes * 60) / intervalSeconds;
        var results = new List<object>();
        var successCount = 0;
        var errorCount = 0;
        var ageRejectedCount = 0;
        var startTime = DateTime.Now;

        _logger.LogInformation("Starting load test: {TotalRequests} requests over {Duration} minutes. Include age errors: {IncludeAgeErrors}",
            totalRequests, durationMinutes, includeAgeErrors);

        for (int i = 1; i <= totalRequests; i++)
        {
            try
            {
                var submitDto = GenerateTestSubmission(i, includeAgeErrors);
                var submission = await _formService.SubmitFormAsync(formId, submitDto);

                // Check if submission was rejected due to age validation
                if (submission.Status == "مرفوض" && submission.RejectionReason?.Contains("العمر") == true)
                {
                    ageRejectedCount++;

                    results.Add(new
                    {
                        RequestNumber = i,
                        Status = "AgeRejected",
                        SubmissionId = submission.SubmissionId,
                        RejectionReason = submission.RejectionReason,
                        ApprovalStatus = submission.Status,
                        Timestamp = DateTime.Now
                    });

                    _logger.LogInformation("Request {RequestNumber}/{Total} - Age validation error saved to DB (ID: {SubmissionId})",
                        i, totalRequests, submission.SubmissionId);
                }
                else
                {
                    successCount++;

                    results.Add(new
                    {
                        RequestNumber = i,
                        Status = "Success",
                        SubmissionId = submission.SubmissionId,
                        ApprovalStatus = submission.Status,
                        Timestamp = DateTime.Now
                    });

                    _logger.LogInformation("Request {RequestNumber}/{Total} completed successfully (Status: {Status})",
                        i, totalRequests, submission.Status);
                }
            }
            catch (Exception ex)
            {
                errorCount++;

                results.Add(new
                {
                    RequestNumber = i,
                    Status = "Error",
                    Error = ex.Message,
                    Timestamp = DateTime.Now
                });

                _logger.LogError(ex, "Request {RequestNumber}/{Total} failed", i, totalRequests);
            }

            // Wait for the specified interval (except for the last request)
            if (i < totalRequests)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));
            }
        }

        var endTime = DateTime.Now;
        var duration = endTime - startTime;

        return Ok(new
        {
            LoadTestSummary = new
            {
                TotalRequests = totalRequests,
                SuccessfulRequests = successCount,
                AgeRejectedRequests = ageRejectedCount,
                FailedRequests = errorCount,
                SuccessRate = (double)successCount / totalRequests * 100,
                AgeRejectionRate = (double)ageRejectedCount / totalRequests * 100,
                Duration = duration.TotalSeconds,
                RequestsPerSecond = totalRequests / duration.TotalSeconds,
                Note = "Age-rejected submissions are now saved to database with rejection status"
            },
            Results = results
        });
    }

    private SubmitFormDto GenerateTestSubmission(int requestNumber, bool includeAgeErrors = false)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Generate age-related errors for some requests if requested
        var birthDate = includeAgeErrors && requestNumber % 3 == 0
            ? DateTime.Now.AddYears(-18) // Under 20 years old to trigger age validation
            : DateTime.Now.AddYears(-25); // Valid age (over 20)

        var calculatedAge = DateTime.Now.Year - birthDate.Year;

        if (birthDate.Date > DateTime.Now.AddYears(-calculatedAge))
        {
            calculatedAge--;
        }

        // Sometimes provide inconsistent age if includeAgeErrors is true
        var providedAge = includeAgeErrors && requestNumber % 5 == 0
            ? calculatedAge + 10 // Inconsistent age to trigger validation
            : calculatedAge; // Consistent age

        return new SubmitFormDto
        {
            SubmittedBy = $"LoadTest_User_{requestNumber}_{timestamp}",
            Values = new Dictionary<string, string>
            {
                ["fullName"] = $"اختبار الحمولة رقم {requestNumber}",
                ["phoneNumber"] = $"966501234{requestNumber:D3}",
                ["birthDate"] = birthDate.ToString("yyyy-MM-dd"),
                ["age"] = providedAge.ToString(),
                ["citizenshipStatus"] = "مواطن",
                ["hasMortgage"] = requestNumber % 2 == 0 ? "نعم" : "لا",
                ["monthlySalary"] = $"{5000 + (requestNumber * 100)}",
                ["monthlyCommitments"] = $"{1000 + (requestNumber * 50)}"
            }
        };
    }
}

#endif