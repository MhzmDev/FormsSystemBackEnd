namespace DynamicForm.Models.DTOs
{
    public class RejectionStatisticsDto
    {
        public string RejectionReason { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}