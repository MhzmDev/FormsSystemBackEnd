using DynamicForm.Models.DTOs;

namespace DynamicForm.Services
{
    public interface IFormService
    {
        Task<FormDto?> GetFormByIdAsync(int formId);
        Task<IEnumerable<FormDto>> GetAllFormsAsync();
        Task<FormDto> CreateFormAsync(CreateFormDto createFormDto);
        Task<FormDto?> UpdateFormAsync(int formId, UpdateFormDto updateFormDto);
        Task<bool> DeleteFormAsync(int formId);
        Task<FormSubmissionResponseDto> SubmitFormAsync(int formId, SubmitFormDto submitFormDto);
    }
}