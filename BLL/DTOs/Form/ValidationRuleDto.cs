namespace DynamicForm.BLL.DTOs.Form
{
    public class ValidationRuleDto
    {
        public string? Operator { get; set; } // For numbers/text: =, >, <, <=, >=, !=
        public string? ValidValue { get; set; } // The valid option/value
        public bool IsValid { get; set; } = true; // For dropdown/checkbox: true=valid, false=invalid
        public string? ErrorMessageAr { get; set; } // Custom Arabic error message
        public string? ErrorMessageEn { get; set; } // Custom English error message
    }
}