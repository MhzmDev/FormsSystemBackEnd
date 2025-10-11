namespace DynamicForm.Models.DTOs
{
    // Remove FormId from submission DTO since it comes from route
    public class SubmitFormDto
    {
        public string? SubmittedBy { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    // Separate DTO for form updates
    public class UpdateFormDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateFormFieldDto> Fields { get; set; } = new List<CreateFormFieldDto>();
    }

    // Summary DTO for listing submissions
    public class FormSubmissionSummaryDto
    {
        public int SubmissionId { get; set; }
        public int FormId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SubmittedBy { get; set; }
        public string Preview { get; set; } = string.Empty; // Summary of key values
    }

    // Pagination support
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

    // Status update DTO
    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}