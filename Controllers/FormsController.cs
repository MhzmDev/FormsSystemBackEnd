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
        ///     Get currently active form
        /// </summary>
        /// <response code="200">Returns the active form - إرجاع النموذج النشط</response>
        /// <response code="404">No active form found - لا يوجد نموذج نشط</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpGet("GetActiveForm")]
        [ProducesResponseType(typeof(ApiResponse<FormDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormDto>> GetActiveForm()
        {
            try
            {
                var form = await _formService.GetActiveFormAsync();

                if (form == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "لا يوجد نموذج نشط حالياً"
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
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "حدث خطأ في النظام",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        ///     Get all forms
        /// </summary>
        /// <response code="200">Returns the list of forms - إرجاع قائمة النماذج</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpGet("GetAllForms")]
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
        ///     Get specific form by ID
        /// </summary>
        /// <param name="id">Form ID </param>
        /// <response code="200">Returns the form details - إرجاع تفاصيل النموذج</response>
        /// <response code="404">Form not found - النموذج غير موجود</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpGet("GetFormByID/{id}")]
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
        ///     Create new form
        /// </summary>
        /// <param name="createFormDto">Form creation data - بيانات إنشاء النموذج</param>
        /// <response code="201">Form created successfully - تم إنشاء النموذج بنجاح</response>
        /// <response code="400">Validation error - خطأ في التحقق</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpPost("CreateNewForm")]
        [ProducesResponseType(typeof(ApiResponse<FormDto>), StatusCodes.Status201Created)]
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
                        Message = "تم إنشاء النموذج بنجاح. تم إلغاء تفعيل النماذج السابقة.",
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
        /// <param name="id">Form ID to update - معرف النموذج للتحديث</param>
        /// <param name="updateFormDto">Form update data - بيانات تحديث النموذج</param>
        /// <response code="200">Form updated successfully - تم تحديث النموذج بنجاح</response>
        /// <response code="404">Form not found - النموذج غير موجود</response>
        /// <response code="500">Server Error - خطأ في الخادم</response>
        [HttpPut("UpdateForm/{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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
                    Message = "تم تحديث النموذج بنجاح. الحقول الإلزامية لا يمكن تعديلها.",
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
        ///     Delete form (soft delete)
        /// </summary>
        /// <param name="id">Form ID to delete</param>
        [HttpDelete("DeleteForm/{id}")]
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
        /// <param name="id">Form ID to submit data to</param>
        /// <param name="submitFormDto">Form submission data </param>
        [HttpPost("SubmitForm/{id}")]
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

        /// <summary>
        ///     Activate a specific form (deactivates all others)
        /// </summary>
        /// <param name="id">Form ID to activate</param>
        /// <response code="200">Form activated successfully - تم تفعيل النموذج بنجاح</response>
        /// <response code="404">Form not found - النموذج غير موجود</response>
        /// <response code="500">Server error - خطأ في الخادم</response>
        [HttpPut("{id}/activate")]
        [SwaggerOperation(
            Summary = "Activate form - تفعيل النموذج",
            Description = "Activates the specified form and deactivates all others (only one form can be active at a time)",
            OperationId = "ActivateForm"
        )]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ActivateForm(int id)
        {
            try
            {
                var result = await _formService.ActivateFormAsync(id);

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
                    Message = "تم تفعيل النموذج بنجاح. تم إلغاء تفعيل النماذج الأخرى."
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
        ///     Update a specific form field (custom fields only)
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="fieldId">Field ID to update</param>
        /// <param name="updateFieldDto">Field update data</param>
        [HttpPut("{formId}/fields/{fieldId}")]
        [ProducesResponseType(typeof(ApiResponse<FormFieldDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormFieldDto>> UpdateFormField(int formId, int fieldId, [FromBody] UpdateFormFieldDto updateFieldDto)
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

                var field = await _formService.UpdateFormFieldAsync(formId, fieldId, updateFieldDto);

                if (field == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "النموذج أو الحقل غير موجود"
                    });
                }

                return Ok(new ApiResponse<FormFieldDto>
                {
                    Success = true,
                    Message = "تم تحديث الحقل بنجاح",
                    Data = field
                });
            }
            catch (InvalidOperationException ex)
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

        /// <summary>
        ///     Delete a specific form field (custom fields only)
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="fieldId">Field ID to delete</param>
        [HttpDelete("{formId}/fields/{fieldId}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteFormField(int formId, int fieldId)
        {
            try
            {
                var result = await _formService.DeleteFormFieldAsync(formId, fieldId);

                if (!result)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "النموذج أو الحقل غير موجود"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "تم حذف الحقل بنجاح"
                });
            }
            catch (InvalidOperationException ex)
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

        /// <summary>
        ///     Add a new field to an existing form
        /// </summary>
        /// <param name="formId">Form ID</param>
        /// <param name="createFieldDto">Field creation data</param>
        [HttpPost("{formId}/fields")]
        [ProducesResponseType(typeof(ApiResponse<FormFieldDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FormFieldDto>> AddFormField(int formId, [FromBody] CreateFormFieldDto createFieldDto)
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

                var field = await _formService.AddFormFieldAsync(formId, createFieldDto);

                return CreatedAtAction(nameof(GetForm), new { id = formId },
                    new ApiResponse<FormFieldDto>
                    {
                        Success = true,
                        Message = "تم إضافة الحقل بنجاح",
                        Data = field
                    });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
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