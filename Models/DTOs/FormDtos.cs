namespace DynamicForm.Models.DTOs
{
    public class CreateFormDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateFormFieldDto> Fields { get; set; } = new List<CreateFormFieldDto>();
    }

    public class CreateFormFieldDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public List<string>? Options { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class FormDto
    {
        public int FormId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<FormFieldDto> Fields { get; set; } = new List<FormFieldDto>();
    }

    public class FormFieldDto
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public List<string>? Options { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class FormSubmissionDto
    {
        public int FormId { get; set; }
        public string? SubmittedBy { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    public class FormSubmissionResponseDto
    {
        public int SubmissionId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SubmittedBy { get; set; }
        public string? RejectionReason { get; set; }
        public string? RejectionReasonEn { get; set; }
        public List<FieldValueDto> Values { get; set; } = new List<FieldValueDto>();
    }

    public class FieldValueDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
    }
}