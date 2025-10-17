namespace DynamicForm.Models.DTOs
{
    // Separate DTO for form updates
    public class UpdateFormDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateFormFieldDto> Fields { get; set; } = new List<CreateFormFieldDto>();
    }

    // New DTO for updating individual form fields
    public class UpdateFormFieldDto
    {
        public string Label { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string>? Options { get; set; }
        public int DisplayOrder { get; set; }
        public string? ValidationRules { get; set; }
    }

    // Status update DTO
    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}