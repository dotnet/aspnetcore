#if NET45
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Razor;
using Microsoft.CSharp;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorCompilationService : ICompilationService
    {
        private static readonly string _tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        private readonly IFileSystem _tempFileSystem = new PhysicalFileSystem(Path.GetTempPath());
        private readonly ICompilationService _baseCompilationService;
        private readonly CompilerCache _cache = new CompilerCache();

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
            GeneratorResults results;
            using (TextReader rdr = new StreamReader(file.CreateReadStream()))
            {
                results = engine.GenerateCode(rdr, '_' + Path.GetFileNameWithoutExtension(file.Name), "Asp", file.PhysicalPath ?? file.Name);
            }

            string generatedCode;

            using (var writer = new StringWriter())
            using (var codeProvider = new CSharpCodeProvider())
            {
                codeProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, writer, new CodeGeneratorOptions());
                generatedCode = writer.ToString();
            }

            if (!results.Success) 
            {
                return CompilationResult.Failed(generatedCode, results.ParserErrors.Select(e => new CompilationMessage(e.Message)));
            }

            Directory.CreateDirectory(_tempPath);
            string tempFile = Path.Combine(_tempPath, Path.GetRandomFileName() + ".cs");

            File.WriteAllText(tempFile, generatedCode);

            _tempFileSystem.TryGetFileInfo(tempFile, out file);
            return await _baseCompilationService.Compile(file);
        }
    }
}
#endif