// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

            Analyses = new ConcurrentBag<StartupComputedAnalysis>();
            ConfigureServicesMethods = new ConcurrentBag<IMethodSymbol>();
            ConfigureMethods = new ConcurrentBag<IMethodSymbol>();
            StartupAnalyzer.AnalysisStarted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.ConfigureServicesMethodFound += (sender, method) => ConfigureServicesMethods.Add(method);
            StartupAnalyzer.ConfigureMethodFound += (sender, method) => ConfigureMethods.Add(method);
        }

        private StartupAnalzyer StartupAnalyzer { get; }

        private MvcDiagnosticAnalyzerRunner Runner { get; }

        private ConcurrentBag<StartupComputedAnalysis> Analyses { get; }

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
        public async Task StartupAnalyzer_MvcOptionsAnalysis_FindsEndpointRoutingDisabled()
        {
            // Arrange
            var source = ReadSource("MvcOptions_UseMvcAndEndpointRoutingDisabled");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var mvcOptionsAnalysis = Assert.Single(Analyses.OfType<MvcOptionsAnalysis>());
            Assert.False(mvcOptionsAnalysis.EndpointRoutingEnabled);

            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            var middleware = Assert.Single(middlewareAnalysis.Middleware);
            Assert.Equal("UseMvcWithDefaultRoute", middleware.UseMethod.Name);

            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task StartupAnalyzer_MvcOptionsAnalysis_FindsEndpointRoutingEnabled()
        {
            // Arrange
            var source = ReadSource("MvcOptions_UseMvcAndEndpointRoutingEnabled");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var mvcOptionsAnalysis = Assert.Single(Analyses.OfType<MvcOptionsAnalysis>());
            Assert.Null(mvcOptionsAnalysis.EndpointRoutingEnabled);

            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            var middleware = Assert.Single(middlewareAnalysis.Middleware);
            Assert.Equal("UseMvcWithDefaultRoute", middleware.UseMethod.Name);

            Assert.Collection(
                diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalzyer.UnsupportedUseMvcWithEndpointRouting, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                });
        }

        private TestSource ReadSource(string fileName)
        {
            return MvcTestSource.Read(nameof(StartupAnalyzerTest), fileName);
        }
    }
}
