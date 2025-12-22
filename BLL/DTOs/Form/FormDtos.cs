namespace DynamicForm.BLL.DTOs.Form
{

    public class FormDto
    {
        public int FormId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<FormFieldDto> Fields { get; set; } = new List<FormFieldDto>();
    }
}