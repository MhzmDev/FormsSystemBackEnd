using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models.DTOs
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

        // ✅ NEW: Search pattern for partial matching
        public string SearchPatternAr { get; set; } = string.Empty;
        public string SearchPatternEn { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Response containing all possible rejection reasons for a form
    /// </summary>
    public class FormRejectionReasonsDto
    {
        public int FormId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public List<PossibleRejectionReasonDto> PossibleReasons { get; set; } = new List<PossibleRejectionReasonDto>();
    }

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