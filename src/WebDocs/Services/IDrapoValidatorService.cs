using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    /// <summary>
    /// Validates a Drapo template (HTML snippet or full file) and reports diagnostics:
    /// unknown attributes/functions, wrong arity, malformed d-for, unbalanced mustaches.
    /// </summary>
    public interface IDrapoValidatorService
    {
        Task<DrapoValidationResultVM> Validate(string html);
    }
}
