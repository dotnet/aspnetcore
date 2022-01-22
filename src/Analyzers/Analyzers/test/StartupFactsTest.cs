// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers.TestFiles.StartupFactsTest;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Analyzers
{
    public class StartupFactsTest : AnalyzerTestBase
    {
        [Theory]
        [InlineData(nameof(BasicStartup), nameof(BasicStartup.ConfigureServices))]
        [InlineData(nameof(EnvironmentStartup), nameof(EnvironmentStartup.ConfigureDevelopmentServices))]
        [InlineData(nameof(EnvironmentStartup), nameof(EnvironmentStartup.configurePRODUCTIONservices))]
        public async Task IsConfigureServices_FindsConfigureServicesMethod(string source, string methodName)
        {
            // Arrange
            var compilation = await CreateCompilationAsync(source);
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
        [InlineData(nameof(NotAStartupClass), nameof(NotAStartupClass.ConfigureServices))]
        [InlineData(nameof(NotAStartupClass), nameof(NotAStartupClass.ConfigureSrvces))]

        // This is an interesting case where a method follows both naming conventions.
        [InlineData(nameof(EnvironmentStartup), nameof(EnvironmentStartup.ConfigureDevelopmentServices2))]
        public async Task IsConfigureServices_RejectsNonConfigureServicesMethod(string source, string methodName)
        {
            // Arrange
            var compilation = await CreateCompilationAsync(source);
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
        [InlineData(nameof(BasicStartup), nameof(BasicStartup.Configure))]
        [InlineData(nameof(EnvironmentStartup), nameof(EnvironmentStartup.configurePRODUCTION))]
        [InlineData(nameof(EnvironmentStartup), nameof(EnvironmentStartup.ConfigureDevelopmentServices2))]
        public async Task IsConfigure_FindsConfigureMethod(string source, string methodName)
        {
            // Arrange
            var compilation = await CreateCompilationAsync(source);
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
        [InlineData(nameof(NotAStartupClass), nameof(NotAStartupClass.Configure))]
        [InlineData(nameof(NotAStartupClass), nameof(NotAStartupClass.Configur))]
        public async Task IsConfigure_RejectsNonConfigureMethod(string source, string methodName)
        {
            // Arrange
            var compilation = await CreateCompilationAsync(source);
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
        public async Task IsStartupClass_FindsStartupClass(string source)
        {
            // Arrange
            var compilation = await CreateCompilationAsync(source);
            var symbols = new StartupSymbols(compilation);

            var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();

            // Act
            var result = StartupFacts.IsStartupClass(symbols, type);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(nameof(NotAStartupClass))]
        public async Task IsStartupClass_RejectsNotStartupClass(string source)
        {
            // Arrange
            var compilation = await CreateCompilationAsync(source);
            var symbols = new StartupSymbols(compilation);

            var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();

            // Act
            var result = StartupFacts.IsStartupClass(symbols, type);

            // Assert
            Assert.False(result);
        }
    }
}
