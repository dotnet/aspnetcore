
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.CoreServices
{
    public interface ICompilationService
    {
        Task<CompilationResult> Compile(IFileInfo fileInfo);
    }
}
