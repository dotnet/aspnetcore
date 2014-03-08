using System;
using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class VirtualPathViewFactory : IVirtualPathViewFactory
    {
        private readonly PhysicalFileSystem _fileSystem;
        private readonly IRazorCompilationService _compilationService;

        public VirtualPathViewFactory(IApplicationEnvironment env, IRazorCompilationService compilationService)
        {
            // TODO: Continue to inject the IFileSystem but only when we get it from the host
            _fileSystem = new PhysicalFileSystem(env.ApplicationBasePath);
            _compilationService = compilationService;
        }

        public async Task<IView> CreateInstance(string virtualPath)
        {
            // TODO: We need to glean the approot from HttpContext
            var appRoot = _fileSystem.Root;

            IFileInfo fileInfo;
            if (_fileSystem.TryGetFileInfo(virtualPath, out fileInfo))
            {
                CompilationResult result = await _compilationService.Compile(appRoot, fileInfo);
                return (IView)Activator.CreateInstance(result.CompiledType);
            }

            return null;
        }
    }
}
