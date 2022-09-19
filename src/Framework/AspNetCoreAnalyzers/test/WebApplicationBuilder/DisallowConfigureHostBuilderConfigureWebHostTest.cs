// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public partial class DisallowConfigureHostBuilderConfigureWebHostTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());

    [Fact]
    public async Task WebApplicationBuilder_HostWithoutConfigureWebHost_Works()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.Host.ConfigureHostOptions(hostBuilder => { });
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WebApplicationBuilder_HostWithConfigureWebHost_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.Host./*MM*/ConfigureWebHost(webHostBuilder => { });
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("ConfigureWebHost cannot be used with WebApplicationBuilder.Host", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_HostWithConfigureWebHost_ProducesDiagnostics_OnDifferentLine()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.Host.
    /*MM*/ConfigureWebHost(webHostBuilder => { });
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("ConfigureWebHost cannot be used with WebApplicationBuilder.Host", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_HostWithConfigureWebHost_ProducesDiagnostics_WhenChained()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.Host
    ./*MM*/ConfigureWebHost(webHostBuilder => { })
    .ConfigureServices(services => { });
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("ConfigureWebHost cannot be used with WebApplicationBuilder.Host", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_HostWithConfigureWebHost_DoesNotProduceDiagnostics_WhenChained()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.Host
    .ConfigureHostOptions(hostBuilder => { }) // Because ConfigureHostOptions() returns IHostBuilder, the type gets erased
    .ConfigureWebHost(webHostBuilder => { });
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WebApplicationBuilder_HostWithConfigureWebHostWithOptions_ProducesDiagnostics()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder();
builder.Host./*MM*/ConfigureWebHost(webHostBuilder => { }, optionsBuilder => { });
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("ConfigureWebHost cannot be used with WebApplicationBuilder.Host", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithConfigureWebHostOnProperty_ProducesDiagnostics_In_Program_Main()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host./*MM*/ConfigureWebHost(webHostBuilder => { });
    }
}
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("ConfigureWebHost cannot be used with WebApplicationBuilder.Host", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithConfigureWebHostOnBuilder_ProducesDiagnostics_In_Program_Main()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var host = builder.Host;
        host./*MM*/ConfigureWebHost(webHostBuilder => { });
    }
}
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("ConfigureWebHost cannot be used with WebApplicationBuilder.Host", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WebApplicationBuilder_WebHostWithConfigureWebHostInsideOtherMethod_ProducesDiagnostics_In_Program_Main()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            ./*MM*/ConfigureWebHost(webHostBuilder => { });
    }
}
public class Startup { }
");
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DoNotUseConfigureWebHostWithConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("ConfigureWebHost cannot be used with WebApplicationBuilder.Host", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task HostBuilder_ConfigureWebHost_DoesNotProduceDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.Hosting;
var builder = Host.CreateDefaultBuilder();
builder.ConfigureWebHost(webHostBuilder => { });
builder.ConfigureWebHost(webHostBuilder => { }, optionsBuilder => { });
";
        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
