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
        public bool IsActive { get; set; }
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
        public List<FieldValueDto> Values { get; set; } = new List<FieldValueDto>();
    }

    public class FormSubmissionSummaryDto
    {
        public int SubmissionId { get; set; }
        public int FormId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SubmittedBy { get; set; }
        public string Preview { get; set; } = string.Empty;

        // Mandatory fields for quick access
        public string Id { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Salary { get; set; } = string.Empty;
        public string MonthlySpent { get; set; } = string.Empty;
        public string FormStatus { get; set; } = string.Empty;
        public string CreationDate { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class FieldValueDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
    }

    public class SubmitFormDto
    {
        public string? SubmittedBy { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }
}