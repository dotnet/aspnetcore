// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.PublicPartialProgramClassAnalyzer,
    Microsoft.AspNetCore.Fixers.PublicPartialProgramClassFixer>;

namespace Microsoft.AspNetCore.Analyzers;

public class PublicPartialProgramClassTest
{
    [Fact]
    public async Task DoesNothingWhenNoDeclarationIsFound()
    {
        // Arrange
        var source = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();
""";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Theory]
    [InlineData("public partial class Program { }")]
    [InlineData("public partial class Program { /* This is just for tests */ }")]
    [InlineData("public partial class Program { \n // This is just for tests \n }")]
    [InlineData("public partial class Program;")]
    public async Task RemovesDeclarationIfItIsFound(string declarationStyle)
    {
        // Arrange
        var source = $$"""
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

{|#0:{{declarationStyle}}|}
""";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.PublicPartialProgramClassNotRequired)
                .WithLocation(0);

        var fixedSource = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();


""";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, [diagnostic], fixedSource);
    }

    [Fact]
    public async Task RemovesDeclarationIfItIsFound_WithLeadingTrivia()
    {
        // Arrange
        var source = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

// This is a test

{|#0:public partial class Program;|}
""";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.PublicPartialProgramClassNotRequired)
                .WithLocation(0);

        var fixedSource = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

// This is a test


""";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, [diagnostic], fixedSource);
    }

    [Fact]
    public async Task DoesNotGeneratesSource_IfProgramDeclaresExplicitInternalAccess()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

internal partial class Program { }
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task DoesNotFix_ExplicitPublicProgramClass()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

public class Program
{
    public static void Main()
    {
        var app = WebApplication.Create();

        app.MapGet("/", () => "Hello, World!");

        app.Run();
    }
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task DoesNotFix_ExplicitPublicPartialProgramClass()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

public partial class Program
{
    public static void Main()
    {
        var app = WebApplication.Create();

        app.MapGet("/", () => "Hello, World!");

        app.Run();
    }
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Theory]
    [InlineData("public int Number { get; set; }")]
    [InlineData("private int _foo = 2;")]
    public async Task DoesNotFix_ExplicitPublicPartialProgramClass_WithProperty(string contents)
    {
        var source = $$"""
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

public partial class Program
{
    {{contents}}
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Theory]
    [InlineData("namespace Foo")]
    [InlineData("public class Foo")]
    public async Task DoesNotFix_ExplicitPublicPartialProgramClassInNestedPattern(string parentDefinition)
    {
        var source = $$"""
using Microsoft.AspNetCore.Builder;

var app = WebApplication.Create();

app.MapGet("/", () => "Hello, World!");

app.Run();

{{parentDefinition}}
{
    public partial class Program { }
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task DoesNotFix_ExplicitInternalProgramClass()
    {
        var source = """
using Microsoft.AspNetCore.Builder;

internal class Program
{
    public static void Main()
    {
        var app = WebApplication.Create();

        app.MapGet("/", () => "Hello, World!");

        app.Run();
    }
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Theory]
    [InlineData("interface")]
    [InlineData("struct")]
    public async Task DoesNotFix_ExplicitInternalProgramType(string type)
    {
        var source = $$"""
using Microsoft.AspNetCore.Builder;

internal {{type}} Program
{
    public static void Main(string[] args)
    {
        var app = WebApplication.Create();

        app.MapGet("/", () => "Hello, World!");

        app.Run();
    }
}
""";

        await VerifyCS.VerifyCodeFixAsync(source, source);
    }
}

