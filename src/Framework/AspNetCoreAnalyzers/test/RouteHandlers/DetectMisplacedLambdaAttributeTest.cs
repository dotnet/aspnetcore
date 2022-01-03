// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class DetectMisplacedLambdaAttributeTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new RouteHandlerAnalyzer());

    [Fact]
    public async Task MinimalAction_WithCorrectlyPlacedAttribute_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", [Authorize] () => Hello());
void Hello() { }
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_WithMisplacedAttribute_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", /*MM*/() => Hello());
[Authorize]
void Hello() { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DetectMisplacedLambdaAttribute, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("'AuthorizeAttribute' should be placed directly on the route handler lambda to be effective", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_WithMisplacedAttributeAndBlockSyntax_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", /*MM*/() => { return Hello(); });
[Authorize]
string Hello() { return ""foo""; }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DetectMisplacedLambdaAttribute, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("'AuthorizeAttribute' should be placed directly on the route handler lambda to be effective", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_WithMultipleMisplacedAttributes_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var app = WebApplication.Create();
app.MapGet(""/"", /*MM*/() => Hello());
[Authorize]
[Produces(""application/xml"")]
void Hello() { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Collection(diagnostics,
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DetectMisplacedLambdaAttribute, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                Assert.Equal("'AuthorizeAttribute' should be placed directly on the route handler lambda to be effective", diagnostic.GetMessage(CultureInfo.InvariantCulture));
            },
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DetectMisplacedLambdaAttribute, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                Assert.Equal("'ProducesAttribute' should be placed directly on the route handler lambda to be effective", diagnostic.GetMessage(CultureInfo.InvariantCulture));
            }
        );
    }

    [Fact]
    public async Task MinimalAction_WithSingleMisplacedAttribute_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
var app = WebApplication.Create();
app.MapGet(""/"", /*MM*/[Authorize]() => Hello());
[Produces(""application/xml"")]
void Hello() { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Collection(diagnostics,
            diagnostic =>
            {
                Assert.Same(DiagnosticDescriptors.DetectMisplacedLambdaAttribute, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                Assert.Equal("'ProducesAttribute' should be placed directly on the route handler lambda to be effective", diagnostic.GetMessage(CultureInfo.InvariantCulture));
            }
        );
    }

    [Fact]
    public async Task MinimalAction_DoesNotWarnOnNonReturningMethods()
    {
        // Arrange
        var source = @"
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", /*MM*/() => {
    Console.WriteLine(""foo"");
    return ""foo"";
});";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_DoesNotWarnOrErrorOnNonExistantLambdas()
    {
        // Arrange
        var source = @"
using System;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", () => ThereIsNoMethod());";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert that there is a singal error but it is not ours (CS0103)
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("CS0103", diagnostic.Descriptor.Id);
    }

    [Fact]
    public async Task MinimalAction_WithMisplacedAttributeOnMethodGroup_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", Hello);
[Authorize]
void Hello() { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MinimalAction_WithMisplacedAttributeOnExternalReference_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
class Program {
    public static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/"", /*MM*/() => Helpers.Hello());
    }
}

public static class Helpers
{
    [Authorize]
    public static void Hello() { }
}
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DetectMisplacedLambdaAttribute, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("'AuthorizeAttribute' should be placed directly on the route handler lambda to be effective", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MinimalAction_OutOfScope_ProducesDiagnostics()
    {
        // Arrange
        // WriteLine has nullability annotation attributes that should
        // not warn
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", /*MM*/() => System.Console.WriteLine(""foo""));
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }
}

