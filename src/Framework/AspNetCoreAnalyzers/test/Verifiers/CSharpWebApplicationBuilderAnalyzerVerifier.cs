// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public static class CSharpWebApplicationBuilderAnalyzerVerifier
{
    public static DiagnosticResult Diagnostic(string diagnosticId = null)
        => CSharpWebApplicationBuilderAnalyzerVerifier.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new DiagnosticResult(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();

    }

    public class Test : CSharpCodeFixTest<WebApplicationBuilderAnalyzer, EmptyCodeFixProvider, XUnitVerifier> { }
}
