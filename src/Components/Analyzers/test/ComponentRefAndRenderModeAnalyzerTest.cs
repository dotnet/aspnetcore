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
        var source = Read("ComponentWithBothRefAndRenderMode");

        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

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
        var source = Read("ComponentWithOnlyRef");

        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ComponentWithOnlyRenderMode_DoesNotReportDiagnostic()
    {
        var source = Read("ComponentWithOnlyRenderMode");

        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultipleComponentsWithMixedUsage_ReportsCorrectDiagnostics()
    {
        var source = Read("MultipleComponentsWithMixedUsage");

        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

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
        var source = Read("NonComponentMethod");

        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NestedComponentsWithParentUsingBothRefAndRenderMode_ReportsDiagnostic()
    {
        var source = Read("NestedComponentsWithParentUsingBothRefAndRenderMode");

        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        Assert.Collection(
            diagnostics,
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.ComponentShouldNotUseRefAndRenderModeOnSameElement, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic.Location);
            });
    }
}