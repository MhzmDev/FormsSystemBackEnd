using DynamicForm.BLL.DTOs.Form;

namespace DynamicForm.BLL.DTOs.Submissions
{
    public class FormSubmissionResponseDto
    {
        public int SubmissionId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? SubmittedBy { get; set; }
        public string? PhoneNumber { get; set; }
        public string? RejectionReason { get; set; }
        public string? RejectionReasonEn { get; set; }
        public List<FieldValueDto> Values { get; set; } = new List<FieldValueDto>();
    }
}