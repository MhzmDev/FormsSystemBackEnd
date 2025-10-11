using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models
{
    public class FormSubmissionValue
    {
        [Key]
        public int ValueId { get; set; }

        public int SubmissionId { get; set; }
        public int FieldId { get; set; }

        [Required]
        [StringLength(2000)]
        public string FieldValue { get; set; } = string.Empty;

        // Field snapshot at submission time - preserves original labels
        [Required]
        [StringLength(100)]
        public string FieldNameAtSubmission { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string FieldTypeAtSubmission { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string LabelAtSubmission { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string? OptionsAtSubmission { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public FormSubmission FormSubmission { get; set; } = null!;
        public FormField FormField { get; set; } = null!;
    }
}