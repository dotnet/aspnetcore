// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.CodeAnalysis
{
    public static class TestCompilation
    {
        private static readonly ConcurrentDictionary<Assembly, IEnumerable<MetadataReference>> _referenceCache =
            new ConcurrentDictionary<Assembly, IEnumerable<MetadataReference>>();

        public static IEnumerable<MetadataReference> GetMetadataReferences(Assembly assembly)
        {
            var dependencyContext = DependencyContext.Load(assembly);

            var metadataReferences = dependencyContext.CompileLibraries
                .SelectMany(l => l.ResolveReferencePaths())
                .Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath))
                .ToArray();

            return metadataReferences;
        }

        public static string AssemblyName => "TestAssembly";

        public static CSharpCompilation Create(Assembly assembly, SyntaxTree syntaxTree = null)
        {
            IEnumerable<SyntaxTree> syntaxTrees = null;

            if (syntaxTree != null)
            {
                syntaxTrees = new[] { syntaxTree };
            }

            if (!_referenceCache.TryGetValue(assembly, out IEnumerable<MetadataReference> metadataReferences))
            {
                metadataReferences = GetMetadataReferences(assembly);
                _referenceCache.TryAdd(assembly, metadataReferences);
            }

            var compilation = CSharpCompilation.Create(AssemblyName, syntaxTrees, metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            EnsureValidCompilation(compilation);

            return compilation;
        }

        private static void EnsureValidCompilation(CSharpCompilation compilation)
        {
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .Emit(stream);
                var diagnostics = string.Join(
                    Environment.NewLine,
                    emitResult.Diagnostics.Select(d => CSharpDiagnosticFormatter.Instance.Format(d)));
                Assert.True(emitResult.Success, $"Compilation is invalid : {Environment.NewLine}{diagnostics}");
            }
        }
    }
}
