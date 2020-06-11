// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers.TestFiles.CompilationFeatureDetectorTest;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Analyzers
{
    public class CompilationFeatureDetectorTest : AnalyzerTestBase
    {
        [Fact]
        public async Task DetectFeaturesAsync_FindsNoFeatures()
        {
            // Arrange
            var compilation = await CreateCompilationAsync(nameof(StartupWithNoFeatures));
            var symbols = new StartupSymbols(compilation);

            var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(nameof(StartupWithNoFeatures)).Single();
            Assert.True(StartupFacts.IsStartupClass(symbols, type));

            // Act
            var features = await CompilationFeatureDetector.DetectFeaturesAsync(compilation);

            // Assert
            Assert.Empty(features);
        }

        [Theory]
        [InlineData(nameof(StartupWithUseSignalR))]
        [InlineData(nameof(StartupWithMapHub))]
        [InlineData(nameof(StartupWithMapBlazorHub))]
        public async Task DetectFeaturesAsync_FindsSignalR(string source)
        {
            // Arrange
            var compilation = await CreateCompilationAsync(source);
            var symbols = new StartupSymbols(compilation);

            var type = (INamedTypeSymbol)compilation.GetSymbolsWithName(source).Single();
            Assert.True(StartupFacts.IsStartupClass(symbols, type));

            // Act
            var features = await CompilationFeatureDetector.DetectFeaturesAsync(compilation);

            // Assert
            Assert.Collection(features, f => Assert.Equal(WellKnownFeatures.SignalR, f));
        }
    }
}
