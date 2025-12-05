namespace DynamicForm.Models.Configuration
{
    public class EmailSettings
    {
        public const string SectionName = "Email";
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(SmtpHost))
            {
                throw new InvalidOperationException("Email:SmtpHost configuration is missing");
            }

            if (SmtpPort <= 0)
            {
                throw new InvalidOperationException("Email:SmtpPort must be greater than 0");
            }

            if (string.IsNullOrWhiteSpace(SenderEmail))
            {
                throw new InvalidOperationException("Email:SenderEmail configuration is missing");
            }

            if (string.IsNullOrWhiteSpace(SenderPassword))
            {
                throw new InvalidOperationException("Email:SenderPassword configuration is missing");
            }

            if (string.IsNullOrWhiteSpace(SenderName))
            {
                throw new InvalidOperationException("Email:SenderName configuration is missing");
            }
        }
    }
}