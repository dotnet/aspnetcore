// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

public class CompilationFeatureDetectorTest
{
    [Fact]
    public async Task DetectFeaturesAsync_FindsNoFeatures()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.CompilationFeatureDetectorTest
{
    public class StartupWithNoFeatures
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapFallbackToFile(""index.html"");
            });
        }
    }
}";
        var compilation = TestCompilation.Create(source);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName("StartupWithNoFeatures").Single();
        Assert.True(StartupFacts.IsStartupClass(symbols, type));

        // Act
        var features = await CompilationFeatureDetector.DetectFeaturesAsync(compilation);

        // Assert
        Assert.Empty(features);
    }

    [Fact]
    public async Task DetectFeatureAsync_StartupWithMapHub_FindsSignalR()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.CompilationFeatureDetectorTest
{
    public class StartupWithMapHub
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<MyHub>("" / test"");
            });
        }
    }

    public class MyHub : Hub
    {
    }
}
";
        var compilation = TestCompilation.Create(source);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName("StartupWithMapHub").Single();
        Assert.True(StartupFacts.IsStartupClass(symbols, type));

        // Act
        var features = await CompilationFeatureDetector.DetectFeaturesAsync(compilation);

        // Assert
        Assert.Collection(features, f => Assert.Equal(WellKnownFeatures.SignalR, f));

    }

    [Fact]
    public async Task DetectFeatureAsync_StartupWithMapBlazorHub_FindsSignalR()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.CompilationFeatureDetectorTest
{
    public class StartupWithMapBlazorHub
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
            });
        }

        public class App : Microsoft.AspNetCore.Components.ComponentBase
        {
        }
    }
}
";
        var compilation = TestCompilation.Create(source);
        var symbols = new StartupSymbols(compilation);

        var type = (INamedTypeSymbol)compilation.GetSymbolsWithName("StartupWithMapBlazorHub").Single();
        Assert.True(StartupFacts.IsStartupClass(symbols, type));

        // Act
        var features = await CompilationFeatureDetector.DetectFeaturesAsync(compilation);

        // Assert
        Assert.Collection(features, f => Assert.Equal(WellKnownFeatures.SignalR, f));

    }
}
