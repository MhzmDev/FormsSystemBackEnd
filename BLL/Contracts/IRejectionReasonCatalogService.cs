using DynamicForm.BLL.DTOs.Submissions.RejectionReasons;

namespace DynamicForm.BLL.Contracts
{
    public interface IRejectionReasonCatalogService
    {
        Task<FormRejectionReasonsDto> GetPossibleRejectionReasonsForActiveFormAsync();
        Task<FormRejectionReasonsDto> GetPossibleRejectionReasonsForFormAsync(int formId);
    }
}