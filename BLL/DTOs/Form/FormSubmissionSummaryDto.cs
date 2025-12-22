namespace DynamicForm.BLL.DTOs.Form;

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