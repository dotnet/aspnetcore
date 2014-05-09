// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class RoslynCompilationService : ICompilationService
    {
        private static readonly ConcurrentDictionary<string, MetadataReference> _metadataFileCache = new ConcurrentDictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);
        
        private readonly ILibraryManager _libraryManager;
        private readonly IApplicationEnvironment _environment;
        private readonly IAssemblyLoaderEngine _loader;

        public RoslynCompilationService(IApplicationEnvironment environment,
                                        IAssemblyLoaderEngine loaderEngine,
                                        ILibraryManager libraryManager)
        {
            _environment = environment;
            _loader = loaderEngine;
            _libraryManager = libraryManager;
        }

        public CompilationResult Compile(string content)
        {
            var syntaxTrees = new[] { CSharpSyntaxTree.ParseText(content) };
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
                    EmitResult result = null;

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

                        var messages = result.Diagnostics.Where(IsError).Select(d => GetCompilationMessage(formatter, d)).ToList();

                        return CompilationResult.Failed(content, messages);
                    }

                    Assembly assembly = null;

                    if (PlatformHelper.IsMono)
                    {
                       assembly = _loader.LoadBytes(ms.ToArray(), pdbBytes: null);
                    }
                    else
                    {
                        assembly = _loader.LoadBytes(ms.ToArray(), pdb.ToArray());
                    }

                    var type = assembly.GetExportedTypes()
                                       .First();

                    return CompilationResult.Successful(String.Empty, type);
                }
            }
        }

        private List<MetadataReference> GetApplicationReferences()
        {
            var references = new List<MetadataReference>();

            var export = _libraryManager.GetLibraryExport(_environment.ApplicationName);

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
