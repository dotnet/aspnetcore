// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class TestCompilation
{
    public static Compilation Create(string source)
    {
        return CSharpCompilation.Create("Test",
            new[] { CSharpSyntaxTree.ParseText(source) },
            TestReferences.MetadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
