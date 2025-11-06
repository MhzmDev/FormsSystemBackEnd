namespace DynamicForm.Models.DTOs
{
    public class ApprovalResult
    {
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
        public string? RejectionReasonEn { get; set; }
        public string Status { get; set; } = "مُرسل";
    }
}