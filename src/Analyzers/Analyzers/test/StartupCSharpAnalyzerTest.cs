// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class StartupCSharpAnalyzerTest : CSharpAnalyzerTest<StartupAnalyzer, DefaultVerifier>
{
    public StartupCSharpAnalyzerTest(StartupAnalyzer analyzer, ImmutableArray<MetadataReference> metadataReferences)
    {
        StartupAnalyzer = analyzer;
        TestState.OutputKind = OutputKind.WindowsApplication;
        TestState.AdditionalReferences.AddRange(metadataReferences);
    }

    public StartupAnalyzer StartupAnalyzer { get; }

    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { StartupAnalyzer };
}
