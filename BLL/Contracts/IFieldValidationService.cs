using DynamicForm.BLL.DTOs.Validaiton;
using DynamicForm.DAL.Models.Entities;

namespace DynamicForm.BLL.Contracts
{
    public interface IFieldValidationService
    {
        Task<(List<string> ArErrors, List<string> EnErrors)> ValidateFieldValueAsync(FormField field, string value);
        Task<ValidationResult> ValidateDynamicRulesAsync(Dictionary<string, string> values, IEnumerable<FormField> formFields);
    }
}