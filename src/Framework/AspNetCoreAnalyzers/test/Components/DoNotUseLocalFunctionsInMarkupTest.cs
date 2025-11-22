// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

public class DoNotUseLocalFunctionsInMarkupTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RenderTreeBuilderAnalyzer());

    [Fact]
    public async Task LocalFunctionWithRenderTreeBuilderCall_ProducesDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components.Rendering;

var builder = new RenderTreeBuilder();

void /*MM*/LocalFunction()
{
    builder.OpenElement(0, ""div"");
    builder.CloseElement();
}

LocalFunction();
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var analyzerDiagnostic = diagnostics.FirstOrDefault(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.NotNull(analyzerDiagnostic);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
        Assert.StartsWith("Local function 'LocalFunction' accesses RenderTreeBuilder from parent scope", analyzerDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task LocalFunctionWithMultipleRenderTreeBuilderCalls_ProducesDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components.Rendering;

var builder = new RenderTreeBuilder();

void /*MM*/LocalFunction()
{
    builder.OpenElement(0, ""div"");
    builder.AddContent(1, ""text"");
    builder.CloseElement();
}

LocalFunction();
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var analyzerDiagnostic = diagnostics.FirstOrDefault(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.NotNull(analyzerDiagnostic);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
        Assert.StartsWith("Local function 'LocalFunction' accesses RenderTreeBuilder from parent scope", analyzerDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task LocalFunctionWithoutRenderTreeBuilderCall_NoDiagnostic()
    {
        // Arrange
        var source = @"
void LocalFunction()
{
    var x = 5;
    System.Console.WriteLine(x);
}

LocalFunction();
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithParameterRenderTreeBuilder_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Components.Rendering;

void LocalFunction(RenderTreeBuilder builder)
{
    builder.OpenElement(0, ""div"");
    builder.CloseElement();
}

var builder = new RenderTreeBuilder();
LocalFunction(builder);
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task NestedLocalFunctionWithRenderTreeBuilderCall_ProducesDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components.Rendering;

var builder = new RenderTreeBuilder();

void OuterFunction()
{
    void /*MM*/InnerFunction()
    {
        builder.OpenElement(0, ""div"");
        builder.CloseElement();
    }
    
    InnerFunction();
}

OuterFunction();
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup).ToList();
        var innerFunctionDiagnostic = analyzerDiagnostics.FirstOrDefault(d => d.GetMessage(CultureInfo.InvariantCulture).Contains("InnerFunction"));
        Assert.NotNull(innerFunctionDiagnostic);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, innerFunctionDiagnostic.Location);
        Assert.StartsWith("Local function 'InnerFunction' accesses RenderTreeBuilder from parent scope", innerFunctionDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task StaticLocalFunction_NoDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Components.Rendering;

var builder = new RenderTreeBuilder();

static void LocalFunction(RenderTreeBuilder builderParam)
{
    builderParam.OpenElement(0, ""div"");
    builderParam.CloseElement();
}

LocalFunction(builder);
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        var analyzerDiagnostics = diagnostics.Where(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.Empty(analyzerDiagnostics);
    }

    [Fact]
    public async Task LocalFunctionWithMethodInvocation_ProducesDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Components.Rendering;

var builder = new RenderTreeBuilder();

void /*MM*/LocalFunction()
{
    builder.AddMarkupContent(0, ""<div>Hello</div>"");
}

LocalFunction();
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var analyzerDiagnostic = diagnostics.FirstOrDefault(d => d.Descriptor == DiagnosticDescriptors.DoNotUseLocalFunctionsInMarkup);
        Assert.NotNull(analyzerDiagnostic);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, analyzerDiagnostic.Location);
        Assert.StartsWith("Local function 'LocalFunction' accesses RenderTreeBuilder from parent scope", analyzerDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }
}