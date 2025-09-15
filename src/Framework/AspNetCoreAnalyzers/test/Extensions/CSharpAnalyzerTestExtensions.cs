// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Analyzers.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Analyzers;

public static class CSharpAnalyzerTestExtensions
{
    extension<TAnalyzer, TVerifier>(CSharpAnalyzerTest<TAnalyzer, TVerifier>)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
        public static CSharpAnalyzerTest<TAnalyzer, TVerifier> Create([StringSyntax("C#-test")] string source, params ReadOnlySpan<DiagnosticResult> expectedDiagnostics)
        {
            var test = new CSharpAnalyzerTest<TAnalyzer, TVerifier>
            {
                TestCode = source.ReplaceLineEndings(),
                // We need to set the output type to an exe to properly
                // support top-level programs in the tests. Otherwise,
                // the test infra will assume we are trying to build a library.
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                ReferenceAssemblies = CSharpAnalyzerVerifier<TAnalyzer>.GetReferenceAssemblies(),
            };

            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);
            return test;
        }
    }

    public static CSharpAnalyzerTest<TAnalyzer, TVerifier> WithSource<TAnalyzer, TVerifier>(this CSharpAnalyzerTest<TAnalyzer, TVerifier> test, [StringSyntax("C#-test")] string source)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TVerifier : IVerifier, new()
    {
        test.TestState.Sources.Add(source);
        return test;
    }
}
