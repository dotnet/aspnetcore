// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Dnx.Runtime.Infrastructure;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    public static class CompilationUtility
    {
        private static readonly ConcurrentDictionary<string, AssemblyMetadata> _metadataCache =
            new ConcurrentDictionary<string, AssemblyMetadata>(StringComparer.Ordinal);
        private static readonly Assembly ExecutingAssembly = typeof(CompilationUtility).GetTypeInfo().Assembly;
        public static readonly string GeneratedAssemblyName = Path.GetRandomFileName() + "." + Path.GetRandomFileName();

        public static Compilation GetCompilation(params string[] resourceFiles)
        {
            var assemblyVersion = ExecutingAssembly.GetName().Version;

            var syntaxTrees = new List<SyntaxTree>
            {
                CSharpSyntaxTree.ParseText(
                    $"[assembly: {typeof(AssemblyVersionAttribute).FullName}(\"{assemblyVersion}\")]")
            };

            foreach (var resourceFile in resourceFiles)
            {
                var resourceContent = ReadManifestResource(resourceFile);
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(resourceContent));
            }

            var libraryExporter = (ILibraryExporter)CallContextServiceLocator
                .Locator
                .ServiceProvider
                .GetService(typeof(ILibraryExporter));
            var applicationName = ExecutingAssembly.GetName().Name;
            var libraryExport = libraryExporter.GetExport(applicationName);

            var references = new List<MetadataReference>();
            var roslynReference = libraryExport.MetadataReferences[0] as IRoslynMetadataReference;
            var compilationReference = roslynReference?.MetadataReference as CompilationReference;
            if (compilationReference != null)
            {
                references.AddRange(compilationReference.Compilation.References);
                references.Add(roslynReference.MetadataReference);
            }
            else
            {
                var export = libraryExporter.GetAllExports(applicationName);
                foreach (var metadataReference in export.MetadataReferences)
                {
                    var reference = metadataReference.ConvertMetadataReference(
                        fileReference => _metadataCache.GetOrAdd(
                            fileReference.Path,
                            _ => fileReference.CreateAssemblyMetadata()));
                    references.Add(reference);
                }
            }

            return CSharpCompilation.Create(
                GeneratedAssemblyName,
                syntaxTrees,
                references);
        }

        private static string ReadManifestResource(string path)
        {
            path = $"{ExecutingAssembly.GetName().Name}.{path}.cs";
            using (var contentStream = ExecutingAssembly.GetManifestResourceStream(path))
            {
                using (var reader = new StreamReader(contentStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
