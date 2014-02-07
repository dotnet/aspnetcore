using System;
using System.IO;
using System.Linq;
using System.Text;
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

        public Task<CompilationResult> Compile(IFileInfo file)
        {
            return _cache.GetOrAdd(file, () => CompileCore(file));
        }

        private async Task<CompilationResult> CompileCore(IFileInfo file)
        {
            GeneratorResults results;
            using (Stream inputStream = file.CreateReadStream())
            {
                results = _razorHost.GenerateCode(file.PhysicalPath, inputStream);
            }

            if (!results.Success)
            {
                return CompilationResult.Failed(results.GeneratedCode, results.ParserErrors.Select(e => new CompilationMessage(e.Message)));
            }

            return await _baseCompilationService.Compile(results.GeneratedCode);
        }
    }
}
