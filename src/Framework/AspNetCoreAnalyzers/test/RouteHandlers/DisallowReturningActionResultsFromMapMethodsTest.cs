// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class DisallowReturningActionResultsFromMapMethodsTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RouteHandlerAnalyzer());

    [Fact]
    public async Task MinimalAction_ReturningIResult_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();
webApp.MapGet(""/"", () => Results.Ok(""Hello""));
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_ReturningCustomIResult_Works()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();
webApp.MapGet(""/"", () => new CustomResult());

class CustomResult : IResult
{
    public Task ExecuteAsync(HttpContext context) => Task.CompletedTask;
}
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_ReturningIResultConditionallyWorks()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();
webApp.MapGet(""/"", (int id) =>
{
    if (id == 0)
    {
        return Results.NotFound();
    }

    return Results.Ok(""Here you go"");
});

";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_ReturningTypeThatImplementsIResultAndActionResultDoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();
webApp.MapGet(""/"", () => new CustomResult());

class CustomResult : IResult, IActionResult
{
    public Task ExecuteAsync(HttpContext context) => Task.CompletedTask;

    public Task ExecuteResultAsync(ActionContext context) => Task.CompletedTask;
}
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_ReturningActionResult_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();
webApp.MapGet(""/"", () => /*MM*/new OkObjectResult(""cool story""));
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("IActionResult instances should not be returned from a MapGet Delegate parameter. Consider returning an equivalent result from Microsoft.AspNetCore.Http.Results.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_ReturningActionResultConditionally_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();
webApp.MapGet(""/"", (int id) => /*MM*/id == 0 ? (ActionResult)new NotFoundResult() : new OkObjectResult(""cool story""));
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("IActionResult instances should not be returned from a MapGet Delegate parameter. Consider returning an equivalent result from Microsoft.AspNetCore.Http.Results.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_ReturningActionResultFromMethodReference_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();
webApp.MapPost(""/"", OkObjectResultReturningMethod);

static object OkObjectResultReturningMethod()
{
    /*MM*/return new OkObjectResult(""ok"");
}
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("IActionResult instances should not be returned from a MapPost Delegate parameter. Consider returning an equivalent result from Microsoft.AspNetCore.Http.Results.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_ReturningActionResultOfTFromMethodReference_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var webApp = WebApplication.Create();
webApp.MapPost(""/"", ActionResultMethod);

static async Task<ActionResult<Person>> ActionResultMethod(int id)
{
    await Task.Yield();
    if (id == 0) /*MM*/return new NotFoundResult();
    return new Person(""test"");
}

public record Person(string Name);
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("IActionResult instances should not be returned from a MapPost Delegate parameter. Consider returning an equivalent result from Microsoft.AspNetCore.Http.Results.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_ReturningActionResultOfTFromANonLocalFunction_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var webApp = WebApplication.Create();
webApp.MapPost(""/"", /*MM*/new MyController().ActionResultMethod);
");

        var controllerSource = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public class MyController
{
    public async Task<ActionResult<Person>> ActionResultMethod(int id)
    {
        await Task.Yield();
        if (id == 0) return new NotFoundResult();
        return new Person(""test"");
    }

    public record Person(string Name);
}
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source, controllerSource);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("IActionResult instances should not be returned from a MapPost Delegate parameter. Consider returning an equivalent result from Microsoft.AspNetCore.Http.Results.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_ReturningActionResultOfTDeclarationInDifferentFile_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var webApp = WebApplication.Create();
webApp.MapPost(""/"", /*MM*/MyController.ActionResultMethod);
");
        var controllerSource = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

public static class MyController
{
    public static async Task<ActionResult<Person>> ActionResultMethod(int id)
    {
        await Task.Yield();
        if (id == 0) return new NotFoundResult();
        return new Person(""test"");
    }

    public record Person(string Name);
}
";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source, controllerSource);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotReturnActionResultsFromRouteHandlers, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("IActionResult instances should not be returned from a MapPost Delegate parameter. Consider returning an equivalent result from Microsoft.AspNetCore.Http.Results.", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }
}

