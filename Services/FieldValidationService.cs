using DynamicForm.Models;
using DynamicForm.Models.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DynamicForm.Services
{
    public class FieldValidationService : IFieldValidationService
    {
        private readonly ILogger<FieldValidationService> _logger;

        public FieldValidationService(ILogger<FieldValidationService> logger)
        {
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateDynamicRulesAsync(Dictionary<string, string> values, IEnumerable<FormField> formFields)
        {
            var result = new ValidationResult();

            // Validate each field against its validation rules
            foreach (var field in formFields.Where(f => !string.IsNullOrEmpty(f.ValidationRules)))
            {
                if (values.TryGetValue(field.FieldName, out var fieldValue))
                {
                    var validationErrors = await ValidateFieldValueAsync(field, fieldValue);
                    result.AllErrors.AddRange(validationErrors.ArErrors);
                    result.AllErrorsEn.AddRange(validationErrors.EnErrors);
                }
            }

            return result;
        }

        public async Task<(List<string> ArErrors, List<string> EnErrors)> ValidateFieldValueAsync(FormField field, string value)
        {
            var arErrors = new List<string>();
            var enErrors = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(field.ValidationRules))
                {
                    return (arErrors, enErrors);
                }

                var validationRule = JsonSerializer.Deserialize<ValidationRuleDto>(field.ValidationRules);

                if (validationRule == null)
                {
                    return (arErrors, enErrors);
                }

                switch (field.FieldType?.ToLower())
                {
                    case "dropdown":
                    case "checkbox":
                        await ValidateDropdownCheckboxField(field, value, validationRule, arErrors, enErrors);

                        break;

                    case "text":
                        ValidateTextField(field, value, validationRule, arErrors, enErrors);

                        break;

                    case "number":
                        ValidateNumberField(field, value, validationRule, arErrors, enErrors);

                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating field {FieldName}", field.FieldName);
            }

            return (arErrors, enErrors);
        }

        private async Task ValidateDropdownCheckboxField(FormField field, string value, ValidationRuleDto rule, List<string> arErrors, List<string> enErrors)
        {
            if (string.IsNullOrEmpty(rule.ValidValue))
            {
                return;
            }

            bool isValueValid = value.Equals(rule.ValidValue, StringComparison.OrdinalIgnoreCase);

            // If rule says this value should be valid (IsValid = true) but it's not the valid value
            // OR if rule says this value should be invalid (IsValid = false) but it matches the invalid value
            if ((rule.IsValid && !isValueValid) || (!rule.IsValid && isValueValid))
            {
                // Use custom error messages if provided, otherwise fall back to default
                var defaultArError = $"القيمة المختارة في حقل '{field.Label}' غير مقبولة: {value}";
                var defaultEnError = $"Selected value in '{field.FieldName}' field is not acceptable: {value}";

                arErrors.Add(!string.IsNullOrEmpty(rule.ErrorMessageAr) ? rule.ErrorMessageAr : defaultArError);
                enErrors.Add(!string.IsNullOrEmpty(rule.ErrorMessageEn) ? rule.ErrorMessageEn : defaultEnError);
            }

            await Task.CompletedTask;
        }

        private void ValidateTextField(FormField field, string value, ValidationRuleDto rule, List<string> arErrors, List<string> enErrors)
        {
            if (string.IsNullOrEmpty(rule.ValidValue) || string.IsNullOrEmpty(rule.Operator))
            {
                return;
            }

            bool isValid = rule.Operator.ToLower() switch
            {
                "=" => value.Equals(rule.ValidValue, StringComparison.OrdinalIgnoreCase),
                "!=" => !value.Equals(rule.ValidValue, StringComparison.OrdinalIgnoreCase),
                _ => true // Unknown operator, pass validation
            };

            if (!isValid)
            {
                var defaultArError = $"قيمة حقل '{field.Label}' يجب أن تكون {rule.Operator} '{rule.ValidValue}' (القيمة المدخلة: {value})";
                var defaultEnError = $"Field '{field.FieldName}' value must be {rule.Operator} '{rule.ValidValue}' (provided: {value})";

                arErrors.Add(!string.IsNullOrEmpty(rule.ErrorMessageAr) ? rule.ErrorMessageAr : defaultArError);
                enErrors.Add(!string.IsNullOrEmpty(rule.ErrorMessageEn) ? rule.ErrorMessageEn : defaultEnError);
            }
        }

        private void ValidateNumberField(FormField field, string value, ValidationRuleDto rule, List<string> arErrors, List<string> enErrors)
        {
            if (string.IsNullOrEmpty(rule.ValidValue) || string.IsNullOrEmpty(rule.Operator))
            {
                return;
            }

            if (!decimal.TryParse(value, out var numericValue) || !decimal.TryParse(rule.ValidValue, out var validNumericValue))
            {
                var defaultArError = $"قيمة حقل '{field.Label}' يجب أن تكون رقماً صحيحاً";
                var defaultEnError = $"Field '{field.FieldName}' must be a valid number";

                arErrors.Add(!string.IsNullOrEmpty(rule.ErrorMessageAr) ? rule.ErrorMessageAr : defaultArError);
                enErrors.Add(!string.IsNullOrEmpty(rule.ErrorMessageEn) ? rule.ErrorMessageEn : defaultEnError);

                return;
            }

            bool isValid = rule.Operator switch
            {
                "=" => numericValue == validNumericValue,
                ">" => numericValue > validNumericValue,
                "<" => numericValue < validNumericValue,
                ">=" => numericValue >= validNumericValue,
                "<=" => numericValue <= validNumericValue,
                "!=" => numericValue != validNumericValue,
                _ => true // Unknown operator, pass validation
            };

            if (!isValid)
            {
                var defaultArError = $"قيمة حقل '{field.Label}' يجب أن تكون {GetArabicOperator(rule.Operator)} {rule.ValidValue} (القيمة المدخلة: {value})";
                var defaultEnError = $"Field '{field.FieldName}' must be {rule.Operator} {rule.ValidValue} (provided: {value})";

                arErrors.Add(!string.IsNullOrEmpty(rule.ErrorMessageAr) ? rule.ErrorMessageAr : defaultArError);
                enErrors.Add(!string.IsNullOrEmpty(rule.ErrorMessageEn) ? rule.ErrorMessageEn : defaultEnError);
            }
        }

        private string GetArabicOperator(string @operator)
        {
            return @operator switch
            {
                "=" => "يساوي",
                ">" => "أكبر من",
                "<" => "أصغر من",
                ">=" => "أكبر من أو يساوي",
                "<=" => "أصغر من أو يساوي",
                "!=" => "لا يساوي",
                _ => @operator
            };
        }
    }
}