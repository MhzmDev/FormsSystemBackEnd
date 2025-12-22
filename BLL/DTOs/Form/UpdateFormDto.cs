namespace DynamicForm.BLL.DTOs.Form
{
    // Separate DTO for form updates
    public class UpdateFormDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateFormFieldDto> Fields { get; set; } = new List<CreateFormFieldDto>();
    }
}