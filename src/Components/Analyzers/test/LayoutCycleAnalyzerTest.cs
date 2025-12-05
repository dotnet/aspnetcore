// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Components.Analyzers;

public class LayoutCycleAnalyzerTest : AnalyzerTestBase
{
    public LayoutCycleAnalyzerTest()
    {
        Analyzer = new LayoutCycleAnalyzer();
        Runner = new ComponentAnalyzerDiagnosticAnalyzerRunner(Analyzer);
    }

    private LayoutCycleAnalyzer Analyzer { get; }
    private ComponentAnalyzerDiagnosticAnalyzerRunner Runner { get; }

    [Fact]
    public async Task LayoutComponentReferencesSelf_ReportsDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components;

[Layout(typeof(MyLayout))]
public class /*MM*/MyLayout : LayoutComponentBase
{
}");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.LayoutComponentCannotReferenceItself, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
    }

    [Fact]
    public async Task LayoutComponentDoesNotReferenceSelf_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Components;

public class MyLayout : LayoutComponentBase
{
}";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LayoutComponentReferencesOtherLayout_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Components;

public class MainLayout : LayoutComponentBase
{
}

[Layout(typeof(MainLayout))]
public class MyLayout : LayoutComponentBase
{
}";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonLayoutComponent_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Components;

[Layout(typeof(MyComponent))]
public class MyComponent : ComponentBase
{
}";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }
}