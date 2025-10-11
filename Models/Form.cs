using System.ComponentModel.DataAnnotations;

namespace DynamicForm.Models
{
    public class Form
    {
        [Key]
        public int FormId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        public ICollection<FormField> FormFields { get; set; } = new List<FormField>();
        public ICollection<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();
    }
}