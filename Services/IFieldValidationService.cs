using DynamicForm.Models;
using DynamicForm.Models.DTOs;
using DynamicForm.Models.Entities;

namespace DynamicForm.Services
{
    public interface IFieldValidationService
    {
        Task<(List<string> ArErrors, List<string> EnErrors)> ValidateFieldValueAsync(FormField field, string value);
        Task<ValidationResult> ValidateDynamicRulesAsync(Dictionary<string, string> values, IEnumerable<FormField> formFields);
    }
}