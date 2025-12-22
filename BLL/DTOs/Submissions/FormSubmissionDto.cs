namespace DynamicForm.BLL.DTOs.Submissions
{
    /// <summary>
    ///     Deprecated : Use FormSubmissionCreateDto instead.
    /// </summary>
    public class FormSubmissionDto
    {
        public int FormId { get; set; }
        public string? SubmittedBy { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }
}