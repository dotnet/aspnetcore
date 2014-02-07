using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IRazorCompilationService
    {
        Task<CompilationResult> Compile(string appRoot, IFileInfo fileInfo);
    }
}
