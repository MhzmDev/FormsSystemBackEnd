using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models
{
    public class FormField
    {
        [Key]
        public int FieldId { get; set; }

        public int FormId { get; set; }

        [Required]
        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FieldType { get; set; } = string.Empty; // text, email, dropdown, checkbox, date

        [Required]
        [StringLength(200)]
        public string Label { get; set; } = string.Empty; // Arabic labels

        public bool IsRequired { get; set; }

        [StringLength(1000)]
        public string? ValidationRules { get; set; } // JSON format

        [StringLength(2000)]
        public string? Options { get; set; } // JSON for dropdown/checkbox options in Arabic

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Form Form { get; set; } = null!;
        public ICollection<FormSubmissionValue> FormSubmissionValues { get; set; } = new List<FormSubmissionValue>();
    }
}