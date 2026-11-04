// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public partial class DisallowConfigureWebHostBuilderUseStartupTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithoutUseStartup_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder();
builder.WebHost.ConfigureKestrel(options => { });
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithoutUseStartupGenericType_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder();
builder.WebHost./*MM*/UseStartup<Startup>();
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("UseStartup cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithoutUseStartupType_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder();
builder.WebHost./*MM*/UseStartup(typeof(Startup));
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("UseStartup cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithoutUseStartupString_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder();
builder.WebHost./*MM*/UseStartup(""Startup"");
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("UseStartup cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithoutUseStartupGenericTypeWithContext_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder();
builder.WebHost./*MM*/UseStartup(context => new Startup());
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("UseStartup cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithUseStartupOnProperty_ProducesDiagnostics_In_Program_Main()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost./*MM*/UseStartup<Startup>();
    }
}
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("UseStartup cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithUseStartupOnBuilder_ProducesDiagnostics_In_Program_Main()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var webHost = builder.WebHost;
        webHost./*MM*/UseStartup<Startup>();
    }
}
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseUseStartupWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("UseStartup cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task HostBuilder_WebHostBuilder_UseStartup_DoesNotProduceDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
var builder = Host.CreateDefaultBuilder();
builder.ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.UseStartup<Startup>());
builder.ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.UseStartup(typeof(Startup)));
builder.ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.UseStartup(""Startup""));
builder.ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.UseStartup(context => new Startup()));
public class Startup { }
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WebHostBuilder_UseStartup_DoesNotProduceDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
var builder = WebHost.CreateDefaultBuilder();
builder.UseStartup<Startup>();
builder.UseStartup(typeof(Startup));
builder.UseStartup(""Startup"");
builder.UseStartup(context => new Startup());
public class Startup { }
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
