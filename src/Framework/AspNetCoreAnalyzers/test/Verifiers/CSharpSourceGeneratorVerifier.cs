// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Verifiers;

public static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
{
    public static async Task VerifyAsync(string source, string generatedFileName, string generatedSource)
    {
        var test = new CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source.ReplaceLineEndings() },
                OutputKind = OutputKind.ConsoleApplication,
                GeneratedSources =
                {
                    (typeof(TSourceGenerator), generatedFileName, SourceText.From(generatedSource, Encoding.UTF8))
                },
                ReferenceAssemblies = CSharpAnalyzerVerifier<WebApplicationBuilderAnalyzer>.GetReferenceAssemblies()
            },
        };
        await test.RunAsync(CancellationToken.None);
    }

    public static async Task VerifyAsync(string source)
    {
        var test = new CSharpSourceGeneratorTest<TSourceGenerator, DefaultVerifier>
        {
            TestState =
            {
                Sources = { source.ReplaceLineEndings() },
                OutputKind = OutputKind.ConsoleApplication,
                ReferenceAssemblies = CSharpAnalyzerVerifier<WebApplicationBuilderAnalyzer>.GetReferenceAssemblies()
            },
        };
        await test.RunAsync(CancellationToken.None);
    }
}
