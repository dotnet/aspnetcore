// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.WebApplicationBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers.WebApplicationBuilderFixer>;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public partial class DisallowConfigureHostLoggingTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());

    [Fact]
    public async Task DoesNotWarnWhenBuilderLoggingIsUsed()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
";
        //Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task DoesNotWarnWhenBuilderLoggingIsUsed_InMain()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddJsonConsole();
    }
}
public class Startup { }
";

        //assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task WarnsWhenBuilderLoggingIsNotUsed_Host()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Host.{|#0:ConfigureLogging(logging => logging.AddJsonConsole())|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
";

        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderLoggingIsNotUsed_WebHost()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.{|#0:ConfigureLogging(logging => logging.AddJsonConsole())|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
";

        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderLoggingIsNotUsed_OnDifferentLine_Host()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Host.
    {|#0:ConfigureLogging(logging => logging.AddJsonConsole())|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
";
        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderLoggingIsNotUsed_OnDifferentLine_WebHost()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.
    {|#0:ConfigureLogging(logging => logging.AddJsonConsole())|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
";

        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderLoggingIsNotUsed_InMain_Host()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.{|#0:ConfigureLogging(logging => logging.AddJsonConsole())|};
    }
}
public class Startup { }
";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddJsonConsole();
    }
}
public class Startup { }
";

        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderLoggingIsNotUsed_InMain_WebHost()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.{|#0:ConfigureLogging(logging => logging.AddJsonConsole())|};
    }
}
public class Startup { }
";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddJsonConsole();
    }
}
public class Startup { }
";

        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0);

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderLoggingIsNotUsed_WhenChained_WebHost()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.
    /*MM*/ConfigureLogging(logging => { })
    .ConfigureServices(services => { });
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        //assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseHostConfigureLogging, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using builder.Logging instead of ConfigureLogging", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WarnsTwiceWhenBuilderLoggingIsNotUsed_Host()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Host.{|#0:ConfigureLogging(logging => logging.AddJsonConsole())|};
builder.Host.{|#1:ConfigureLogging(logging => logging.AddJsonConsole())|};
";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole();
builder.Logging.AddJsonConsole();
";
        var expectedDiagnostic = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(1)
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenConfigureLoggingIsCalledWhenChainedWithCreateBuilder()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
WebApplication.CreateBuilder(args).Host.{|#0:ConfigureLogging(logging => logging.AddJsonConsole())|};
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
WebApplication.CreateBuilder(args).Logging.AddJsonConsole();
";
        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenConfigureLoggingIsCalledAsAnArgument()
    {
        //arrange
        var source = @"
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(builder.Host.{|#0:ConfigureLogging(logging => logging.AddJsonConsole())|});
";
        var fixedSource = @"
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(builder.Logging.AddJsonConsole());
";
        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureLogging).WithArguments("ConfigureLogging").WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }
}

