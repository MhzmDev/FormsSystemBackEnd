using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DynamicForm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class RejectionAnalyticsController : ControllerBase
    {
        private readonly IRejectionAnalyticsService _rejectionAnalyticsService;

        public RejectionAnalyticsController(IRejectionAnalyticsService rejectionAnalyticsService)
        {
            _rejectionAnalyticsService = rejectionAnalyticsService;
        }

        /// <summary>
        ///     Get rejected submissions with service duration less than 3 months
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
        /// <param name="fromDate">Filter submissions from this date</param>
        /// <param name="toDate">Filter submissions until this date</param>
        [HttpGet("service-duration-under-3-months")]
        [ProducesResponseType(typeof(ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>>> GetRejectedByServiceDuration(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "رقم الصفحة يجب أن يكون أكبر من أو يساوي 1"
                    });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "عدد العناصر في الصفحة يجب أن يكون بين 1 و 100"
                    });
                }

                var result = await _rejectionAnalyticsService.GetRejectedByServiceDurationAsync(
                    page, pageSize, fromDate, toDate);

                return Ok(new ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>
                {
                    Success = true,
                    Message = "تم جلب الطلبات المرفوضة بسبب مدة الخدمة بنجاح",
                    Data = result
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Get rejected submissions with service duration less than 3 months and optional 90-day filter
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
        /// <param name="fromDate">Filter submissions from this date</param>
        /// <param name="toDate">Filter submissions until this date</param>
        /// <param name="olderThan3Months">
        ///     Filter by 90-day threshold: true = older than 3 months, false = within last 3 months,
        ///     null = all submissions
        /// </param>
        /// <param name="sendEmail">Send CSV report via email to the currently logged-in user (optional, default: false)</param>
        [AllowAnonymous]
        [HttpGet("service-duration-under-3-months-90Days")]
        [ProducesResponseType(typeof(ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>>> GetRejectedByServiceDuration90Days(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] bool? olderThan3Months = null,
            [FromQuery] bool? sendEmail = null)
        {
            try
            {
                if (page < 1)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "رقم الصفحة يجب أن يكون أكبر من أو يساوي 1"
                    });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "عدد العناصر في الصفحة يجب أن يكون بين 1 و 100"
                    });
                }

                // Get recipient email from currently logged-in user if sendEmail is requested
                string? recipientEmail = null;

                if (sendEmail == true)
                {
                    recipientEmail = User.FindFirstValue(ClaimTypes.Email);

                    if (string.IsNullOrWhiteSpace(recipientEmail))
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message =
                                "لم يتم العثور على البريد الإلكتروني للمستخدم الحالي. يرجى التأكد من تسجيل الدخول بحساب يحتوي على بريد إلكتروني."
                        });
                    }
                }

                var result = await _rejectionAnalyticsService.GetRejectedByServiceDuration90DaysAsync(
                    page, pageSize, fromDate, toDate, olderThan3Months, sendEmail ?? false, recipientEmail);

                // Dynamic message based on filter and email
                var message = (olderThan3Months, sendEmail) switch
                {
                    (true, true) => $"تم جلب الطلبات المرفوضة التي مضى عليها أكثر من 3 أشهر بنجاح وسيتم إرسال التقرير إلى {recipientEmail}",
                    (false, true) => $"تم جلب الطلبات المرفوضة خلال آخر 3 أشهر بنجاح وسيتم إرسال التقرير إلى {recipientEmail}",
                    (null, true) => $"تم جلب الطلبات المرفوضة بنجاح وسيتم إرسال التقرير إلى {recipientEmail}",
                    (true, _) => "تم جلب الطلبات المرفوضة التي مضى عليها أكثر من 3 أشهر بنجاح",
                    (false, _) => "تم جلب الطلبات المرفوضة خلال آخر 3 أشهر بنجاح",
                    _ => "تم جلب الطلبات المرفوضة بسبب مدة الخدمة بنجاح"
                };

                return Ok(new ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>
                {
                    Success = true,
                    Message = message,
                    Data = result
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Get rejection statistics grouped by rejection reason
        /// </summary>
        [HttpGet("statistics/by-reason")]
        [ProducesResponseType(typeof(ApiResponse<List<RejectionStatisticsDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<RejectionStatisticsDto>>>> GetRejectionStatistics([FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var statistics = await _rejectionAnalyticsService.GetRejectionStatisticsAsync(fromDate, toDate);

                return Ok(new ApiResponse<List<RejectionStatisticsDto>>
                {
                    Success = true,
                    Message = "تم جلب إحصائيات الرفض بنجاح",
                    Data = statistics
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }
    }
}