// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.AspNetCore.Blazor.Razor.Extensions
{
    public abstract class BaseTagHelperDescriptorProviderTest
    {
        static BaseTagHelperDescriptorProviderTest()
        {
            var dependencyContext = DependencyContext.Load(typeof(ComponentTagHelperDescriptorProviderTest).Assembly);

            var metadataReferences = dependencyContext.CompileLibraries
                .SelectMany(l => l.ResolveReferencePaths())
                .Select(assemblyPath => MetadataReference.CreateFromFile(assemblyPath))
                .ToArray();

            BaseCompilation = CSharpCompilation.Create(
                "TestAssembly",
                Array.Empty<SyntaxTree>(),
                metadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        protected static Compilation BaseCompilation { get; }

        // For simplicity in testing, exclude the built-in components. We'll add more and we
        // don't want to update the tests when that happens.
        protected static TagHelperDescriptor[] ExcludeBuiltInComponents(TagHelperDescriptorProviderContext context)
        {
            return context.Results
                .Where(c => c.AssemblyName == "TestAssembly")
                .OrderBy(c => c.Name)
                .ToArray();
        }

    }
}
