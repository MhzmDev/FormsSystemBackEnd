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

    public class SubmissionValueSummaryDto
    {
        public string ArLabel { get; set; } = string.Empty;
        public string EnLabel { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
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
        public string? PhoneNumber { get; set; }
        public string Preview { get; set; } = string.Empty; // Summary of key values
        public string? RejectionReason { get; set; }
        public string? RejectionReasonEn { get; set; }
        public List<SubmissionValueSummaryDto> Values { get; set; } = new List<SubmissionValueSummaryDto>();
    }

    // Pagination support
    public class PagedResultSubmission<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int TodaySubmissionsCount { get; set; } // Count of submissions today
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
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

    // Status update DTO
    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}