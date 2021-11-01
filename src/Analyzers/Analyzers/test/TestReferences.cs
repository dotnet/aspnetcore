// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Microsoft.AspNetCore.Analyzers;

public static class TestReferences
{
    public static readonly ReferenceAssemblies EmptyReferenceAssemblies = new("some-tfm");

    public static ImmutableArray<MetadataReference> MetadataReferences { get; } = GetMetadataReferences();

    private static ImmutableArray<MetadataReference> GetMetadataReferences()
    {
        var seen = new HashSet<string>();

        var references = ImmutableArray.CreateBuilder<MetadataReference>();

        foreach (var defaultCompileLibrary in DependencyContext.Load(typeof(TestReferences).Assembly).CompileLibraries)
        {
            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(new AppBaseCompilationAssemblyResolver()))
            {
                TryAdd(resolveReferencePath);
            }
        }

        // The deps file in the project is incorrect and does not contain "compile" nodes for some references.
        // However these binaries are always present in the bin output. As a "temporary" workaround, we'll add
        // every dll file that's present in the test's build output as a metadatareference.
        foreach (var assembly in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
        {
            TryAdd(assembly);
        }

        return references.ToImmutable();

        void TryAdd(string assemblyPath)
        {
            var name = Path.GetFileNameWithoutExtension(assemblyPath);

            if (!name.StartsWith("Microsoft.Extensions", StringComparison.Ordinal) &&
                !name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) &&
                !name.StartsWith("System", StringComparison.Ordinal) &&
                !name.StartsWith("netstandard", StringComparison.Ordinal))
            {
                return;
            }

            if (seen.Add(name))
            {
                references.Add(MetadataReference.CreateFromFile(assemblyPath));
            }
        }
    }
}
