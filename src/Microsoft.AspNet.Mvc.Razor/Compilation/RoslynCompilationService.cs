using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Net.Runtime.Services;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class RoslynCompilationService : ICompilationService
    {
        private readonly IMetadataReferenceProvider _provider;
        private readonly IApplicationEnvironment _environment;

        public RoslynCompilationService(IServiceProvider serviceProvider)
        {
            // TODO: Get these services via ctor injection when we get container chaining implemented
            _provider = (IMetadataReferenceProvider)serviceProvider.GetService(typeof(IMetadataReferenceProvider));
            _environment = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));
        }

        public Task<CompilationResult> Compile(string content)
        {
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(content) };
            var targetFramework = _environment.TargetFramework;

            // Get references from the application itself and from
            // this assembly for the base types etc
            var referenceNames = new[] {
                _environment.ApplicationName,
                typeof(RoslynCompilationService).GetTypeInfo().Assembly.GetName().Name
            };

            var references = referenceNames.SelectMany(name => _provider.GetReferences(name, targetFramework))
                                           .Cast<MetadataReference>();

            var assemblyName = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(assemblyName,
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                        syntaxTrees: syntaxTrees,
                        references: references);

            var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var messages = result.Diagnostics.Where(IsError).Select(d => GetCompilationMessage(d));

                return Task.FromResult(CompilationResult.Failed(content, messages));
            }

            // TODO: Flow loader to this code so we're not using Load() directly
            var type = Assembly.Load(ms.ToArray())
                               .GetExportedTypes()
                               .First();

            return Task.FromResult(CompilationResult.Successful(String.Empty, type));
        }

        private CompilationMessage GetCompilationMessage(Diagnostic diagnostic)
        {
#if NET45
            var formatter = DiagnosticFormatter.Instance;
#else
            var formatter = new DiagnosticFormatter();
#endif
            return new CompilationMessage(formatter.Format(diagnostic));
        }

        private bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }
    }
}
