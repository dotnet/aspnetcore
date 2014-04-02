
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface ICompilationService
    {
        CompilationResult Compile(string content);
    }
}
