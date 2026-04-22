// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.AspNetCore.Components.Testing.Generators;

namespace Microsoft.AspNetCore.Components.Testing.Tests.Generators;

public class ServiceOverrideAnalyzerTests
{
    [Fact]
    public async Task NoDiagnostics_WhenNoCallsites()
    {
        var source = """
            namespace TestApp
            {
                class MyTest
                {
                    void Setup() { }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostics_WhenMethodIsValid()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public static void FakeWeather(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>(nameof(TestOverrides.FakeWeather));
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostics_WhenMethodIsValidWithStringLiteral()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public static void FakeWeather(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>("FakeWeather");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NoDiagnostics_WhenValidNonGenericOverload()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public static void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices(typeof(TestOverrides), "Configure");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsE2E003_WhenMethodIsNotStatic()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public void FakeWeather(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>("FakeWeather");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("E2E003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("FakeWeather", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("must be static", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ReportsE2E003_WhenMethodIsNotStatic_NonGenericOverload()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices(typeof(TestOverrides), "Configure");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("E2E003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task ReportsE2E001_WhenMethodNotFound()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    // No FakeWeather method
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>("FakeWeather");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("E2E001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("FakeWeather", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("TestApp.TestOverrides", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ReportsE2E001_WhenMethodHasWrongSignature()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public static void FakeWeather(string notServices) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>("FakeWeather");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("E2E001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public async Task ReportsE2E002_WhenMethodNameNotConstant()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public static void FakeWeather(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var method = "FakeWeather";
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>(method);
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("E2E002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("compile-time constant", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task NoDiagnostics_WhenCallIsNotOnServerStartOptions()
    {
        var source = """
            namespace TestApp
            {
                class SomeOtherClass
                {
                    public void ConfigureServices<T>(string methodName) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var other = new SomeOtherClass();
                        other.ConfigureServices<MyTest>("NotARealMethod");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsE2E003_SpecificToNonStaticMethod_NotMethodNotFound()
    {
        // When the method exists with correct parameter but isn't static,
        // we should get E2E003 (must be static), NOT E2E001 (not found)
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>("Configure");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("E2E003", diagnostic.Id);
        // Confirm it's NOT E2E001
        Assert.NotEqual("E2E001", diagnostic.Id);
    }

    [Fact]
    public async Task ReportsE2E001_WhenStaticMethodExistsButWrongParameter()
    {
        // Static method exists but with wrong parameter type — should be E2E001
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    public static void FakeWeather(int count) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>("FakeWeather");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("E2E001", diagnostic.Id);
    }

    [Fact]
    public async Task DiagnosticLocationPointsToInvocation()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides { }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>("Missing");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.NotEqual(Location.None, diagnostic.Location);
        var span = diagnostic.Location.GetLineSpan();
        Assert.True(span.IsValid);
    }

    [Fact]
    public async Task NoDiagnostics_WhenPrivateStaticMethodWithCorrectSignature()
    {
        var source = """
            namespace TestApp
            {
                class TestOverrides
                {
                    static void FakeWeather(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
                }

                class MyTest
                {
                    void Setup()
                    {
                        var options = new Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions();
                        options.ConfigureServices<TestOverrides>("FakeWeather");
                    }
                }
            }
            """;

        var diagnostics = await GetAnalyzerDiagnosticsAsync(source);
        Assert.Empty(diagnostics);
    }

    static async Task<Diagnostic[]> GetAnalyzerDiagnosticsAsync(string userSource)
    {
        var compilation = StartupHookGeneratorTests.CreateCompilationWithInfrastructure(userSource);
        var analyzers = new DiagnosticAnalyzer[] { new ServiceOverrideAnalyzer() };
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers.ToImmutableArray());
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.ToArray();
    }
}
