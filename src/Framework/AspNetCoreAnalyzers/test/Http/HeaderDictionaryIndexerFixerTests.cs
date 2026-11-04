// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.Http.HeaderDictionaryIndexerAnalyzer,
    Microsoft.AspNetCore.Analyzers.Http.Fixers.HeaderDictionaryIndexerFixer>;

namespace Microsoft.AspNetCore.Analyzers.Http;

public class HeaderDictionaryIndexerFixerTests
{
    [Fact]
    public async Task IHeaderDictionary_Get_MismatchCase_Fixed()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    var s = {|#0:context.Request.Headers[""content-type""]|};
    await next();
});
",
        new DiagnosticResult[]
        {
            new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
                .WithLocation(0)
                .WithMessage("The header 'content-type' can be accessed using the ContentType property"),
        },
        @"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    var s = context.Request.Headers.ContentType;
    await next();
});
");
    }

    [Fact]
    public async Task IHeaderDictionary_Set_MismatchCase_Fixed()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    {|#0:context.Request.Headers[""content-type""]|} = """";
    await next();
});
",
        new DiagnosticResult[]
        {
            new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
                .WithLocation(0)
                .WithMessage("The header 'content-type' can be accessed using the ContentType property"),
        },
        @"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
var webApp = WebApplication.Create();
webApp.Use(async (HttpContext context, Func<Task> next) =>
{
    context.Request.Headers.ContentType = """";
    await next();
});
");
    }

    [Fact]
    public async Task HeaderDictionary_CastToIHeaderDictionary_SetFromMethod_KnownProperty_Fix()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(@"
using Microsoft.AspNetCore.Http;
IHeaderDictionary headers = new HeaderDictionary();
{|#0:headers[""Content-Type""]|} = GetValue();

static string GetValue() => string.Empty;
",
        new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
            .WithLocation(0)
            .WithMessage("The header 'Content-Type' can be accessed using the ContentType property"),
@"
using Microsoft.AspNetCore.Http;
IHeaderDictionary headers = new HeaderDictionary();
headers.ContentType = GetValue();

static string GetValue() => string.Empty;
");
    }

    [Fact]
    public async Task HeaderDictionary_CastToIHeaderDictionary_GetToMethod_KnownProperty_Fix()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(@"
using Microsoft.AspNetCore.Http;
IHeaderDictionary headers = new HeaderDictionary();
SetValue({|#0:headers[""Content-Type""]|});

static void SetValue(string s)
{
}
",
        new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
            .WithLocation(0)
            .WithMessage("The header 'Content-Type' can be accessed using the ContentType property"),
@"
using Microsoft.AspNetCore.Http;
IHeaderDictionary headers = new HeaderDictionary();
SetValue(headers.ContentType);

static void SetValue(string s)
{
}
");
    }

    [Fact]
    public async Task HttpContext_GetToMethod_KnownProperty_Fix()
    {
        // Arrange & Act & Assert
        await VerifyCS.VerifyCodeFixAsync(@"
using Microsoft.AspNetCore.Http;
var httpContext = new DefaultHttpContext();
SetValue({|#0:httpContext.Request.Headers[""Content-Type""]|});

static void SetValue(string s)
{
}
",
        new DiagnosticResult(DiagnosticDescriptors.UseHeaderDictionaryPropertiesInsteadOfIndexer)
            .WithLocation(0)
            .WithMessage("The header 'Content-Type' can be accessed using the ContentType property"),
@"
using Microsoft.AspNetCore.Http;
var httpContext = new DefaultHttpContext();
SetValue(httpContext.Request.Headers.ContentType);

static void SetValue(string s)
{
}
");
    }
}
