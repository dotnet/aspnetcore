using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Razor;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorCompilationService : IRazorCompilationService
    {
        private static readonly CompilerCache _cache = new CompilerCache();
        private readonly IApplicationEnvironment _environment;
        private readonly ICompilationService _baseCompilationService;
        private readonly IMvcRazorHost _razorHost;
        private readonly string _appRoot;

        public RazorCompilationService(IApplicationEnvironment environment,
                                       ICompilationService compilationService, 
                                       IMvcRazorHost razorHost)
        {
            _environment = environment;
            _baseCompilationService = compilationService;
            _razorHost = razorHost;
            _appRoot = EnsureTrailingSlash(environment.ApplicationBasePath);
        }

        public Task<CompilationResult> Compile([NotNull]IFileInfo file)
        {
            return _cache.GetOrAdd(file, () => CompileCore(file));
        }

        // TODO: Make this internal
        public async Task<CompilationResult> CompileCore(IFileInfo file)
        {
            GeneratorResults results;
            using (Stream inputStream = file.CreateReadStream())
            {
                Contract.Assert(file.PhysicalPath.StartsWith(_appRoot, StringComparison.OrdinalIgnoreCase));
                var rootRelativePath = file.PhysicalPath.Substring(_appRoot.Length);
                results = _razorHost.GenerateCode(_environment.ApplicationName, rootRelativePath, inputStream);
            }

            if (!results.Success)
            {
                var messages = results.ParserErrors.Select(e => new CompilationMessage(e.Message));
                throw new CompilationFailedException(messages, results.GeneratedCode);
            }

            return await _baseCompilationService.Compile(results.GeneratedCode);
        }

        private static string EnsureTrailingSlash([NotNull]string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar;
            }
            return path;
        }
    }
}
