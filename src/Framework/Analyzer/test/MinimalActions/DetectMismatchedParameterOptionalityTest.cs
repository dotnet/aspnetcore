// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Testing.Utilities.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.DelegateEndpoints.DelegateEndpointAnalyzer,
    Microsoft.AspNetCore.Analyzers.DelegateEndpoints.DelegateEndpointFixer>;

namespace Microsoft.AspNetCore.Analyzers.DelegateEndpoints;

public partial class DetectMismatchedParameterOptionalityTest
{
    [Fact]
    public async Task MatchingRequiredOptionality_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}"", (string name) => $""Hello {name}"");
    }
}";
        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}"", (string? name) => $""Hello {name}"");
    }
}";
        var expectedDiagnostics = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithSpan(10, 39, 10, 50);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task MatchingMultipleRequiredOptionality_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}/{title?}"", (string name, string title) => $""Hello {name}, you are a {title}."");
    }
}";
        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}/{title?}"", (string? name, string? title) => $""Hello {name}, you are a {title}."");
    }
}";
        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithSpan(10, 48, 10, 59),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("title").WithSpan(10, 61, 10, 73)
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);

    }

    [Fact]
    public async Task MatchingSingleRequiredOptionality_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}/{title?}"", (string name, string? title) => $""Hello {name}, you are a {title}."");
    }
}";
        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}/{title?}"", (string? name, string? title) => $""Hello {name}, you are a {title}."");
    }
}";
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithSpan(10, 48, 10, 59);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task MismatchedOptionalityInMethodGroup_CanBeFixed()
    {
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        string SayHello(string name, string title) => $""Hello {name}, you are a {title}."";
        app.MapGet(""/hello/{name?}/{title?}"", SayHello);
    }
}";
        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        string SayHello(string? name, string? title) => $""Hello {name}, you are a {title}."";
        app.MapGet(""/hello/{name?}/{title?}"", SayHello);
    }
}";
        var expectedDiagnostics = new[] {
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithSpan(10, 25, 10, 36),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("title").WithSpan(10, 38, 10, 50)
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource);
    }

    [Fact]
    public async Task MismatchedOptionalityInSeparateSource_CanBeFixed()
    {
        var usageSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}/{title?}"", Helpers.SayHello);
    }
}";
        var source = @"
#nullable enable
using System;

public static class Helpers
{
    public static string SayHello(string name, string title)
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
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("name").WithSpan(7, 35, 7, 46),
            new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("title").WithSpan(7, 48, 7, 60),
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostics, fixedSource, usageSource);
    }

    [Fact]
    public async Task MatchingRequiredOptionality_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name}"", (string name) => $""Hello {name}"");
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task ParameterFromRouteOrQuery_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name}"", (string name) => $""Hello {name}"");
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task MatchingOptionality_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
app.MapGet(""/hello/{name?}"", (string? name) => $""Hello {name}"");
    }
    
}";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task RequiredRouteParamOptionalArgument_DoesNotProduceDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
class Program {
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
app.MapGet(""/hello/{name}"", (string? name) => $""Hello {name}"");
    }
}";

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
class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
app.MapGet(""/hello/{Age?}"", ([FromRoute] int age) => $""Age: {age}"");
    }
}";

        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
app.MapGet(""/hello/{Age?}"", ([FromRoute] int? age) => $""Age: {age}"");
    }
}";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("age").WithSpan(10, 30, 10, 49);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task OptionalRouteParamRequiredArgument_WithRegexConstraint_ProducesDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
class Program
{
    static void Main(string[] args)
    {
        
var app = WebApplication.Create();
app.MapGet(""/hello/{age:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)?}"", (int age) => $""Age: {age}"");
    }
}";

        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;
class Program
{
    static void Main(string[] args)
    {
        
var app = WebApplication.Create();
app.MapGet(""/hello/{age:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)?}"", (int? age) => $""Age: {age}"");
    }
}";
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("age").WithSpan(10, 66, 10, 73);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task OptionalRouteParamRequiredArgument_WithTypeConstraint_ProducesDiagnostics()
    {
        // Arrange
        var source = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{age:int?}"", (int age) => $""Age: {age}"");
    }
}";

        var fixedSource = @"
#nullable enable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{age:int?}"", (int? age) => $""Age: {age}"");
    }
}";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DetectMismatchedParameterOptionality).WithArguments("age").WithSpan(10, 42, 10, 49);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task MatchingRequiredOptionality_WithDisabledNullability()
    {
        var source = @"
#nullable disable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}"", (string name) => $""Hello {name}"");
    }
}";
        var fixedSource = @"
#nullable disable
using Microsoft.AspNetCore.Builder;

class Program
{
    static void Main(string[] args)
    {
        var app = WebApplication.Create();
        app.MapGet(""/hello/{name?}"", (string name) => $""Hello {name}"");
    }
}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
