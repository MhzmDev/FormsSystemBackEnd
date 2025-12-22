namespace DynamicForm.BLL.DTOs.General
{
    // Pagination support
    public class PagedResultSubmission<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int TodaySubmissionsCount { get; set; } // Count of submissions today
        public int TodayApprovedSubmissionsCount { get; set; } //  Count of approved submissions today
        public int TodayRejectedSubmissionsCount { get; set; } //  Count of rejected submissions today
        public int ApprovedSubmissionsCount { get; set; } //  Count of approved submissions
        public int RejectedSubmissionsCount { get; set; } //  Count of rejected submissions
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}