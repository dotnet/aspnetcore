using System;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class DefaultCompilationService : ICompilationService
    {
        public Task<CompilationResult> Compile(IFileInfo fileInfo)
        {
            return null;
        }
    }
}
