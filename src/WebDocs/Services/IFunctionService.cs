using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebDocs.Services
{
    public interface IFunctionService
    {
        Task<string> GetContent(string name);
        Task<List<string>> GetNames();
    }
}
