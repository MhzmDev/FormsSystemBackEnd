namespace DynamicForm.BLL.DTOs.Submissions.RejectionReasons
{
    /// <summary>
    ///     For the existing submission-based endpoint (keep this)
    /// </summary>
    public class RejectionReasonSummaryDto
    {
        public string ReasonAr { get; set; } = string.Empty;
        public string ReasonEn { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime? LastOccurrence { get; set; }
    }
}