// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;
public partial class DisallowConfigureAppConfigureHostBuilderTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());

    [Fact]
    public async Task ConfigurationBuilderRunsWithoutDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(""foo.json"", optional: true);
";
        // Act
        var diagnostic = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostic);
    }

    [Fact]
    public async Task ConfigureAppHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Host./*MM*/ConfigureAppConfiguration(builder =>
{
    builder.AddJsonFile(""foo.json"", optional: true);
});
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using WebApplicationBuilder.Configuration instead of ConfigureAppConfiguration", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ConfigureHostHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Host./*MM*/ConfigureHostConfiguration(builder =>
{
    builder.AddJsonFile(""foo.json"", optional: true);
});
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using WebApplicationBuilder.Configuration instead of ConfigureHostConfiguration", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ConfigureAppWebHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost./*MM*/ConfigureAppConfiguration(builder =>
{
    builder.AddJsonFile(""foo.json"", optional: true);
});
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using WebApplicationBuilder.Configuration instead of ConfigureAppConfiguration", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ConfigureAppWebHostBuilderWithContextProducesDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost./*MM*/ConfigureAppConfiguration((context, webHostBuilder) => { });
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using WebApplicationBuilder.Configuration instead of ConfigureAppConfiguration", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }
    [Fact]
    public async Task ConfigureAppWebHostBuilderProducesDiagnosticInMain()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
public static class Test
{
    public static void Main(string[]args) {
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost./*MM*/ConfigureAppConfiguration(builder => { });
}
}
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using WebApplicationBuilder.Configuration instead of ConfigureAppConfiguration", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }
    [Fact]
    public async Task ConfigureAppWebHostOnBuilderProducesDiagnosticInMain()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
public static class Test
{
    public static void Main(string[]args) {
    var builder = WebApplication.CreateBuilder(args);
    var webhost = builder.WebHost;
    webhost./*MM*/ConfigureAppConfiguration(builder => { });
}
}
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder, diagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
        Assert.Equal("Suggest using WebApplicationBuilder.Configuration instead of ConfigureAppConfiguration", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }
    [Fact]
    public async Task TwoInvocationsProduceTwoDiagnostic()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Host./*MM1*/ConfigureHostConfiguration(builder =>
{
    builder.AddJsonFile(""foo.json"", optional: true);
});
builder.WebHost./*MM2*/ConfigureAppConfiguration(builder =>
{
    builder.AddJsonFile(""foo.json"", optional: true);
});
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Assert
        Assert.Equal(2, diagnostics.Length);
        var diagnostic1 = diagnostics[0];
        var diagnostic2 = diagnostics[1];
        Assert.Same(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder, diagnostic1.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic1.Location);
        Assert.Equal("Suggest using WebApplicationBuilder.Configuration instead of ConfigureHostConfiguration", diagnostic1.GetMessage(CultureInfo.InvariantCulture));
        Assert.Same(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder, diagnostic2.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM2"], diagnostic2.Location);
        Assert.Equal("Suggest using WebApplicationBuilder.Configuration instead of ConfigureAppConfiguration", diagnostic2.GetMessage(CultureInfo.InvariantCulture));
    }
}
