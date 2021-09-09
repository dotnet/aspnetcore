// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

public static class CSharpDelegateEndpointsAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DelegateEndpointAnalyzer, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId = null)
        => CSharpDelegateEndpointsAnalyzerVerifier<DelegateEndpointAnalyzer>.Diagnostic(diagnosticId);

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => new DiagnosticResult(descriptor);

    public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test { TestCode = source };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    public class Test : CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, XUnitVerifier> { }
}