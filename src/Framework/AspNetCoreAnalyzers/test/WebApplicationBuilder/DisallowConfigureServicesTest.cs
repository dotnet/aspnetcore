// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;

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

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WarnsWhenBuilderHostConfigureServicesIsUsed()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Host./*MM*/ConfigureServices(services =>
{
services.AddAntiforgery();
});
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
    public async Task WarnsWhenBuilderWebHostConfigureServicesIsUsed()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost./*MM*/ConfigureServices(services =>
{
services.AddAntiforgery();
});
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
    public async Task WarnsWhenBuilderHostConfigureServicesIsUsed_OnDifferentLine()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Host.
    /*MM*/ConfigureServices(services =>
    {
    services.AddAntiforgery();
    });
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
    public async Task WarnsWhenBuilderWebHostConfigureServicesIsUsed_OnDifferentLine()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.
    /*MM*/ConfigureServices(services =>
    {
    services.AddAntiforgery();
    });
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
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WarnsWhenBuilderHostConfigureServicesIsUsedOnProperty_In_Program_Main()
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
        builder.Host./*MM*/ConfigureServices(services =>
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
    public async Task WarnsWhenBuilderWebHostConfigureServicesIsUsedOnProperty_In_Program_Main()
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
        builder.WebHost./*MM*/ConfigureServices(services =>
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
    public async Task WarnsWhenBuilderHostConfigureServicesIsUsed_In_Program_Main()
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
        var host = builder.Host;
        host./*MM*/ConfigureServices(services =>
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
    public async Task WarnsWhenBuilderWebHostConfigureServicesIsUsed_In_Program_Main()
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
        var webHost = builder.WebHost;
        webHost./*MM*/ConfigureServices(services =>
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
    public async Task WarnsTwiceWhenBuilderHostConfigureServicesIsUsed_Twice()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Host./*MM1*/ConfigureServices(services =>
{
    services.AddAntiforgery();
});
builder.Host./*MM2*/ConfigureServices(services =>
{
    services.AddAntiforgery();
});
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Asserts
        Assert.Equal(2, diagnostics.Length);

        // First diagnostic
        var firstDiagnostic = diagnostics[0];

        Assert.Same(DiagnosticDescriptors.DoNotUseHostConfigureServices, firstDiagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], firstDiagnostic.Location);
        Assert.Equal("Suggest using builder.Services instead of ConfigureServices", firstDiagnostic.GetMessage(CultureInfo.InvariantCulture));

        // Second diagnostic
        var secondDiagnostic = diagnostics[1];

        Assert.Same(DiagnosticDescriptors.DoNotUseHostConfigureServices, secondDiagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM2"], secondDiagnostic.Location);
        Assert.Equal("Suggest using builder.Services instead of ConfigureServices", secondDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task WarnsTwiceWhenBuilderWebHostConfigureServicesIsUsed_Twice()
    {
        // Arrange
        var source = TestSource.Read(@"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost./*MM1*/ConfigureServices(services =>
{
    services.AddAntiforgery();
});
builder.WebHost./*MM2*/ConfigureServices(services =>
{
    services.AddAntiforgery();
});
");

        // Act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

        // Asserts
        Assert.Equal(2, diagnostics.Length);

        // First diagnostic
        var firstDiagnostic = diagnostics[0];

        Assert.Same(DiagnosticDescriptors.DoNotUseHostConfigureServices, firstDiagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], firstDiagnostic.Location);
        Assert.Equal("Suggest using builder.Services instead of ConfigureServices", firstDiagnostic.GetMessage(CultureInfo.InvariantCulture));

        // Second diagnostic
        var secondDiagnostic = diagnostics[1];

        Assert.Same(DiagnosticDescriptors.DoNotUseHostConfigureServices, secondDiagnostic.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM2"], secondDiagnostic.Location);
        Assert.Equal("Suggest using builder.Services instead of ConfigureServices", secondDiagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

}
