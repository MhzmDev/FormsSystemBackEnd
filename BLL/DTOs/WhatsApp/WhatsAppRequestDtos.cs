namespace DynamicForm.BLL.DTOs.WhatsApp
{
    public class CreateSubscriberDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = "male";
    }
}