// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Razor;

public class RazorHotReloadTest
{
    [Fact]
    public void ClearCache_CanClearViewCompiler()
    {
        // Regression test for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1425693
        // Arrange
        var serviceProvider = GetServiceProvider();

        var compilerProvider = Assert.IsType<DefaultViewCompilerProvider>(serviceProvider.GetRequiredService<IViewCompilerProvider>());
        var hotReload = serviceProvider.GetRequiredService<RazorHotReload>();

        // Act
        hotReload.ClearCache(Type.EmptyTypes);

        // Assert
        Assert.Null(compilerProvider.Compiler.CompiledViews);
    }

    [Fact]
    public void ClearCache_ResetsViewEngineLookupCache()
    {
        // Arrange
        var serviceProvider = GetServiceProvider();

        var viewEngine = Assert.IsType<RazorViewEngine>(serviceProvider.GetRequiredService<IRazorViewEngine>());
        var hotReload = serviceProvider.GetRequiredService<RazorHotReload>();
        var lookup = viewEngine.ViewLookupCache;

        // Act
        hotReload.ClearCache(Type.EmptyTypes);

        // Assert
        Assert.NotSame(lookup, viewEngine.ViewLookupCache);
    }

    [Fact]
    public void ClearCache_ResetsRazorPageActivator()
    {
        // Arrange
        var serviceProvider = GetServiceProvider();

        var pageActivator = Assert.IsType<RazorPageActivator>(serviceProvider.GetRequiredService<IRazorPageActivator>());
        var hotReload = serviceProvider.GetRequiredService<RazorHotReload>();
        var cache = pageActivator.ActivationInfo;
        cache[new RazorPageActivator.CacheKey()] = new RazorPagePropertyActivator(
            typeof(string), typeof(object),
            new EmptyModelMetadataProvider(),
            new RazorPagePropertyActivator.PropertyValueAccessors());

        // Act
        Assert.Single(pageActivator.ActivationInfo);
        hotReload.ClearCache(Type.EmptyTypes);

        // Assert
        Assert.Empty(pageActivator.ActivationInfo);
    }

    private static ServiceProvider GetServiceProvider()
    {
        var diagnosticListener = new DiagnosticListener("Microsoft.AspNetCore");

        var serviceProvider = new ServiceCollection()
            .AddControllersWithViews()
            .Services
            // Manually add RazorHotReload because it's only added if MetadataUpdateHandler.IsSupported = true 
            .AddSingleton<RazorHotReload>()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton<DiagnosticSource>(diagnosticListener)
            .AddSingleton(diagnosticListener)
            .BuildServiceProvider();
        return serviceProvider;
    }
}
