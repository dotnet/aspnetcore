using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorCompilationService : IRazorCompilationService
    {
        private static readonly CompilerCache _cache = new CompilerCache();
        private readonly ICompilationService _baseCompilationService;

        public RazorCompilationService(ICompilationService compilationService)
        {
            _baseCompilationService = compilationService;
        }

        public Task<CompilationResult> Compile(IFileInfo file)
        {
            return _cache.GetOrAdd(file, () => CompileCore(file));
        }

        private async Task<CompilationResult> CompileCore(IFileInfo file)
        {
            var host = new MvcRazorHost();
            var engine = new RazorTemplateEngine(host);

            var namespaceBuilder = GenerateNamespace(file);
            
            GeneratorResults results;
            using (TextReader rdr = new StreamReader(file.CreateReadStream()))
            {
                results = engine.GenerateCode(rdr, '_' + file.Name, namespaceBuilder.ToString(), file.PhysicalPath ?? file.Name);
            }

            if (!results.Success)
            {
                return CompilationResult.Failed(results.GeneratedCode, results.ParserErrors.Select(e => new CompilationMessage(e.Message)));
            }

            return await _baseCompilationService.Compile(results.GeneratedCode);
        }

        private static StringBuilder GenerateNamespace(IFileInfo file)
        {
            string virtualPath = file.PhysicalPath;
            if (virtualPath.StartsWith("~/", StringComparison.Ordinal))
            {
                virtualPath = virtualPath.Substring(2);
            }

            var namespaceBuilder = new StringBuilder(virtualPath.Length);

            foreach (char c in Path.GetDirectoryName(virtualPath))
            {
                if (c == Path.DirectorySeparatorChar)
                {
                    namespaceBuilder.Append('.');
                }
                else if (!Char.IsLetterOrDigit(c))
                {
                    namespaceBuilder.Append('_');
                }
                else
                {
                    namespaceBuilder.Append(c);
                }
            }
            return namespaceBuilder;
        }
    }
}
