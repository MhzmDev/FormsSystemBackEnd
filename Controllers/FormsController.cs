using Microsoft.AspNetCore.Mvc;
using DynamicForm.Models.DTOs;
using DynamicForm.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace DynamicForm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Tags("Forms Management")]
    public class FormsController : ControllerBase
    {
        private readonly IFormService _formService;

        public FormsController(IFormService formService)
        {
            _formService = formService;
        }

        /// <summary>
        ///     Get active form (only one)
        /// </summary>
        /// <returns></returns>
        [HttpGet("ActiveForm")]
        [ProducesDefaultResponseType(typeof(ApiResponse<PagedResult<FormDto>>))]
        [ProducesResponseType(typeof(ApiResponse<FormDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<FormDto>>> GetActiveForm()
        {
            try
            {
                var form = await _formService.GetActiveFormAsync();

                if (form == null)
                {
                    return Ok(new ApiResponse<FormDto>
                    {
                        Success = true,
                        Message = "لا يوجد نموذج نشط حالياً",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<FormDto>
                {
                    Success = true,
                    Message = "تم جلب النموذج النشط بنجاح",
                    Data = form
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<FormDto>
                {
                    Success = false,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        /// <summary>
        ///     Get all forms
        /// </summary>
        /// <param name="isActive">Filter by active status (optional). if empty, all forms will be returned</param>
        /// <response code="200">Returns the list of forms </response>
        /// <response code="500">Server error </response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FormDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<PagedResult<FormDto>>>> GetAllForms([FromQuery] bool? isActive = null, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageIndex < 1)
                {
                    return BadRequest(new ApiResponse<PagedResult<FormDto>>
                    {
                        Success = false,
                        Message = "رقم الصفحه يجب ان يكون 1 واقل من 50"
                    });
                }

                if (pageSize < 1 || pageSize > 50)
                {
                    return BadRequest(new ApiResponse<PagedResult<FormDto>>
                    {
                        Success = false,
                        Message = "حجم الصفحه يجب ان يكون بين 1 و 50"
                    });
                }

                var forms = await _formService.GetAllFormsAsync(pageIndex, pageSize, isActive);

                var message = isActive.HasValue
                    ? (isActive.Value ? "تم جلب النماذج النشطة بنجاح" : "تم جلب النماذج غير النشطة بنجاح")
                    : "تم جلب جميع النماذج بنجاح";

                return Ok(new ApiResponse<PagedResult<FormDto>>
                {
                    Success = true,
                    Message = message,
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
        ///     Get specific form by ID
        /// </summary>
        /// <param name="id">Form ID </param>
        /// <response code="200">Returns the form details - إرجاع تفاصيل النموذج</response>
        /// <response code="404">Form not found - النموذج غير موجود</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpGet("{id}")]
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
        ///     Create a New Form 
        /// </summary>
        /// <param name="id">Form ID - معرف النموذج</param>
        /// <param name="createFormDto">Form submission data - بيانات إرسال النموذج</param>
        /// <response code="201">Form submitted successfully - تم إرسال النموذج بنجاح</response>
        /// <response code="400">Validation error or missing required fields - خطأ في التحقق أو حقول مطلوبة مفقودة</response>
        /// <response code="404">Form not found - النموذج غير موجود</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FormSubmissionResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
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
        ///     Activate a form by Id (set a form as active, will deactivate others)
        /// </summary>
        /// <param name="id">Form Id</param>
        [HttpPost("{id}/activate")]
        [ProducesResponseType(typeof(ApiResponse<FormDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<FormDto>>> ActivateForm(int id)
        {
            try
            {
                var form = await _formService.ActivateFormAsync(id);

                if (form == null)
                {
                    return NotFound(new ApiResponse<FormDto>
                    {
                        Success = false,
                        Message = "النموذج غير موجود"
                    });
                }

                return Ok(new ApiResponse<FormDto>
                {
                    Success = true,
                    Message = "تم تفعيل النموذج بنجاح",
                    Data = form
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<FormDto>
                {
                    Success = false,
                    Message = $"حدث خطأ: {ex.Message}"
                });
            }
        }

        /// <summary>
        ///     Delete form by Form Id (soft delete)
        /// </summary>
        /// <param name="id">Form Id</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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
        ///     Submit form data
        /// </summary>
        /// <param name="id">Form Id</param>
        /// <param name="submitFormDto">Form submission data</param>
        [HttpPost("{id}/submit")]
        [ProducesResponseType(typeof(ApiResponse<FormSubmissionResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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