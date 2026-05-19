// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Analyzers;

public class ComponentInternalUsageDiagnosticsAnalyzerTest : AnalyzerTestBase
{
    public ComponentInternalUsageDiagnosticsAnalyzerTest()
    {
        Analyzer = new ComponentInternalUsageDiagnosticAnalyzer();
        Runner = new ComponentAnalyzerDiagnosticAnalyzerRunner(Analyzer);
    }

    private ComponentInternalUsageDiagnosticAnalyzer Analyzer { get; }
    private ComponentAnalyzerDiagnosticAnalyzerRunner Runner { get; }

    [Fact]
    public async Task InternalUsage_FindsUseOfInternalTypesInDeclarations()
    {
        // Arrange
        var source = Read("UsesRendererTypesInDeclarations");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Collection(
            diagnostics,
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMBaseClass"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMField"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMInvocation"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMProperty"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMParameter"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMReturnType"], diagnostic.Location);
            });
    }

    [Fact]
    public async Task InternalUsage_FindsUseOfInternalTypesInMethodBody()
    {
        // Arrange
        var source = Read("UsersRendererTypesInMethodBody");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Collection(
            diagnostics,
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMField"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMNewObject"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMProperty"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMNewObject2"], diagnostic.Location);
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DoNotUseRenderTreeTypes, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MMInvocation"], diagnostic.Location);
            });
    }
}
