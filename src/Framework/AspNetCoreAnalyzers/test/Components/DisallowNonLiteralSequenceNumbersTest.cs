// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

public class DisallowNonLiteralSequenceNumbersTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RenderTreeBuilderAnalyzer());

    [Fact]
    public async Task RenderTreeBuilderInvocationWithNumericLiteralArgument_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Components.Rendering;
var renderTreeBuilder = new RenderTreeBuilder();
renderTreeBuilder.OpenElement(0, ""div"");
renderTreeBuilder.CloseElement();
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task RenderTreeBuilderInvocationWithNonConstantArgument_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components.Rendering;
var renderTreeBuilder = new RenderTreeBuilder();
var i = 0;
renderTreeBuilder.OpenRegion(/*MM*/i);
renderTreeBuilder.CloseRegion();
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith("'i' should not be used as a sequence number.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RenderTreeBuilderInvocationWithConstantArgument_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components.Rendering;
var renderTreeBuilder = new RenderTreeBuilder();
const int i = 0;
renderTreeBuilder.OpenRegion(/*MM*/i);
renderTreeBuilder.CloseRegion();
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith("'i' should not be used as a sequence number.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task RenderTreeBuilderInvocationWithInvocationArgument_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components.Rendering;
var renderTreeBuilder = new RenderTreeBuilder();
renderTreeBuilder.OpenElement(/*MM*/ComputeSequenceNumber(0), ""div"");
renderTreeBuilder.CloseElement();
static int ComputeSequenceNumber(int i) => i + 1;
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.StartsWith("'ComputeSequenceNumber(0)' should not be used as a sequence number.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }
}
