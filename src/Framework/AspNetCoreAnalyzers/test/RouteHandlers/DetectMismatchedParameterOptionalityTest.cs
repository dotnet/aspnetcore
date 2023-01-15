// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer,
    Microsoft.AspNetCore.Analyzers.RouteHandlers.Fixers.DetectMismatchedParameterOptionalityFixer>;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public partial class DetectMismatchedParameterOptionalityTest
{
    [Fact]
    public async Task MatchingRequiredOptionality_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}"", ({|#0:string name|}) => $""Hello {name}"");";

        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}"", (string? name) => $""Hello {name}"");";

        var expectedDiagnostics = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task MatchingMultipleRequiredOptionality_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}/{title?}"", ({|#0:string name|}, {|#1:string title|}) => $""Hello {name}, you are a {title}."");
";
        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}/{title?}"", (string? name, string? title) => $""Hello {name}, you are a {title}."");
";
        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("title").WithLocation(1)
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);

    }

    [Fact]
    public async Task MatchingSingleRequiredOptionality_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}/{title?}"", ({|#0:string name|}, string? title) => $""Hello {name}, you are a {title}."");
";
        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}/{title?}"", (string? name, string? title) => $""Hello {name}, you are a {title}."");
";
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task MismatchedOptionalityInMethodGroup_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
string SayHello({|#0:string name|}, {|#1:string title|}) => $""Hello {name}, you are a {title}."";
app.MapGet(""/hello/{name?}/{title?}"", SayHello);
";
        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
string SayHello(string? name, string? title) => $""Hello {name}, you are a {title}."";
app.MapGet(""/hello/{name?}/{title?}"", SayHello);
";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("title").WithLocation(1)
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task MismatchedOptionalityInMethodGroupFromPartialMethod_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}/{title?}"", ExternalImplementation.SayHello);

public partial class ExternalImplementation
{
    public static partial string SayHello({|#0:string name|}, {|#1:string title|});
}

public partial class ExternalImplementation
{
    public static partial string SayHello({|#2:string name|}, {|#3:string title|})
    {
        return $""Hello {name}, you are a {title}."";
    }
}
";
        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}/{title?}"", ExternalImplementation.SayHello);

public partial class ExternalImplementation
{
    public static partial string SayHello(string? name, string? title);
}

public partial class ExternalImplementation
{
    public static partial string SayHello(string? name, string? title)
    {
        return $""Hello {name}, you are a {title}."";
    }
}
";
        // Diagnostics are produced at both the declaration and definition syntax
        // for partial method definitions to support the CodeFix correctly resolving both.
        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("title").WithLocation(1),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithLocation(2),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("title").WithLocation(3)
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task MismatchedOptionalityInSeparateSource_CanBeFixed()
    {
        var usageSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}/{title?}"", Helpers.SayHello);
";
        var source = @"
#nullable enable
using System;

public static class Helpers
{
    public static string SayHello({|#0:string name|}, {|#1:string title|})
    {
        return $""Hello {name}, you are a {title}."";
    }
}";
        var fixedSource = @"
#nullable enable
using System;

public static class Helpers
{
    public static string SayHello(string? name, string? title)
    {
        return $""Hello {name}, you are a {title}."";
    }
}";

        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("title").WithLocation(1)
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, usageSource: usageSource);
    }

    [Fact]
    public async Task MatchingRequiredOptionality_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name}"", (string name) => $""Hello {name}"");
";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task ParameterFromRouteOrQuery_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name}"", (string name) => $""Hello {name}"");
";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task MatchingOptionality_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}"", (string? name) => $""Hello {name}"");
";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task RequiredRouteParamOptionalArgument_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name}"", (string? name) => $""Hello {name}"");
";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task OptionalRouteParamRequiredArgument_WithFromRoute_ProducesDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

var app = WebApplication.Create();
app.MapGet(""/hello/{Age?}"", ({|#0:[FromRoute] int age|}) => $""Age: {age}"");
";

        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

var app = WebApplication.Create();
app.MapGet(""/hello/{Age?}"", ([FromRoute] int? age) => $""Age: {age}"");
";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("age").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task OptionalRouteParamRequiredArgument_WithRegexConstraint_ProducesDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{age:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)?}"", ({|#0:int age|}) => $""Age: {age}"");
";

        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{age:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)?}"", (int? age) => $""Age: {age}"");
";
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("age").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task OptionalRouteParamRequiredArgument_WithTypeConstraint_ProducesDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{age:int?}"", ({|#0:int age|}) => $""Age: {age}"");
";

        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{age:int?}"", (int? age) => $""Age: {age}"");
";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("age").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task MatchingRequiredOptionality_WithDisabledNullability()
    {
        var source = @"
#nullable disable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}"", (string name) => $""Hello {name}"");
";
        var fixedSource = @"
#nullable disable
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();
app.MapGet(""/hello/{name?}"", (string name) => $""Hello {name}"");
";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
