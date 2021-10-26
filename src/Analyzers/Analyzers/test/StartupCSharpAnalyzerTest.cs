// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class StartupCSharpAnalyzerTest : CSharpAnalyzerTest<StartupAnalyzer, XUnitVerifier>
{
    public StartupCSharpAnalyzerTest(StartupAnalyzer analyzer, ImmutableArray<MetadataReference> metadataReferences)
    {
        StartupAnalyzer = analyzer;
        SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);

            return project
                .WithCompilationOptions(((CSharpCompilationOptions)project.CompilationOptions)
                    .WithOutputKind(OutputKind.WindowsApplication))
             .WithMetadataReferences(metadataReferences)
             .Solution;
        });
    }

    public StartupAnalyzer StartupAnalyzer { get; }

    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers() => new[] { StartupAnalyzer };
}
