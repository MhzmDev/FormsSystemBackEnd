using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        /// Get rejected submissions with service duration less than 3 months
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
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? fromDate = null,
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
            }
            catch (Exception ex)
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
        /// Get rejection statistics grouped by rejection reason
        /// </summary>
        [HttpGet("statistics/by-reason")]
        [ProducesResponseType(typeof(ApiResponse<List<RejectionStatisticsDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<RejectionStatisticsDto>>>> GetRejectionStatistics(
            [FromQuery] DateTime? fromDate = null,
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
            }
            catch (Exception ex)
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