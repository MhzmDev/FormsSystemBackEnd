namespace DynamicForm.BLL.DTOs.Submissions.RejectionReasons
{
    /// <summary>
    ///     Response containing all possible rejection reasons for a form
    /// </summary>
    public class FormRejectionReasonsDto
    {
        public int FormId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public List<PossibleRejectionReasonDto> PossibleReasons { get; set; } = new List<PossibleRejectionReasonDto>();
    }
}