using Microsoft.AspNetCore.Mvc;
using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace DynamicForm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Tags("Forms Management - إدارة النماذج")]
    public class FormsController : ControllerBase
    {
        private readonly IFormService _formService;

        public FormsController(IFormService formService)
        {
            _formService = formService;
        }

        /// <summary>
        ///     Get all active forms - الحصول على جميع النماذج النشطة
        /// </summary>
        /// <response code="200">Returns the list of forms - إرجاع قائمة النماذج</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get all forms - الحصول على جميع النماذج",
            Description = "Retrieves all active forms with their field definitions",
            OperationId = "GetAllForms",
            Tags = new[] { "Forms Management - إدارة النماذج" }
        )]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FormDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<FormDto>>> GetAllForms()
        {
            try
            {
                var forms = await _formService.GetAllFormsAsync();

                return Ok(new ApiResponse<IEnumerable<FormDto>>
                {
                    Success = true,
                    Message = "تم جلب النماذج بنجاح",
                    Data = forms
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
        ///     Get specific form by ID - الحصول على نموذج محدد بالمعرف
        /// </summary>
        /// <param name="id">Form ID - معرف النموذج</param>
        /// <response code="200">Returns the form details - إرجاع تفاصيل النموذج</response>
        /// <response code="404">Form not found - النموذج غير موجود</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get form by ID - الحصول على النموذج بالمعرف",
            Description = "Retrieves detailed information about a specific form",
            OperationId = "GetFormById"
        )]
        [ProducesResponseType(typeof(ApiResponse<FormDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormDto>> GetForm(int id)
        {
            try
            {
                var form = await _formService.GetFormByIdAsync(id);

                if (form == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "النموذج غير موجود"
                    });
                }

                return Ok(new ApiResponse<FormDto>
                {
                    Success = true,
                    Message = "تم جلب النموذج بنجاح",
                    Data = form
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
        ///     Submit form data - إرسال بيانات النموذج
        /// </summary>
        /// <param name="id">Form ID - معرف النموذج</param>
        /// <param name="submitFormDto">Form submission data - بيانات إرسال النموذج</param>
        /// <response code="201">Form submitted successfully - تم إرسال النموذج بنجاح</response>
        /// <response code="400">Validation error or missing required fields - خطأ في التحقق أو حقول مطلوبة مفقودة</response>
        /// <response code="404">Form not found - النموذج غير موجود</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpPost]
        [SwaggerOperation(
            Summary = "Submit form data - إرسال بيانات النموذج",
            Description = "Submits user data for a specific form",
            OperationId = "SubmitForm"
        )]
        [ProducesResponseType(typeof(ApiResponse<FormSubmissionResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormDto>> CreateForm([FromBody] CreateFormDto createFormDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "البيانات المدخلة غير صحيحة",
                        Errors = ModelState
                    });
                }

                var form = await _formService.CreateFormAsync(createFormDto);

                return CreatedAtAction(nameof(GetForm), new { id = form.FormId },
                    new ApiResponse<FormDto>
                    {
                        Success = true,
                        Message = "تم إنشاء النموذج بنجاح",
                        Data = form
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
        ///     تحديث نموذج موجود
        ///     Update existing form
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<FormDto>> UpdateForm(int id, [FromBody] UpdateFormDto updateFormDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "البيانات المدخلة غير صحيحة",
                        Errors = ModelState
                    });
                }

                var form = await _formService.UpdateFormAsync(id, updateFormDto);

                if (form == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "النموذج غير موجود"
                    });
                }

                return Ok(new ApiResponse<FormDto>
                {
                    Success = true,
                    Message = "تم تحديث النموذج بنجاح",
                    Data = form
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
        ///     حذف نموذج (إلغاء تفعيل)
        ///     Delete form (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteForm(int id)
        {
            try
            {
                var result = await _formService.DeleteFormAsync(id);

                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "النموذج غير موجود"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "تم حذف النموذج بنجاح"
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
        ///     إرسال بيانات النموذج
        ///     Submit form data
        /// </summary>
        [HttpPost("{id}/submit")]
        public async Task<ActionResult<FormSubmissionResponseDto>> SubmitForm(int id, [FromBody] SubmitFormDto submitFormDto)
        {
            try
            {
                var submission = await _formService.SubmitFormAsync(id, submitFormDto);

                return CreatedAtRoute("GetSubmissionById", new { id = submission.SubmissionId },
                    new ApiResponse<FormSubmissionResponseDto>
                    {
                        Success = true,
                        Message = "تم إرسال البيانات بنجاح",
                        Data = submission
                    });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
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