// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.Http;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpAnalyzerVerifier<
    Microsoft.AspNetCore.Analyzers.Http.RequestDelegateReturnTypeAnalyzer>;

public class RequestDelegateReturnTypeAnalyzerTests
{
    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ThrowError_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    context.SetEndpoint(new Endpoint(c => throw new Exception(), EndpointMetadataCollection.Empty, ""Test""));
    await next();
});
");
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnNull_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    context.SetEndpoint(new Endpoint(c => null, EndpointMetadataCollection.Empty, ""Test""));
    await next();
});
");
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnType_EndpointCtor_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    context.SetEndpoint(new Endpoint({|#0:c => { return Task.FromResult(DateTime.Now); }|}, EndpointMetadataCollection.Empty, ""Test""));
    await next();
});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("System.DateTime")));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnType_AsTask_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", {|#0:(HttpContext context) =>
{
    return context.Request.ReadFromJsonAsync<object>().AsTask();
}|});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("object?")));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnType_DelegateCtor_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(next =>
{
    return new RequestDelegate({|#0:(HttpContext context) =>
    {
        next(context).Wait();
        return Task.FromResult(""hello world"");
    }|});
});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("string")));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnTypeMethodCall_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", {|#0:(HttpContext context) => Task.FromResult(""hello world"")|});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("string")));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnTypeVariable_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"",{|#0:(HttpContext context) =>
{
    var t = Task.FromResult(""hello world"");
    return t;
}|});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("string")));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnTypeTernary_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", {|#0:(HttpContext context) =>
{
    var t1 = Task.FromResult(""hello world"");
    var t2 = Task.FromResult(""hello world"");
    return t1.IsCompleted ? t1 : t2;
}|});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("string")));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_ReturnTypeCoalesce_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", {|#0:(HttpContext context) =>
{
    var t1 = Task.FromResult(""hello world"");
    var t2 = Task.FromResult(""hello world"");
    return t1 ?? t2;
}|});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("string")));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_MultipleReturns_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", {|#0:(HttpContext context) =>
{
    var t1 = Task.FromResult(""hello world"");
    var t2 = Task.FromResult(""hello world"");
    if (t1.IsCompleted)
    {
        return t1;
    }
    else
    {
        return t2;
    }
}|});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("string")));
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_MixReturnValues_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", {|#0:(HttpContext context) =>
{
    var t1 = Task.FromResult(""hello world"");
    var t2 = Task.FromResult(1);
    if (t1.IsCompleted)
    {
        return Task.CompletedTask;
    }
    else
    {
        return t2;
    }
}|});
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("int")));
    }

    [Fact]
    public async Task AnonymousDelegate_NotRequestDelegate_Async_HasReturnType_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", async (HttpContext context) => ""hello world"");
");
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_Async_HasReturns_NoReturnType_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", async (HttpContext context) =>
{
    if (Task.CompletedTask.IsCompleted)
    {
        await Task.Yield();
        return;
    }
    else
    {
        await Task.Delay(1000);
        return;
    }
});
");
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_NoReturnType_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", (HttpContext context) => Task.CompletedTask);
");
    }

    [Fact]
    public async Task AnonymousDelegate_RequestDelegate_MultipleReturns_NoReturnType_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", (HttpContext context) =>
{
    if (Task.CompletedTask.IsCompleted)
    {
        return Task.CompletedTask;
    }
    else
    {
        return Task.CompletedTask;
    }
});
");
    }

    [Fact]
    public async Task MethodReference_RequestDelegate_HasReturnType_ReportDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", {|#0:HttpMethod|});

static Task<string> HttpMethod(HttpContext context) => Task.FromResult(""hello world"");
",
        new DiagnosticResult(DiagnosticDescriptors.DoNotReturnValueFromRequestDelegate)
            .WithLocation(0)
            .WithMessage(Resources.FormatAnalyzer_RequestDelegateReturnValue_Message("string")));
    }

    [Fact]
    public async Task MethodReference_RequestDelegate_NoReturnType_NoDiagnostics()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.MapGet(""/"", HttpMethod);

static Task HttpMethod(HttpContext context) => Task.CompletedTask;
");
    }
}
