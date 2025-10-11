using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models
{
    public class FormSubmission
    {
        [Key]
        public int SubmissionId { get; set; }

        public int FormId { get; set; }
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? SubmittedBy { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "مُرسل";

        // Navigation properties
        public Form Form { get; set; } = null!;
        public ICollection<FormSubmissionValue> FormSubmissionValues { get; set; } = new List<FormSubmissionValue>();
    }
}