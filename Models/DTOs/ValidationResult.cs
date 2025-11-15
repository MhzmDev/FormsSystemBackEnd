namespace DynamicForm.Models.DTOs
{
    public class ValidationResult
    {
        public List<string> AllErrors { get; set; } = new List<string>();
        public List<string> AllErrorsEn { get; set; } = new List<string>();
        public bool HasErrors => AllErrors.Any();
    }
}