using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class RoslynCompilationService : ICompilationService
    {
        private readonly IDependencyExporter _exporter;
        private readonly IApplicationEnvironment _environment;
        private readonly IAssemblyLoaderEngine _loader;

        public RoslynCompilationService(IApplicationEnvironment environment,
                                        IAssemblyLoaderEngine loaderEngine,
                                        IDependencyExporter exporter)
        {
            _environment = environment;
            _loader = loaderEngine;
            _exporter = exporter;
        }

        public Task<CompilationResult> Compile(string content)
        {
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(content) };
            var targetFramework = _environment.TargetFramework;

            var references = GetApplicationReferences().ToList();

            var assemblyName = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(assemblyName,
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                        syntaxTrees: syntaxTrees,
                        references: references);

            using (var ms = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    var result = compilation.Emit(ms, pdbStream: pdb);

                    if (!result.Success)
                    {
                        var formatter = new DiagnosticFormatter();

                        var messages = result.Diagnostics.Where(IsError).Select(d => GetCompilationMessage(formatter, d));

                        return Task.FromResult(CompilationResult.Failed(content, messages));
                    }

                    var type = _loader.LoadBytes(ms.ToArray(), pdb.ToArray())
                                       .GetExportedTypes()
                                       .First();

                    return Task.FromResult(CompilationResult.Successful(String.Empty, type));
                }
            }
        }

        private IEnumerable<MetadataReference> GetApplicationReferences()
        {
            var assemblyNames = new[] {
                _environment.ApplicationName,
                typeof(RoslynCompilationService).GetTypeInfo().Assembly.GetName().Name
            };

            return assemblyNames.Select(a => _exporter.GetDependencyExport(a, _environment.TargetFramework))
                                .SelectMany(e => e.MetadataReferences.SelectMany(ConvertMetadataReference));
        }

        private IEnumerable<MetadataReference> ConvertMetadataReference(IMetadataReference metadataReference)
        {
            var fileMetadataReference = metadataReference as IMetadataFileReference;

            if (fileMetadataReference != null)
            {
                string path = fileMetadataReference.Path;
#if NET45
                return new[] { new MetadataFileReference(path) };
#else
                // TODO: What about access to the file system? We need to be able to 
                // read files from anywhere on disk, not just under the web root
                using (var stream = File.OpenRead(path))
                {
                    return new[] { new MetadataImageReference(stream) };
                }
#endif
            }

            var roslynReference = metadataReference as IRoslynMetadataReference;

            if (roslynReference != null)
            {
                // REVIEW: We should really only compile against the app's closure
                var compilatonReference = roslynReference.MetadataReference as CompilationReference;
                if (compilatonReference != null)
                {
                    return new[] { compilatonReference }.Concat(compilatonReference.Compilation.References);
                }

                return new[] { roslynReference.MetadataReference };
            }

            throw new NotSupportedException();
        }

        private CompilationMessage GetCompilationMessage(DiagnosticFormatter formatter, Diagnostic diagnostic)
        {
            return new CompilationMessage(formatter.Format(diagnostic));
        }

        private bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }
    }
}
