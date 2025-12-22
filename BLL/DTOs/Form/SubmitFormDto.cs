namespace DynamicForm.BLL.DTOs.Form
{
    // Remove FormId from submission DTO since it comes from route
    public class SubmitFormDto
    {
        public string? SubmittedBy { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }
}