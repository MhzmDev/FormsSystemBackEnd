namespace DynamicForm.BLL.DTOs.Form
{
    public class CreateFormDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<CreateFormFieldDto>? Fields { get; set; } = new List<CreateFormFieldDto>();
    }
}