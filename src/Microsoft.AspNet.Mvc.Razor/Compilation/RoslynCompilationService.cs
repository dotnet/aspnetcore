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

            var references = GetApplicationReferences();

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

                        var messages = result.Diagnostics.Where(IsError).Select(d => GetCompilationMessage(formatter, d)).ToList();

                        return Task.FromResult(CompilationResult.Failed(content, messages));
                    }

                    var type = _loader.LoadBytes(ms.ToArray(), pdb.ToArray())
                                       .GetExportedTypes()
                                       .First();

                    return Task.FromResult(CompilationResult.Successful(String.Empty, type));
                }
            }
        }

        private List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();

            // TODO: We need a way to get the current application's dependencies

            var assemblies = new[] {
                _environment.ApplicationName,
#if NET45
                "mscorlib",
                "System",
                "System.Core",
                "Microsoft.CSharp",
#else
                "System.Linq",
                "System.Collections",
                "System.Dynamic.Runtime",
                "System.Collections.Generic",
#endif
                "Microsoft.AspNet.Mvc",
                "Microsoft.AspNet.Mvc.Razor",
                "Microsoft.AspNet.Mvc.Rendering",
            };

            var exports = new List<IDependencyExport>();

            foreach (var assemblyName in assemblies)
            {
                var export = _exporter.GetDependencyExport(assemblyName, _environment.TargetFramework);

                if (export == null)
                {
                    continue;
                }

                exports.Add(export);
            }

            ExtractReferences(exports, references);

            return references;
        }

        private void ExtractReferences(List<IDependencyExport> exports, List<MetadataReference> references)
        {
            var paths = new HashSet<string>();

            foreach (var export in exports)
            {
                foreach (var metadataReference in export.MetadataReferences)
                {
                    var fileMetadataReference = metadataReference as IMetadataFileReference;

                    if (fileMetadataReference != null)
                    {
                        string path = fileMetadataReference.Path;

                        paths.Add(path);
                    }
                    else
                    {
                        var roslynReference = metadataReference as IRoslynMetadataReference;

                        if (roslynReference != null)
                        {
                            references.Add(roslynReference.MetadataReference);
                        }
                    }
                }
            }

            references.AddRange(paths.Select(CreateMetadataFileReference));
        }

        private MetadataReference CreateMetadataFileReference(string path)
        {
#if NET45
            return new MetadataFileReference(path);
#else
            // TODO: What about access to the file system? We need to be able to 
            // read files from anywhere on disk, not just under the web root
            using (var stream = File.OpenRead(path))
            {
                return new MetadataImageReference(stream);
            }
#endif
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
