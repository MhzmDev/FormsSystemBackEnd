using DynamicForm.Models.DTOs;

namespace DynamicForm.Services
{
    public interface IFormService
    {
        Task<FormDto?> GetFormByIdAsync(int formId);
        Task<FormDto?> GetActiveFormAsync();
        Task<IEnumerable<FormDto>> GetAllFormsAsync();
        Task<FormDto> CreateFormAsync(CreateFormDto createFormDto);
        Task<FormDto?> UpdateFormAsync(int formId, UpdateFormDto updateFormDto);
        Task<bool> DeleteFormAsync(int formId);
        Task<FormSubmissionResponseDto> SubmitFormAsync(int formId, SubmitFormDto submitFormDto);
        Task<bool> ActivateFormAsync(int formId);
        
        // New field update methods
        Task<FormFieldDto?> UpdateFormFieldAsync(int formId, int fieldId, UpdateFormFieldDto updateFieldDto);
        Task<bool> DeleteFormFieldAsync(int formId, int fieldId);
        Task<FormFieldDto> AddFormFieldAsync(int formId, CreateFormFieldDto createFieldDto);
    }
}