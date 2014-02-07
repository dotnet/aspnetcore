using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Razor;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorCompilationService : IRazorCompilationService
    {
        private static readonly CompilerCache _cache = new CompilerCache();
        private readonly ICompilationService _baseCompilationService;
        private readonly IMvcRazorHost _razorHost;

        public RazorCompilationService(ICompilationService compilationService, IMvcRazorHost razorHost)
        {
            _baseCompilationService = compilationService;
            _razorHost = razorHost;
        }

        public Task<CompilationResult> Compile(string appRoot, IFileInfo file)
        {
            return _cache.GetOrAdd(file, () => CompileCore(appRoot, file));
        }

        private async Task<CompilationResult> CompileCore(string appRoot, IFileInfo file)
        {
            GeneratorResults results;
            using (Stream inputStream = file.CreateReadStream())
            {
                Contract.Assert(file.PhysicalPath.StartsWith(appRoot, StringComparison.OrdinalIgnoreCase));
                // Remove the app name segment so that it appears as part of the root relative path: 
                // work/src/myapp/ -> work/src
                // root relative path: myapp/views/home/index.cshtml
                // TODO: The root namespace might be a property we'd have to read via configuration since it 
                // affects other things such as resx files.
                appRoot = Path.GetDirectoryName(appRoot.TrimEnd(Path.DirectorySeparatorChar));
                string rootRelativePath = file.PhysicalPath.Substring(appRoot.Length).TrimStart(Path.DirectorySeparatorChar);
                results = _razorHost.GenerateCode(rootRelativePath, inputStream);
            }

            if (!results.Success)
            {
                return CompilationResult.Failed(results.GeneratedCode, results.ParserErrors.Select(e => new CompilationMessage(e.Message)));
            }

            return await _baseCompilationService.Compile(results.GeneratedCode);
        }
    }
}
