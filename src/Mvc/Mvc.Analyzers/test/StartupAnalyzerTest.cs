// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Analyzers
{
    public class StartupAnalyzerTest
    {
        public StartupAnalyzerTest()
        {
            StartupAnalyzer = new StartupAnalzyer();

            Runner = new MvcDiagnosticAnalyzerRunner(StartupAnalyzer);

            Analyses = new ConcurrentBag<object>();
            ConfigureServicesMethods = new ConcurrentBag<IMethodSymbol>();
            ConfigureMethods = new ConcurrentBag<IMethodSymbol>();
            StartupAnalyzer.ServicesAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.OptionsAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.MiddlewareAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.ConfigureServicesMethodFound += (sender, method) => ConfigureServicesMethods.Add(method);
            StartupAnalyzer.ConfigureMethodFound += (sender, method) => ConfigureMethods.Add(method);
        }

        private StartupAnalzyer StartupAnalyzer { get; }

        private MvcDiagnosticAnalyzerRunner Runner { get; }

        private ConcurrentBag<object> Analyses { get; }

        private ConcurrentBag<IMethodSymbol> ConfigureServicesMethods { get; }

        private ConcurrentBag<IMethodSymbol> ConfigureMethods { get; }

        [Fact]
        public async Task StartupAnalyzer_FindsStartupMethods_StartupSignatures_Standard()
        {
            // Arrange
            var source = ReadSource("StartupSignatures_Standard");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            Assert.Empty(diagnostics);

            Assert.Collection(ConfigureServicesMethods, m => Assert.Equal("ConfigureServices", m.Name));
            Assert.Collection(ConfigureMethods, m => Assert.Equal("Configure", m.Name));
        }

        [Fact]
        public async Task StartupAnalyzer_FindsStartupMethods_StartupSignatures_MoreVariety()
        {
            // Arrange
            var source = ReadSource("StartupSignatures_MoreVariety");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            Assert.Empty(diagnostics);

            Assert.Collection(
                ConfigureServicesMethods.OrderBy(m => m.Name),
                m => Assert.Equal("ConfigureServices", m.Name));

            Assert.Collection(
                ConfigureMethods.OrderBy(m => m.Name),
                m => Assert.Equal("Configure", m.Name),
                m => Assert.Equal("ConfigureProduction", m.Name));
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_UseMvc_FindsEndpointRoutingDisabled()
        {
            // Arrange
            var source = ReadSource("MvcOptions_UseMvcWithDefaultRouteAndEndpointRoutingDisabled");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
            Assert.True(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            var middleware = Assert.Single(middlewareAnalysis.Middleware);
            Assert.Equal("UseMvcWithDefaultRoute", middleware.UseMethod.Name);

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_AddMvcOptions_FindsEndpointRoutingDisabled()
        {
            // Arrange
            var source = ReadSource("MvcOptions_UseMvcWithDefaultRouteAndAddMvcOptionsEndpointRoutingDisabled");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
            Assert.True(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            var middleware = Assert.Single(middlewareAnalysis.Middleware);
            Assert.Equal("UseMvcWithDefaultRoute", middleware.UseMethod.Name);

            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData("MvcOptions_UseMvc", "UseMvc")]
        [InlineData("MvcOptions_UseMvcAndConfiguredRoutes", "UseMvc")]
        [InlineData("MvcOptions_UseMvcWithDefaultRoute", "UseMvcWithDefaultRoute")]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_FindsEndpointRoutingEnabled(string sourceFileName, string mvcMiddlewareName)
        {
            // Arrange
            var source = ReadSource(sourceFileName);

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
            Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            var middleware = Assert.Single(middlewareAnalysis.Middleware);
            Assert.Equal(mvcMiddlewareName, middleware.UseMethod.Name);

            Assert.Collection(
                diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalzyer.Diagnostics.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                });
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_MultipleMiddleware()
        {
            // Arrange
            var source = ReadSource("MvcOptions_UseMvcWithOtherMiddleware");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
            Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());

            Assert.Collection(
                middlewareAnalysis.Middleware,
                item => Assert.Equal("UseAuthorization", item.UseMethod.Name),
                item => Assert.Equal("UseMiddleware", item.UseMethod.Name),
                item => Assert.Equal("UseMvc", item.UseMethod.Name),
                item => Assert.Equal("UseRouting", item.UseMethod.Name),
                item => Assert.Equal("UseEndpoints", item.UseMethod.Name));

            Assert.Collection(
                diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalzyer.Diagnostics.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                });
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_MultipleUseMvc()
        {
            // Arrange
            var source = ReadSource("MvcOptions_UseMvcMultiple");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var optionsAnalysis = Assert.Single(Analyses.OfType<OptionsAnalysis>());
            Assert.False(OptionsFacts.IsEndpointRoutingExplicitlyDisabled(optionsAnalysis));

            Assert.Collection(
                diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalzyer.Diagnostics.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic.Location);
                },
                diagnostic =>
                {
                    Assert.Same(StartupAnalzyer.Diagnostics.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM2"], diagnostic.Location);
                },
                diagnostic =>
                {
                    Assert.Same(StartupAnalzyer.Diagnostics.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM3"], diagnostic.Location);
                });
        }

        [Fact]
        public async Task StartupAnalyzer_ServicesAnalysis_CallBuildServiceProvider()
        {
            // Arrange
            var source = ReadSource("ConfigureServices_BuildServiceProvider");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var servicesAnalysis = Assert.Single(Analyses.OfType<ServicesAnalysis>());
            Assert.NotEmpty(servicesAnalysis.Services);
            Assert.Collection(diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalzyer.Diagnostics.BuildServiceProviderShouldNotCalledInConfigureServicesMethod, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.MarkerLocations["MM1"], diagnostic.Location);
                });
        }

        private TestSource ReadSource(string fileName)
        {
            return MvcTestSource.Read(nameof(StartupAnalyzerTest), fileName);
        }
    }
}
