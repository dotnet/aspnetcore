// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

public class ApiActionsDoNotRequireExplicitModelValidationCheckCodeFixProviderTest
{
    private MvcDiagnosticAnalyzerRunner AnalyzerRunner { get; } = new MvcDiagnosticAnalyzerRunner(new ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzer());

    private CodeFixRunner CodeFixRunner { get; } = new IgnoreCS1701WarningCodeFixRunner();

    [Fact]
    public Task CodeFixRemovesModelStateIsInvalidBlockWithIfNotCheck()
        => RunTest();

    [Fact]
    public Task CodeFixRemovesModelStateIsInvalidBlockWithEqualityCheck()
        => RunTest();

    [Fact]
    public Task CodeFixRemovesIfBlockWithoutBraces()
        => RunTest();

    private async Task RunTest([CallerMemberName] string testMethod = "")
    {
        // Arrange
        var project = GetProject(testMethod);
        var controllerDocument = project.DocumentIds[0];
        var expectedOutput = Read(testMethod + ".Output");

        // Act
        var diagnostics = await AnalyzerRunner.GetDiagnosticsAsync(project);
        Assert.NotEmpty(diagnostics);
        var actualOutput = await CodeFixRunner.ApplyCodeFixAsync(
            new ApiActionsDoNotRequireExplicitModelValidationCheckCodeFixProvider(),
            project.GetDocument(controllerDocument),
            diagnostics[0]);

        Assert.Equal(expectedOutput, actualOutput, ignoreLineEndingDifferences: true);
    }

    private Project GetProject(string testMethod)
    {
        var testSource = Read(testMethod + ".Input");
        return MvcDiagnosticAnalyzerRunner.CreateProjectWithReferencesInBinDir(GetType().Assembly, new[] { testSource });
    }

    private string Read(string fileName)
    {
        return MvcTestSource.Read(GetType().Name, fileName)
            .Source
            .Replace("_INPUT_", "_TEST_")
            .Replace("_OUTPUT_", "_TEST_");
    }
}
