using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DynamicForm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize] // ✅ Require authentication for all submission endpoints
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        /// <summary>
        ///     Get all submissions across all forms
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
        /// <param name="fromDate">Filter submissions from this date</param>
        /// <param name="toDate">Filter submissions until this date</param>
        /// <param name="status">Filter by status</param>
        /// <param name="isActive">
        ///     Filter by form active status (true for active forms, false for inactive forms, null for all
        ///     forms)
        /// </param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResultSubmission<FormSubmissionSummaryDto>>> GetAllSubmissions([FromQuery] int page = 1,
            [FromQuery] int pageSize = 10, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null, [FromQuery] bool? isActive = null)
        {
            try
            {
                // validate pagination
                if (page < 1)
                {
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "رقم الصفحة يجب أن يكون أكبر من أو يساوي 1" });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "عدد العناصر في الصفحة يجب أن يكون بين 1 و 100" });
                }

                var submissions = await _submissionService.GetAllSubmissionsAsync(page, pageSize, fromDate, toDate, status, isActive);

                return Ok(new ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>
                {
                    Success = true, Message = "تم جلب جميع المرسلات بنجاح", Data = submissions
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "حدث خطأ في النظام", Error = ex.Message });
            }
        }

        /// <summary>
        ///     Get all submissions for the currently active form
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
        /// <param name="fromDate">Filter submissions from this date</param>
        /// <param name="toDate">Filter submissions until this date</param>
        /// <param name="status">Filter by status</param>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResultSubmission<FormSubmissionSummaryDto>>> GetActiveFormSubmissions([FromQuery] int page = 1,
            [FromQuery] int pageSize = 10, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null)
        {
            try
            {
                // validate pagination
                if (page < 1)
                {
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "رقم الصفحة يجب أن يكون أكبر من أو يساوي 1" });
                }

                if (pageSize < 1 || pageSize > 50)
                {
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "عدد العناصر في الصفحة يجب أن يكون بين 1 و 50" });
                }

                var submissions = await _submissionService.GetActiveFormSubmissionsAsync(page, pageSize, fromDate, toDate, status);

                if (!submissions.Items.Any())
                {
                    return Ok(new ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>
                    {
                        Success = true, Message = "لا توجد مرسلات للنموذج النشط حاليًا", Data = submissions
                    });
                }

                return Ok(new ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>
                {
                    Success = true, Message = "تم جلب مرسلات النموذج النشط بنجاح", Data = submissions
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "حدث خطأ في النظام", Error = ex.Message });
            }
        }

        /// <summary>
        ///     Get all submissions for a specific form
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 10, max: 50)</param>
        /// <param name="fromDate">Filter submissions from this date</param>
        /// <param name="toDate">Filter submissions until this date</param>
        /// <param name="status">Filter by status</param>
        [HttpGet("form/{formId}")]
        [ProducesResponseType(typeof(ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>>> GetSubmissionsByFormId(int formId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1)
                {
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "رقم الصفحة يجب أن يكون أكبر من 0" });
                }

                if (pageSize < 1 || pageSize > 50)
                {
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "حجم الصفحة يجب أن يكون بين 1 و 50" });
                }

                var submissions = await _submissionService.GetSubmissionsByFormIdAsync(formId, page, pageSize, fromDate, toDate, status);

                return Ok(new ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>
                {
                    Success = true, Message = $"تم جلب مرسلات النموذج رقم {formId} بنجاح", Data = submissions
                });
            } catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "حدث خطأ في النظام", Error = ex.Message });
            }
        }

        /// <summary>
        ///     Get specific submission with full details
        /// </summary>
        [HttpGet("{id}", Name = "GetSubmissionById")]
        [ProducesResponseType(typeof(ApiResponse<FormSubmissionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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
        ///     Update submission status
        /// </summary>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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
        ///     Delete submission (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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
        ///     Get submissions by rejection reason
        /// </summary>
        [HttpGet("by-rejection-reason")]
        [ProducesResponseType(typeof(ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>>> GetSubmissionsByRejectionReason(
            [FromQuery] string rejectionReason,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "يجب تحديد سبب الرفض"
                    });
                }

                if (page < 1)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "رقم الصفحة يجب أن يكون أكبر من 0"
                    });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "حجم الصفحة يجب أن يكون بين 1 و 100"
                    });
                }

                var submissions = await _submissionService.GetSubmissionsByRejectionReasonAsync(
                    rejectionReason, page, pageSize, fromDate, toDate);

                return Ok(new ApiResponse<PagedResultSubmission<FormSubmissionSummaryDto>>
                {
                    Success = true,
                    Message = $"تم جلب الطلبات المرفوضة بسبب '{rejectionReason}' بنجاح",
                    Data = submissions
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
        ///     Export submissions by rejection reason to CSV and send via email
        /// </summary>
        [HttpPost("export-by-rejection-reason")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> ExportSubmissionsByRejectionReason(
            [FromBody] ExportSubmissionsRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "يجب تحديد سبب الرفض"
                    });
                }

                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "لم يتم العثور علي بريد الكتروني من المستخدم الحالي"
                    });
                }

                var result = await _submissionService.ExportSubmissionsToCSVAndEmailAsync(request.RejectionReason, request.FromDate, request.ToDate,
                    userEmail);

                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "لا توجد طلبات مرفوضة بهذا السبب أو حدث خطأ أثناء التصدير"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"تم إرسال التقرير بنجاح إلى {userEmail}"
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