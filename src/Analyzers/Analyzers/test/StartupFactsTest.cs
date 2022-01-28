// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

public class StartupFactsTest
{
    private const string BasicStartup = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupFactsTest
{
    public class BasicStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}";
    private const string EnvironmentStartup = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupFactsTest
{
    public class EnvironmentStartup
    {
        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
        }

        public void configurePRODUCTIONservices(IServiceCollection services)
        {
        }

        // Yes, this is technically a Configure method - if you have an Enviroment called DevelopmentServices2.
        public static void ConfigureDevelopmentServices2(IConfiguration configuration, ILogger logger, IApplicationBuilder app)
        {
        }

        public static void configurePRODUCTION(IConfiguration configuration, ILogger logger, IApplicationBuilder app)
        {
        }
    }
}
";
    private const string NotAStartupClass = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupFactsTest
{
    public class NotAStartupClass
    {
        // no args - not a ConfigureServices (technically it is, but we exclude this case).
        public void ConfigureServices()
        {
        }

        // extra arg - not a ConfigureServices
        public void ConfigureServices(IServiceCollection services, string x)
        {
        }

        // wrong name - not a ConfigureServices
        public void ConfigureSrvces(IServiceCollection services)
        {
        }

        // non-public - not a ConfigureServices
        internal void ConfigureServices(IServiceCollection services)
        {
        }

        // no IApplicationBuilder - not a Configure
        public void Configure(IConfiguration configuration)
        {
        }

        // wrong prefix - not a Configure
        public void Configur(IApplicationBuilder app)
        {
        }

        // non-public - not a Configure
        internal void Configure(IApplicationBuilder app)
        {
        }
    }
}";
    private static readonly Dictionary<string, string> TestSources = new Dictionary<string, string>
    {
        [nameof(BasicStartup)] = BasicStartup,
        [nameof(EnvironmentStartup)] = EnvironmentStartup,
        [nameof(NotAStartupClass)] = NotAStartupClass,
    };

    [Theory]
    [InlineData(nameof(BasicStartup), "ConfigureServices")]
    [InlineData(nameof(EnvironmentStartup), "ConfigureDevelopmentServices")]
    [InlineData(nameof(EnvironmentStartup), "configurePRODUCTIONservices")]
    public void IsConfigureServices_FindsConfigureServicesMethod(string source, string methodName)
    {
        // Arrange
        var compilation = TestCompilation.Create(TestSources[source]);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();
        var methods = type.GetMembers(methodName).Cast<IMethodSymbol>();

        foreach (var method in methods)
        {
            // Act
            var result = StartupFacts.IsConfigureServices(symbols, method);

            // Assert
            Assert.True(result);
        }
    }

    [Theory]
    [InlineData(nameof(NotAStartupClass), "ConfigureServices")]
    [InlineData(nameof(NotAStartupClass), "ConfigureSrvces")]

    // This is an interesting case where a method follows both naming conventions.
    [InlineData(nameof(EnvironmentStartup), "ConfigureDevelopmentServices2")]
    public void IsConfigureServices_RejectsNonConfigureServicesMethod(string source, string methodName)
    {
        // Arrange
        var compilation = TestCompilation.Create(TestSources[source]);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();
        var methods = type.GetMembers(methodName).Cast<IMethodSymbol>();

        foreach (var method in methods)
        {
            // Act
            var result = StartupFacts.IsConfigureServices(symbols, method);

            // Assert
            Assert.False(result);
        }
    }

    [Theory]
    [InlineData(nameof(BasicStartup), "Configure")]
    [InlineData(nameof(EnvironmentStartup), "configurePRODUCTION")]
    [InlineData(nameof(EnvironmentStartup), "ConfigureDevelopmentServices2")]
    public void IsConfigure_FindsConfigureMethod(string source, string methodName)
    {
        // Arrange
        var compilation = TestCompilation.Create(TestSources[source]);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();
        var methods = type.GetMembers(methodName).Cast<IMethodSymbol>();

        foreach (var method in methods)
        {
            // Act
            var result = StartupFacts.IsConfigure(symbols, method);

            // Assert
            Assert.True(result);
        }
    }

    [Theory]
    [InlineData(nameof(NotAStartupClass), "Configure")]
    [InlineData(nameof(NotAStartupClass), "Configur")]
    public void IsConfigure_RejectsNonConfigureMethod(string source, string methodName)
    {
        // Arrange
        var compilation = TestCompilation.Create(TestSources[source]);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();
        var methods = type.GetMembers(methodName).Cast<IMethodSymbol>();

        foreach (var method in methods)
        {
            // Act
            var result = StartupFacts.IsConfigure(symbols, method);

            // Assert
            Assert.False(result);
        }
    }

    [Theory]
    [InlineData(nameof(BasicStartup))]
    [InlineData(nameof(EnvironmentStartup))]
    public void IsStartupClass_FindsStartupClass(string source)
    {
        // Arrange
        var compilation = TestCompilation.Create(TestSources[source]);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();

        // Act
        var result = StartupFacts.IsStartupClass(symbols, type);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(nameof(NotAStartupClass))]
    public void IsStartupClass_RejectsNotStartupClass(string source)
    {
        // Arrange
        var compilation = TestCompilation.Create(TestSources[source]);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();

        // Act
        var result = StartupFacts.IsStartupClass(symbols, type);

        // Assert
        Assert.False(result);
    }
}
