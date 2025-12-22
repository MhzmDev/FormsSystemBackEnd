using System.ComponentModel.DataAnnotations;

namespace DynamicForm.BLL.DTOs.Submissions.RejectionReasons
{
    /// <summary>
    ///     Represents a possible rejection reason for a form (not from actual submissions)
    /// </summary>
    public class PossibleRejectionReasonDto
    {
        public string ReasonTextAr { get; set; } = string.Empty;
        public string ReasonTextEn { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // "Validation", "BusinessRule", "DynamicField"
        public string? FieldName { get; set; } // If related to a specific field

        //  Search pattern for partial matching
        public string SearchPatternAr { get; set; } = string.Empty;
        public string SearchPatternEn { get; set; } = string.Empty;
    }
}