using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models.DTOs
{
    public class ExportSubmissionsRequest
    {
        [Required(ErrorMessage = "سبب الرفض مطلوب")]
        public string RejectionReason { get; set; } = string.Empty;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}