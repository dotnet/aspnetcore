using System;
using System.Threading.Tasks;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class VirtualPathViewFactory : IVirtualPathViewFactory
    {
        private readonly IVirtualFileSystem _fileSystem;
        private readonly IRazorCompilationService _compilationService;

        public VirtualPathViewFactory(IVirtualFileSystem fileSystem, IRazorCompilationService compilationService)
        {
            _fileSystem = fileSystem;
            _compilationService = compilationService;
        }

        public async Task<IView> CreateInstance(string virtualPath)
        {
            IFileInfo fileInfo;
            if (_fileSystem.TryGetFileInfo(virtualPath, out fileInfo))
            {
                CompilationResult result = await _compilationService.Compile(fileInfo);
                return (IView)Activator.CreateInstance(result.CompiledType);
            }

            return null;
        }
    }
}
