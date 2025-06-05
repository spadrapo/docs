using System.Collections.Generic;
using System.Threading.Tasks;
using WebDocs.Models;

namespace WebDocs.Services
{
    public interface IFunctionService
    {
        Task<string> GetContent(string name);
        Task<List<string>> GetNames();
        Task<List<FunctionVM>> GetList();
        Task<FunctionVM> Get(string name);
    }
}
