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

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_SameHttpMethod_HasRequestDelegate_HasDiagnostics()
    {
        // Arrange
        var source = @"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
var app = WebApplication.Create();
app.MapGet({|#0:""/""|}, () => Hello());
app.MapGet({|#1:""/""|}, (HttpContext context) => Task.CompletedTask);
void Hello() { }
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_SameHttpMethod_InMethod_HasDiagnostics()
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

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_TernaryStatement_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
_ = (true)
    ? app.MapGet(""/"", () => Hello())
    : app.MapGet(""/"", () => Hello());
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_SwitchStatement_NoDiagnostics()
    {
        // Arrange
        var source = @"
using System;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
switch (Random.Shared.Next())
{
    case 0:
        app.MapGet(""/"", () => Hello());
        return;
    case 1:
        app.MapGet(""/"", () => Hello());
        return;
}
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_InsideSwitchStatement_HasDiagnostics()
    {
        // Arrange
        var source = @"
using System;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
switch (Random.Shared.Next())
{
    case 0:
        app.MapGet({|#0:""/""|}, () => Hello());
        app.MapGet({|#1:""/""|}, () => Hello());
        return;

}
void Hello() { }
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_SwitchExpression_NoDiagnostics()
    {
        // Arrange
        var source = @"
using System;
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
_ = Random.Shared.Next() switch
{
    0 => app.MapGet(""/"", () => Hello()),
    1 => app.MapGet(""/"", () => Hello()),
    _ => throw new Exception()
};
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_NullCoalescing_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
_ = app.MapGet(""/"", () => Hello()) ?? app.MapGet(""/"", () => Hello());
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_NullCoalescingAssignment_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
var ep = app.MapPost(""/"", () => Hello());
ep ??= app.MapPost(""/"", () => Hello());
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_DifferentMethods_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", () => Hello());
app.MapPost(""/"", () => Hello());
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

    [Fact]
    public async Task DuplicateMapGetRoutes_DuplicatesInsideConditional_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
if (true)
{
    app.MapGet({|#0:""/""|}, () => Hello());
    app.MapGet({|#1:""/""|}, () => Hello());
}
void Hello() { }
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_UnknownUsageOfEndConventionBuilderExtension_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", () => Hello()).DoSomething();
app.MapGet(""/"", () => Hello());
void Hello() { }

internal static class Extensions
{
    public static void DoSomething(this IEndpointConventionBuilder builder)
    {
        builder.WithMetadata(new object());
    }
}
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_UnknownUsageOfEndConventionBuilder_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
Extensions.DoSomething(app.MapGet(""/"", () => Hello()));
app.MapGet(""/"", () => Hello());
void Hello() { }

internal static class Extensions
{
    public static void DoSomething(IEndpointConventionBuilder builder)
    {
        builder.WithMetadata(new object());
    }
}
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_AddMethod_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGet(""/"", () => Hello()).Add(b => {});
app.MapGet(""/"", () => Hello());
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_AssignedToVariable_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
_ = app.MapGet(""/"", () => Hello());
app.MapGet(""/"", () => Hello());
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_MultipleGroups_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
var group1 = app.MapGroup(""/group1"");
var group2 = app.MapGroup(""/group2"");
group1.MapGet(""/"", () => Hello());
group2.MapGet(""/"", () => Hello());
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_MultipleGroups_DirectInvocation_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGroup(""/group1"").MapGet(""/"", () => { });
app.MapGroup(""/group2"").MapGet(""/"", () => { });
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_SingleGroup_DifferentBuilderVariable_DirectInvocation_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app1 = WebApplication.Create();
var app2 = app1;
app1.MapGroup(""/group1"").MapGet(""/"", () => { });
app2.MapGroup(""/group1"").MapGet(""/"", () => { });
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task DuplicateRoutes_SingleGroup_DirectInvocation_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
app.MapGroup(""/group1"").MapGet({|#0:""/""|}, () => { });
app.MapGroup(""/group1"").MapGet({|#1:""/""|}, () => { });
";

        // Act & Assert
        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_SingleGroup_DirectInvocation_InMethod_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
var app = WebApplication.Create();
void RegisterEndpoints(IEndpointRouteBuilder builder)
{
    builder.MapGroup(""/group1"").MapGet({|#0:""/""|}, () => { });
    builder.MapGroup(""/group1"").MapGet({|#1:""/""|}, () => { });
}

RegisterEndpoints(app);

void Hello() { }
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_SingleGroup_RoutePattern_DirectInvocation_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing.Patterns;
var routePattern = RoutePatternFactory.Parse(""/group1"");
var app = WebApplication.Create();
app.MapGroup(routePattern).MapGet({|#0:""/""|}, () => { });
app.MapGroup(routePattern).MapGet({|#1:""/""|}, () => { });
";

        // Act & Assert
        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Fact]
    public async Task DuplicateRoutes_EndpointsOnGroup_HasDiagnostics()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
var app = WebApplication.Create();
var group1 = app.MapGroup(""/group1"");
group1.MapGet({|#0:""/""|}, () => Hello());
group1.MapGet({|#1:""/""|}, () => Hello());
var group2 = app.MapGroup(""/group2"");
group2.MapGet(""/"", () => Hello());
void Hello() { }
";

        // Act & Assert
        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Theory]
    [InlineData(@"RequireAuthorization()")]
    [InlineData(@"AllowAnonymous()")]
    [InlineData(@"Produces(statusCode:420)")]
    [InlineData(@"WithDisplayName(""test!"")")]
    [InlineData(@"WithName(""test!"")")]
    [InlineData(@"RequireCors(""test!"")")]
    [InlineData(@"CacheOutput(""test!"")")]
    [InlineData(@"DisableRateLimiting()")]
    [InlineData(@"RequireAuthorization().DisableRateLimiting()")]
    public async Task DuplicateRoutes_AllowedBuilderExtensionMethods_HasDiagnostics(string method)
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
var app = WebApplication.Create();
app.MapGet({|#0:""/""|}, () => Hello())." + method + @";
app.MapGet({|#1:""/""|}, () => Hello());
void Hello() { }
";

        var expectedDiagnostics = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.AmbiguousRouteHandlerRoute).WithArguments("/").WithLocation(1)
        };

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source, expectedDiagnostics);
    }

    [Theory]
    [InlineData(@"RequireHost(""test!"")")]
    [InlineData(@"RequireHost(""test!"").DisableRateLimiting()")]
    [InlineData(@"RequireAuthorization().RequireHost(""test!"")")]
    public async Task DuplicateRoutes_UnknownBuilderExtensionMethods_NoDiagnostics(string method)
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
var app = WebApplication.Create();
app.MapGet({|#0:""/""|}, () => Hello())." + method + @";
app.MapGet({|#1:""/""|}, () => Hello());
void Hello() { }
";

        // Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
