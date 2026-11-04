// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.WebApplicationBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers.WebApplicationBuilderFixer>;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public partial class DisallowConfigureServicesTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());

    [Fact]
    public async Task DoesNotWarnWhenBuilderConfigureServicesIsNotUsed()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task WarnsWhenBuilderHostConfigureServicesIsUsed()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Host.{|#0:ConfigureServices(services => services.AddAntiforgery())|};
";

        var fixedSource = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
";

        // Act
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureServices).WithArguments("ConfigureServices").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderWebHostConfigureServicesIsUsed()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.{|#0:ConfigureServices(services => services.AddAntiforgery())|};
";

        var fixedSource = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
";

        // Act
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureServices).WithArguments("ConfigureServices").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderHostConfigureServicesIsUsed_OnDifferentLine()
    {
        //Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Host.
    {|#0:ConfigureServices(services => services.AddAntiforgery())|};
";

        var fixedSource = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
";

        // Act
        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureServices).WithArguments("ConfigureServices").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderWebHostConfigureServicesIsUsed_OnDifferentLine()
    {
        //Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.
    {|#0:ConfigureServices(services => services.AddAntiforgery())|};
";

        var fixedSource = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
";

        // Act
        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureServices).WithArguments("ConfigureServices").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task DoesNotWarnWhenBuilderConfigureServicesIsNotUsed_InProgramMain()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddAntiforgery();
    }
}
public class Startup { }
";

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task WarnsWhenBuilderHostConfigureServicesIsUsedOnProperty_In_Program_Main()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.{|#0:ConfigureServices(services => services.AddAntiforgery())|};
    }
}
public class Startup { }
";

        var fixedSource = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddAntiforgery();
    }
}
public class Startup { }
";

        // Act
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureServices).WithArguments("ConfigureServices").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderWebHostConfigureServicesIsUsedOnProperty_In_Program_Main()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.{|#0:ConfigureServices(services => services.AddAntiforgery())|};
    }
}
public class Startup { }
";

        var fixedSource = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddAntiforgery();
    }
}
public class Startup { }
";

        // Act
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureServices).WithArguments("ConfigureServices").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenBuilderHostConfigureServicesIsUsed_Inside_Another_Method()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureHost(builder.Host);
    }

    private static void ConfigureHost(ConfigureHostBuilder hostBuilder)
    {
        hostBuilder
            ./*MM*/ConfigureServices(services =>
            {
            services.AddAntiforgery();
            });
    }
}
public class Startup { }
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseHostConfigureServices, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using builder.Services instead of ConfigureServices", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WarnsTwiceWhenBuilderLoggingIsNotUsed_Host()
    {
        //arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Host.{|#0:ConfigureServices(services => services.AddAntiforgery())|};
builder.Host.{|#1:ConfigureServices(services => services.AddAntiforgery())|};
";

        var fixedSource = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
builder.Services.AddAntiforgery();
";
        var expectedDiagnostic = new[]
{
            new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureServices).WithArguments("ConfigureServices").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.DoNotUseHostConfigureServices).WithArguments("ConfigureServices").WithLocation(1)
        };

        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

}
