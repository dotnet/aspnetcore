// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Components.Analyzers;

public class ComponentRefAndRenderModeAnalyzerTest : AnalyzerTestBase
{
    public ComponentRefAndRenderModeAnalyzerTest()
    {
        Analyzer = new ComponentRefAndRenderModeAnalyzer();
        Runner = new ComponentAnalyzerDiagnosticAnalyzerRunner(Analyzer);
    }

    private ComponentRefAndRenderModeAnalyzer Analyzer { get; }
    private ComponentAnalyzerDiagnosticAnalyzerRunner Runner { get; }

    [Fact]
    public async Task ComponentWithBothRefAndRenderMode_ReportsDiagnostic()
    {
        // Arrange
        var source = Read("ComponentWithBothRefAndRenderMode");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Collection(
            diagnostics,
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.ComponentShouldNotUseRefAndRenderModeOnSameElement, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic.Location);
            });
    }

    [Fact]
    public async Task ComponentWithOnlyRef_DoesNotReportDiagnostic()
    {
        // Arrange
        var source = Read("ComponentWithOnlyRef");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ComponentWithOnlyRenderMode_DoesNotReportDiagnostic()
    {
        // Arrange
        var source = Read("ComponentWithOnlyRenderMode");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultipleComponentsWithMixedUsage_ReportsCorrectDiagnostics()
    {
        // Arrange
        var source = Read("MultipleComponentsWithMixedUsage");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Collection(
            diagnostics,
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.ComponentShouldNotUseRefAndRenderModeOnSameElement, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic.Location);
            });
    }

    [Fact]
    public async Task NonComponentMethod_DoesNotReportDiagnostic()
    {
        // Arrange
        var source = Read("NonComponentMethod");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }
}