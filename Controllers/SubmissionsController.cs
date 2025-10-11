using Microsoft.AspNetCore.Mvc;
using DynamicForm.Models.DTOs;
using DynamicForm.Services;

namespace DynamicForm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        /// <summary>
        /// الحصول على جميع المرسلات (عبر جميع النماذج)
        /// Get all submissions across all forms
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<FormSubmissionSummaryDto>>> GetAllSubmissions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var submissions = await _submissionService.GetAllSubmissionsAsync(page, pageSize, fromDate, toDate, status);
                return Ok(new ApiResponse<PagedResult<FormSubmissionSummaryDto>>
                {
                    Success = true,
                    Message = "تم جلب جميع المرسلات بنجاح",
                    Data = submissions
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
        /// الحصول على مرسلة محددة بالتفصيل
        /// Get specific submission with full details
        /// </summary>
        [HttpGet("{id}", Name = "GetSubmissionById")]
        public async Task<ActionResult<FormSubmissionResponseDto>> GetSubmission(int id)
        {
            try
            {
                var submission = await _submissionService.GetSubmissionByIdAsync(id);
                if (submission == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المرسلة غير موجودة"
                    });
                }

                return Ok(new ApiResponse<FormSubmissionResponseDto>
                {
                    Success = true,
                    Message = "تم جلب المرسلة بنجاح",
                    Data = submission
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
        /// تحديث حالة المرسلة
        /// Update submission status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateSubmissionStatus(int id, [FromBody] UpdateStatusDto updateStatusDto)
        {
            try
            {
                var result = await _submissionService.UpdateSubmissionStatusAsync(id, updateStatusDto.Status);
                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المرسلة غير موجودة"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "تم تحديث حالة المرسلة بنجاح"
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
        /// حذف مرسلة
        /// Delete submission (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSubmission(int id)
        {
            try
            {
                var result = await _submissionService.DeleteSubmissionAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "المرسلة غير موجودة"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "تم حذف المرسلة بنجاح"
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