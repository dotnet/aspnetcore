using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface IRazorCompilationService
    {
        CompilationResult Compile(IFileInfo fileInfo);
    }
}
