namespace DynamicForm.BLL.DTOs.WhatsApp
{
    public class WhatsAppContentDto
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Lang { get; set; } = "ar";
        public WhatsAppParamsDto Params { get; set; } = new();
    }
}