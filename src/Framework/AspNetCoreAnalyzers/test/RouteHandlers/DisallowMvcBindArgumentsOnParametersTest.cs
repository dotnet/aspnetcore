// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class DisallowMvcBindArgumentsOnParametersTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RouteHandlerAnalyzer());

    [Fact]
    public async Task MinimalAction_WithoutBindAttributes_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", (string name) => {});
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_WithAllowedMvcAttributes_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var webApp = WebApplication.Create();
webApp.MapGet(""/{id}"", ([FromBody] string name, [FromRoute] int id, [FromQuery] int age) => {});
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_Lambda_WithBindAttributes_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", (/*MM*/[Bind] string name) => {});
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseModelBindingAttributesOnRouteHandlerParameters, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("BindAttribute should not be specified for a MapGet Delegate parameter", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_MethodReference_WithBindAttributes_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var webApp = WebApplication.Create();
webApp.MapPost(""/"", PostWithBind);

static void PostWithBind(/*MM*/[ModelBinder] string name) {}
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseModelBindingAttributesOnRouteHandlerParameters, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("ModelBinderAttribute should not be specified for a MapPost Delegate parameter", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }
}

