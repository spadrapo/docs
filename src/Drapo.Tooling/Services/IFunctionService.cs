using System.Collections.Generic;
using System.Threading.Tasks;
using Drapo.Tooling.Models;

namespace Drapo.Tooling.Services
{
    public interface IFunctionService
    {
        Task<string> GetContent(string name);
        Task<List<string>> GetNames();
        Task<List<FunctionVM>> GetList();
        Task<FunctionVM> Get(string name);
    }
}
