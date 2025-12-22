namespace DynamicForm.BLL.DTOs.Form
{
    public class FormFieldDto
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public List<string>? Options { get; set; }
        public int DisplayOrder { get; set; }
        public ValidationRuleDto? ValidationRules { get; set; } // ADD THIS LINE
    }
}