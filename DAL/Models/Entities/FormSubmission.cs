using System.ComponentModel.DataAnnotations;

namespace DynamicForm.DAL.Models.Entities
{
    public class FormSubmission
    {
        [Key]
        public int SubmissionId { get; set; }

        public int FormId { get; set; }
        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? SubmittedBy { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "جديد";

        [StringLength(1000)]
        public string? RejectionReason { get; set; }

        [StringLength(1000)]
        public string? RejectionReasonEn { get; set; }

        // Navigation properties
        public Form Form { get; set; } = null!;
        public ICollection<FormSubmissionValue> FormSubmissionValues { get; set; } = new List<FormSubmissionValue>();
    }
}