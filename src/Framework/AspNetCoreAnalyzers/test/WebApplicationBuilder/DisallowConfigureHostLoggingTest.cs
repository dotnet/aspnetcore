// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;

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
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);
        //assert
        Assert.Empty(diagnostics);
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
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source);
        //assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WarnsWhenBuilderLoggingIsNotUsed_Host()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Host./*MM*/ConfigureLogging(logging =>
{
logging.AddJsonConsole();
});
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
    public async Task WarnsWhenBuilderLoggingIsNotUsed_WebHost()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost./*MM*/ConfigureLogging(logging =>
{
logging.AddJsonConsole();
});
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
    public async Task WarnsWhenBuilderLoggingIsNotUsed_OnDifferentLine_Host()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Host.
    /*MM*/ConfigureLogging(logging =>
{
    logging.AddJsonConsole();
});
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
    public async Task WarnsWhenBuilderLoggingIsNotUsed_OnDifferentLine_WebHost()
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
    /*MM*/ConfigureLogging(logging =>
{
logging.AddJsonConsole();
});
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
    public async Task WarnsWhenBuilderLoggingIsNotUsed_InMain_Host()
    {
        //arrange
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
public static class Program
{
    public static void Main (string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host./*MM*/ConfigureLogging(logging =>
        {
        logging.AddJsonConsole();
        });
    }
}
public class Startup { }
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
    public async Task WarnsWhenBuilderLoggingIsNotUsed_InMain_WebHost()
    {
        //arrange
        var source = TestSource.Read(@"
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
        builder.WebHost./*MM*/ConfigureLogging(logging =>
        {
        logging.AddJsonConsole();
        });
    }
}
public class Startup { }
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
        var source = TestSource.Read(@"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
var builder = WebApplication.CreateBuilder(args);
builder.Host./*MM1*/ConfigureLogging(logging =>
{
logging.AddJsonConsole();
});
builder.Host./*MM2*/ConfigureLogging(logging =>
{
logging.AddJsonConsole();
});
");
        //act
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        //assert
        Assert.Equal(2, diagnostics.Length);
        var diagnostic1 = diagnostics[0];
        var diagnostic2 = diagnostics[1];

        Assert.Same(DiagnosticDescriptors.DoNotUseHostConfigureLogging, diagnostic1.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic1.Location);
        Assert.Equal("Suggest using builder.Logging instead of ConfigureLogging", diagnostic1.GetMessage(CultureInfo.InvariantCulture));

        Assert.Same(DiagnosticDescriptors.DoNotUseHostConfigureLogging, diagnostic2.Descriptor);
        AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM2"], diagnostic2.Location);
        Assert.Equal("Suggest using builder.Logging instead of ConfigureLogging", diagnostic2.GetMessage(CultureInfo.InvariantCulture));
    }

}

