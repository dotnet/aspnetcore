// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    public class MinimalStartupTest : AnalyzerTestBase
    {
        public MinimalStartupTest()
        {
            StartupAnalyzer = new StartupAnalyzer();

            Runner = new AnalyzersDiagnosticAnalyzerRunner(StartupAnalyzer, OutputKind.ConsoleApplication);

            Analyses = new ConcurrentBag<object>();
            ConfigureServicesMethods = new ConcurrentBag<IMethodSymbol>();
            ConfigureMethods = new ConcurrentBag<IMethodSymbol>();
            StartupAnalyzer.ServicesAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.OptionsAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.MiddlewareAnalysisCompleted += (sender, analysis) => Analyses.Add(analysis);
            StartupAnalyzer.ConfigureServicesMethodFound += (sender, method) => ConfigureServicesMethods.Add(method);
            StartupAnalyzer.ConfigureMethodFound += (sender, method) => ConfigureMethods.Add(method);
        }

        private StartupAnalyzer StartupAnalyzer { get; }

        private AnalyzersDiagnosticAnalyzerRunner Runner { get; }

        private ConcurrentBag<object> Analyses { get; }

        private ConcurrentBag<IMethodSymbol> ConfigureServicesMethods { get; }

        private ConcurrentBag<IMethodSymbol> ConfigureMethods { get; }

        [Fact]
        public async Task StartupAnalyzer_FindsStartupMethods_StartupSignatures_WebApplicationBuilder()
        {
            // Arrange
            var source = Read("StartupSignatures_WebApplicationBuilder");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            Assert.Empty(diagnostics);

            //Assert.Collection(ConfigureServicesMethods, m => Assert.Equal("ConfigureServices", m.Name));
            //Assert.Collection(ConfigureMethods, m => Assert.Equal("Configure", m.Name));
        }

        [Fact]
        public async Task M()
        {
            // Arrange
            var source = Read("UseAuthAfterUseEndpoints");

            // Act
            var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);

            // Assert
            var middlewareAnalysis = Assert.Single(Analyses.OfType<MiddlewareAnalysis>());
            Assert.NotEmpty(middlewareAnalysis.Middleware);
            Assert.Collection(diagnostics,
                diagnostic =>
                {
                    Assert.Same(StartupAnalyzer.Diagnostics.IncorrectlyConfiguredAuthorizationMiddleware, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(source.DefaultMarkerLocation, diagnostic.Location);
                });
        }
    }
}
