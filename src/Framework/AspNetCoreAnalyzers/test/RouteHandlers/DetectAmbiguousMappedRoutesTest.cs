// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer>;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class DetectAmbiguousMappedRoutesTest
{
    [Fact]
    public async Task DuplicateRoutes_SameHttpMethod_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet({|#0:""/""|}, () => Hello());
app.MapGet({|#1:""/""|}, () => Hello());
void Hello() { }
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_SameHttpMethod_InMethod_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
var app = WebApplication.Create();
void RegisterEndpoints(IEndpointRouteBuilder builder)
{
    builder.MapGet({|#0:""/""|}, () => Hello());
    builder.MapGet({|#1:""/""|}, () => Hello());
}

RegisterEndpoints(app);

void Hello() { }
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_DifferentMethods_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet({|#0:""/""|}, () => Hello());
app.MapPost({|#1:""/""|}, () => Hello());
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateMapGetRoutes_InsideConditional_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
if (true)
{
    app.MapGet(""/"", () => Hello());
}
else
{
    app.MapGet(""/"", () => Hello());
}
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}

