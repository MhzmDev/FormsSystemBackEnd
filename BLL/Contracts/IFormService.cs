using DynamicForm.BLL.DTOs.Form;
using DynamicForm.BLL.DTOs.General;
using DynamicForm.BLL.DTOs.Submissions;

namespace DynamicForm.BLL.Contracts
{
    public interface IFormService
    {
        Task<FormDto?> GetFormByIdAsync(int formId);
        Task<FormDto?> GetActiveFormAsync();
        Task<PagedResult<FormDto>> GetAllFormsAsync(int pageIndex, int pageSize, bool? isActive);
        Task<FormDto> CreateFormAsync(CreateFormDto createFormDto);
        Task<FormDto?> UpdateFormAsync(int formId, UpdateFormDto updateFormDto);
        Task<FormDto?> ActivateFormAsync(int formId);
        Task<bool> DeleteFormAsync(int formId);
        Task<FormSubmissionResponseDto> SubmitFormAsync(int formId, SubmitFormDto submitFormDto);
        Task<FormSubmissionResponseDto> SubmitFormTestAsync(int formId, SubmitFormDto submitFormDto);
    }
}