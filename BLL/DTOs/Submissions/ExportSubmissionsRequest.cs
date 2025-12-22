using System.ComponentModel.DataAnnotations;

namespace DynamicForm.BLL.DTOs.Submissions
{
    public class ExportSubmissionsRequest
    {
        [Required(ErrorMessage = "سبب الرفض مطلوب")]
        public string RejectionReason { get; set; } = string.Empty;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}