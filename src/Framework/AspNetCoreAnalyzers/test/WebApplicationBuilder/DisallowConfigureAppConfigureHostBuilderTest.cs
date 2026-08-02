// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.WebApplicationBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers.WebApplicationBuilderFixer>;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder;

public partial class DisallowConfigureAppConfigureHostBuilderTest
{
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

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task ConfigureAppHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Host.{|#0:ConfigureAppConfiguration(builder => builder.AddJsonFile(""foo.json"", optional: true))|};
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(""foo.json"", optional: true);
";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureAppConfiguration").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task ConfigureHostHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Host.{|#0:ConfigureHostConfiguration(builder => builder.AddJsonFile(""foo.json"", optional: true))|};
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(""foo.json"", optional: true);
";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureHostConfiguration").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task ConfigureAppWebHostBuilderProducesDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.{|#0:ConfigureAppConfiguration(builder => builder.AddJsonFile(""foo.json"", optional: true))|};
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(""foo.json"", optional: true);
";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureAppConfiguration").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task ConfigureAppWebHostBuilderProducesDiagnosticInMain()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
public static class Test
{
    public static void Main(string[]args) {
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.{|#0:ConfigureAppConfiguration(builder => builder.AddJsonFile(""foo.json"", optional: true))|};
    }
}
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
public static class Test
{
    public static void Main(string[]args) {
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile(""foo.json"", optional: true);
    }
}
";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureAppConfiguration").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task TwoInvocationsProduceTwoDiagnostic()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Host.{|#0:ConfigureHostConfiguration(builder => builder.AddJsonFile(""foo.json"", optional: true))|};
builder.WebHost.{|#1:ConfigureAppConfiguration(builder => builder.AddJsonFile(""foo.json"", optional: true))|};
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(""foo.json"", optional: true);
builder.Configuration.AddJsonFile(""foo.json"", optional: true);
";

        var expectedDiagnostic = new[] {
            new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureHostConfiguration").WithLocation(0),
            new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureAppConfiguration").WithLocation(1)
        };

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task TwoMethodsInArgumentsProducesTwoProperties()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Host.{|#0:ConfigureAppConfiguration((context, builder) => { builder.AddJsonFile(""foo.json"", optional: true); builder.AddEnvironmentVariables();})|};
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(""foo.json"", optional: true).AddEnvironmentVariables();
";

        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureAppConfiguration").WithLocation(0);

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnostic, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenConfigureAppConfigurationIsCalledWhenChainedWithCreateBuilder()
    {
        //arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
WebApplication.CreateBuilder(args).Host.{|#0:ConfigureAppConfiguration((context, builder) => builder.AddJsonFile(""foo.json"", optional: true))|};
";
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
WebApplication.CreateBuilder(args).Configuration.AddJsonFile(""foo.json"", optional: true);
";
        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureAppConfiguration").WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }

    [Fact]
    public async Task WarnsWhenConfigureAppConfigurationIsCalledAsAnArgument()
    {
        //arrange
        var source = @"
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(builder.Host.{|#0:ConfigureAppConfiguration((context, builder) => builder.AddJsonFile(""foo.json"", optional: true))|});
";
        var fixedSource = @"
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
var builder = WebApplication.CreateBuilder(args);
Console.WriteLine(builder.Configuration.AddJsonFile(""foo.json"", optional: true));
";
        var expectedDiagnosis = new DiagnosticResult(DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder).WithArguments("ConfigureAppConfiguration").WithLocation(0);
        await VerifyCS.VerifyCodeFixAsync(source, expectedDiagnosis, fixedSource);
    }
}
