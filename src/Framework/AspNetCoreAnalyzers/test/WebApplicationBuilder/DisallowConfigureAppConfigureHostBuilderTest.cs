// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.RouteHandlers.CSharpRouteHandlerCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer,
    Microsoft.AspNetCore.Analyzers.RouteHandlers.Fixers.DetectMismatchedParameterOptionalityFixer>;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;
public partial class DisallowConfigureAppConfigureHostBuilderTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new WebApplicationBuilderAnalyzer());
    /**
     * Verify that the correct code produces no diagnostic
     */ 
    [Fact]
    public async Task ConfigurationBuilderRunsWithoutDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJSonFile(fileName, optional: true);
";
        // Act
        var diagnostic = await Runner.GetDiagnosticsAsync(source);

        // Assert
        Assert.Empty(diagnostic); 
    }
    /**
     * Verify the fixed code and the diagnostic for builder.Host.ConfigureAppConfiguration
     */
    [Fact]
    public async Task ConfigureAppHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureAppConfiguration(builder =>
{
    builder.AddJsonFile(fileName, optional: true);
});
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(fileName, optional: true);
";
        // Act
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource); 
    }

    /**
     * Verify the fixed code and the diagnostic for builder.Host.ConfigureHostConfiguration
     */
    [Fact]
    public async Task ConfigureHostHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureHostConfiguration(builder =>
{
    builder.AddJsonFile(fileName, optional: true);
});
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(fileName, optional: true);
";
        // Act
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    /**
     * Verify the fixed code and the diagnostic for builder.WebHost.ConfigureAppConfiguration
     */
    [Fact]
    public async Task ConfigureAppWebHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureAppConfiguration(builder =>
{
    builder.AddJsonFile(fileName, optional: true);
});
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(fileName, optional: true);
";
        // Act
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }
}
