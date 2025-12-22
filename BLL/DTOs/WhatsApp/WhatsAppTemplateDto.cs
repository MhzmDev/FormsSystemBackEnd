namespace DynamicForm.BLL.DTOs.WhatsApp
{
    public partial class WhatsAppTemplateDto
    {
        public string UserId { get; set; } = string.Empty;
        public string CreateIfNotFound { get; set; } = "yes";
        public WhatsAppContentDto Content { get; set; } = new();
    }
}