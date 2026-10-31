// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public partial class DisallowConfigureWebHostBuilderConfigureTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithoutConfigure_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.WebHost.ConfigureKestrel(options => { });
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithConfigure_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.WebHost./*MM*/Configure(webHostBuilder => { });
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Configure cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithConfigureWithContext_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.WebHost./*MM*/Configure((context, webHostBuilder) => { });
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Configure cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithConfigureOnProperty_ProducesDiagnostics_In_Program_Main()
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
        builder.WebHost./*MM*/Configure((context, webHostBuilder) => { });
    }
}
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Configure cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithConfigureOnBuilder_ProducesDiagnostics_In_Program_Main()
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
        webHost./*MM*/Configure((context, webHostBuilder) => { });
    }
}
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWithConfigureWebHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Configure cannot be used with WebApplicationBuilder.WebHost", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task HostBuilder_WebHostBuilder_Configure_DoesNotProduceDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
var builder = Host.CreateDefaultBuilder();
builder.ConfigureWebHostDefaults(webHostBuilder => webHostBuilder.Configure(configure => { }));
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WebHostBuilder_Configure_DoesNotProduceDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
var builder = WebHost.CreateDefaultBuilder();
builder.Configure(configure => { });
builder.Configure((context, webHostBuilder) => { });
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
