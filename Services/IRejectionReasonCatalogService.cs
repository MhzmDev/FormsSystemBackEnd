using DynamicForm.Models.DTOs;

namespace DynamicForm.Services
{
    public interface IRejectionReasonCatalogService
    {
        Task<FormRejectionReasonsDto> GetPossibleRejectionReasonsForActiveFormAsync();
        Task<FormRejectionReasonsDto> GetPossibleRejectionReasonsForFormAsync(int formId);
    }
}