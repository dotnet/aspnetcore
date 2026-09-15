// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.RerouteAuthorizationAnalyzer,
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers.RerouteAuthorizationFixer>;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public sealed class RerouteAuthorizationAnalyzerTests
{
    [Fact]
    public async Task StatusCodeReExecuteWithImplicitRoutingProducesDiagnosticAndFix()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

{|#0:app.UseStatusCodePagesWithReExecute("/errors/{0}")|};

app.MapGet("/errors/{statusCode}", () => "Error").RequireAuthorization();
""";
        var fixedSource = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.UseRouting();

app.MapGet("/errors/{statusCode}", () => "Error").RequireAuthorization();
""";
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.ExplicitRoutingRequiredForAuthorization)
            .WithLocation(0)
            .WithArguments("UseStatusCodePagesWithReExecute");

        await VerifyCS.VerifyCodeFixAsync(source, diagnostic, fixedSource);
    }

    [Fact]
    public async Task RewriterWithImplicitRoutingProducesDiagnosticAndFix()
    {
        var source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var options = new RewriteOptions().AddRewrite("^old$", "new", skipRemainingRules: true);
{|#0:app.UseRewriter(options)|};
app.UseAuthorization();

app.MapGet("/new", () => "New");
""";
        var fixedSource = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var options = new RewriteOptions().AddRewrite("^old$", "new", skipRemainingRules: true);
app.UseRewriter(options);
app.UseRouting();

app.UseAuthorization();

app.MapGet("/new", () => "New");
""";
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.ExplicitRoutingRequiredForAuthorization)
            .WithLocation(0)
            .WithArguments("UseRewriter");

        await VerifyCS.VerifyCodeFixAsync(source, diagnostic, fixedSource);
    }

    [Fact]
    public async Task RerouterWithoutAuthorizationDoesNotProduceDiagnostic()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.MapGet("/errors/{statusCode}", () => "Error");
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task ExceptionHandlerPathWithImplicitRoutingProducesDiagnosticAndFix()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

{|#0:app.UseExceptionHandler("/error")|};
app.MapGet("/error", () => "Error").RequireAuthorization();
""";
        var fixedSource = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseRouting();

app.MapGet("/error", () => "Error").RequireAuthorization();
""";
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.ExplicitRoutingRequiredForAuthorization)
            .WithLocation(0)
            .WithArguments("UseExceptionHandler");

        await VerifyCS.VerifyCodeFixAsync(source, diagnostic, fixedSource);
    }

    [Fact]
    public async Task ExceptionHandlerBranchDoesNotProduceDiagnostic()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseExceptionHandler(errorApp => { });
app.MapGet("/error", () => "Error").RequireAuthorization();
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task StatusCodeReExecuteAfterExplicitRoutingProducesDiagnosticAndFix()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseRouting();
{|#0:app.UseStatusCodePagesWithReExecute("/errors/{0}")|};
app.MapGet("/errors/{statusCode}", () => "Error").RequireAuthorization();
""";
        var fixedSource = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.UseRouting();
app.MapGet("/errors/{statusCode}", () => "Error").RequireAuthorization();
""";
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.ReroutingMiddlewareMustPrecedeRouting)
            .WithLocation(0)
            .WithArguments("UseStatusCodePagesWithReExecute");

        await VerifyCS.VerifyCodeFixAsync(source, diagnostic, fixedSource);
    }

    [Fact]
    public async Task StatusCodeReExecuteBeforeExplicitRoutingDoesNotProduceDiagnostic()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/errors/{0}");
app.UseRouting();
app.MapGet("/errors/{statusCode}", () => "Error").RequireAuthorization();
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RewriterBeforeExplicitRoutingDoesNotProduceDiagnostic()
    {
        var source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var options = new RewriteOptions().AddRewrite("^old$", "new", skipRemainingRules: true);
app.UseRewriter(options);
app.UseRouting();
app.MapGet("/new", () => "New").RequireAuthorization();
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public async Task RewriterAfterExplicitRoutingProducesDiagnosticAndFix()
    {
        var source = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var options = new RewriteOptions().AddRewrite("^old$", "new", skipRemainingRules: true);
app.UseRouting();
{|#0:app.UseRewriter(options)|};
app.MapGet("/new", () => "New").RequireAuthorization();
""";
        var fixedSource = """
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var options = new RewriteOptions().AddRewrite("^old$", "new", skipRemainingRules: true);
app.UseRewriter(options);
app.UseRouting();
app.MapGet("/new", () => "New").RequireAuthorization();
""";
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.ReroutingMiddlewareMustPrecedeRouting)
            .WithLocation(0)
            .WithArguments("UseRewriter");

        await VerifyCS.VerifyCodeFixAsync(source, diagnostic, fixedSource);
    }

    [Fact]
    public async Task RerouterSeparatedFromExplicitRoutingProducesDiagnosticWithoutFix()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseRouting();
app.Use(async (context, next) => await next(context));
{|#0:app.UseStatusCodePagesWithReExecute("/errors/{0}")|};
app.MapGet("/errors/{statusCode}", () => "Error").RequireAuthorization();
""";
        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.ReroutingMiddlewareMustPrecedeRouting)
            .WithLocation(0)
            .WithArguments("UseStatusCodePagesWithReExecute");

        await VerifyCS.VerifyAnalyzerAsync(source, diagnostic);
    }

    [Fact]
    public async Task ConditionalRerouterDoesNotProduceDiagnostic()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (true)
{
    app.UseStatusCodePagesWithReExecute("/errors/{0}");
}

app.MapGet("/errors/{statusCode}", () => "Error").RequireAuthorization();
""";

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
