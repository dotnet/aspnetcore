// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public static class TestCompilation
    {
        public static Compilation Create(SyntaxTree syntaxTree = null)
        {
            IEnumerable<SyntaxTree> syntaxTrees = null;

            if (syntaxTree != null)
            {
                syntaxTrees = new[] { syntaxTree };
            }

            var currentAssembly = typeof(TestCompilation).GetTypeInfo().Assembly;
            var dependencyContext = DependencyContext.Load(currentAssembly);

            var references = dependencyContext.CompileLibraries.SelectMany(l => l.ResolveReferencePaths())
                .Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath));
            var compilation = CSharpCompilation.Create("TestAssembly", syntaxTrees, references);

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
