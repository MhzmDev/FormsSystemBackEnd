namespace DynamicForm.Models.DTOs.WhatsApp
{
    public class CreateSubscriberDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = "male";
    }

    public class WhatsAppTemplateDto
    {
        public string UserId { get; set; } = string.Empty;
        public string CreateIfNotFound { get; set; } = "yes";
        public WhatsAppContentDto Content { get; set; } = new();
    }

    public class WhatsAppContentDto
    {
        public string Namespace { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Lang { get; set; } = "ar";
        public WhatsAppParamsDto Params { get; set; } = new();
    }

    public class WhatsAppParamsDto
    {
        public string BODY_1 { get; set; } = string.Empty;
        public string BODY_2 { get; set; } = string.Empty;
        public string BODY_3 { get; set; } = string.Empty;
        public string BODY_4 { get; set; } = string.Empty;
        public string BODY_5 { get; set; } = string.Empty;
        public string BODY_6 { get; set; } = string.Empty;
        public string BODY_7 { get; set; } = string.Empty;
        public string BODY_8 { get; set; } = string.Empty;
        public string QUICK_REPLY_1 { get; set; } = "f146755s2591243";
    }
}