using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IRazorCompilationService
    {
        Task<CompilationResult> Compile(IFileInfo fileInfo);
    }
}
