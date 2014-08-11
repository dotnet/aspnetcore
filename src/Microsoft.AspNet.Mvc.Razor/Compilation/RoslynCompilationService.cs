// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNet.FileSystems;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// A type that uses Roslyn to compile C# content.
    /// </summary>
    public class RoslynCompilationService : ICompilationService
    {
        private readonly ConcurrentDictionary<string, MetadataReference> _metadataFileCache =
            new ConcurrentDictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

        private readonly ILibraryManager _libraryManager;
        private readonly IApplicationEnvironment _environment;
        private readonly IAssemblyLoaderEngine _loader;

        /// <summary>
        /// Initalizes a new instance of the <see cref="RoslynCompilationService"/> class.
        /// </summary>
        /// <param name="environment">The environment for the executing application.</param>
        /// <param name="loaderEngine">The loader used to load compiled assemblies.</param>
        /// <param name="libraryManager">The library manager that provides export and reference information.</param>
        public RoslynCompilationService(IApplicationEnvironment environment,
                                        IAssemblyLoaderEngine loaderEngine,
                                        ILibraryManager libraryManager)
        {
            _environment = environment;
            _loader = loaderEngine;
            _libraryManager = libraryManager;
        }

        /// <inheritdoc />
        public CompilationResult Compile(IFileInfo fileInfo, string compilationContent)
        {
            var sourceText = SourceText.From(compilationContent, Encoding.UTF8);
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(sourceText, path: fileInfo.PhysicalPath) };
            var targetFramework = _environment.TargetFramework;

            var references = GetApplicationReferences();

            var assemblyName = Path.GetRandomFileName();

            var compilation = CSharpCompilation.Create(assemblyName,
                        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                        syntaxTrees: syntaxTrees,
                        references: references);

            using (var ms = new MemoryStream())
            {
                using (var pdb = new MemoryStream())
                {
                    EmitResult result;

                    if (PlatformHelper.IsMono)
                    {
                        result = compilation.Emit(ms, pdbStream: null);
                    }
                    else
                    {
                        result = compilation.Emit(ms, pdbStream: pdb);
                    }

                    if (!result.Success)
                    {
                        var formatter = new DiagnosticFormatter();

                        var messages = result.Diagnostics
                                             .Where(IsError)
                                             .Select(d => GetCompilationMessage(formatter, d))
                                             .ToList();

                        return CompilationResult.Failed(fileInfo, compilationContent, messages);
                    }

                    Assembly assembly;
                    ms.Seek(0, SeekOrigin.Begin);

                    if (PlatformHelper.IsMono)
                    {
                        assembly = _loader.LoadStream(ms, pdbStream: null);
                    }
                    else
                    {
                        pdb.Seek(0, SeekOrigin.Begin);
                        assembly = _loader.LoadStream(ms, pdb);
                    }

                    var type = assembly.GetExportedTypes()
                                       .First();

                    return CompilationResult.Successful(type);
                }
            }
        }

        private List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();

            var export = _libraryManager.GetAllExports(_environment.ApplicationName);

            foreach (var metadataReference in export.MetadataReferences)
            {
                var fileMetadataReference = metadataReference as IMetadataFileReference;

                if (fileMetadataReference != null)
                {
                    references.Add(CreateMetadataFileReference(fileMetadataReference.Path));
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

            return references;
        }

        private MetadataReference CreateMetadataFileReference(string path)
        {
            return _metadataFileCache.GetOrAdd(path, _ =>
            {
                // TODO: What about access to the file system? We need to be able to 
                // read files from anywhere on disk, not just under the web root
                using (var stream = File.OpenRead(path))
                {
                    return new MetadataImageReference(stream);
                }
            });
        }

        private static CompilationMessage GetCompilationMessage(DiagnosticFormatter formatter, Diagnostic diagnostic)
        {
            return new CompilationMessage(formatter.Format(diagnostic));
        }

        private static bool IsError(Diagnostic diagnostic)
        {
            return diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error;
        }
    }
}
