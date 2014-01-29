
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface ICompilationService
    {
        Task<CompilationResult> Compile(string content);
    }
}
