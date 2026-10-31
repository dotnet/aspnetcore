// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

public class DefaultViewCompilerTest
{
    [Fact]
    public async Task CompileAsync_ReturnsResultWithNullAttribute_IfFileIsNotFoundInFileSystem()
    {
        // Arrange
        var path = "/file/does-not-exist";
        var viewCompiler = GetViewCompiler();

        // Act
        var result1 = await viewCompiler.CompileAsync(path);
        var result2 = await viewCompiler.CompileAsync(path);

        // Assert
        Assert.Empty(result1.ExpirationTokens);
    }

    [Fact]
    public async Task CompileAsync_ReturnsCompiledViews()
    {
        // Arrange
        var path = "/Views/Home/Index.cshtml";
        var compiledView = new CompiledViewDescriptor
        {
            RelativePath = path,
        };
        var viewCompiler = GetViewCompiler(compiledViews: new[] { compiledView });

        // Act
        var result = await viewCompiler.CompileAsync(path);

        // Assert
        Assert.Same(compiledView, result);

        // This view doesn't have checksums so it can't be recompiled.
        Assert.Null(compiledView.ExpirationTokens);
    }

    [Theory]
    [InlineData("/views/home/index.cshtml")]
    [InlineData("/VIEWS/HOME/INDEX.CSHTML")]
    [InlineData("/viEws/HoME/inDex.cshtml")]
    public async Task CompileAsync_PerformsCaseInsensitiveLookupsForCompiledViews(string lookupPath)
    {
        // Arrange
        var path = "/Views/Home/Index.cshtml";
        var precompiledView = new CompiledViewDescriptor
        {
            RelativePath = path,
        };
        var viewCompiler = GetViewCompiler(compiledViews: new[] { precompiledView });

        // Act
        var result = await viewCompiler.CompileAsync(lookupPath);

        // Assert
        Assert.Same(precompiledView, result);
    }

    [Fact]
    public async Task CompileAsync_PerformsCaseInsensitiveLookupsForCompiledViews_WithNonNormalizedPaths()
    {
        // Arrange
        var path = "/Views/Home/Index.cshtml";
        var compiledView = new CompiledViewDescriptor
        {
            RelativePath = path,
        };
        var viewCompiler = GetViewCompiler(compiledViews: new[] { compiledView });

        // Act
        var result = await viewCompiler.CompileAsync("Views\\Home\\Index.cshtml");

        // Assert
        Assert.Same(compiledView, result);
    }

    [Fact]
    public async Task CompileAsync_DiscoversHotReloadedTypes()
    {
        // Arrange
        var path = "/Views/Home/Index.cshtml";
        var compiledView = new CompiledViewDescriptor
        {
            RelativePath = path,
        };
        var compiledViews = new List<CompiledViewDescriptor>
            {
                compiledView,
            };
        var viewCompiler = GetViewCompiler(compiledViews);

        // Act - 1
        var result = await viewCompiler.CompileAsync(path);

        // Assert - 1
        Assert.Same(compiledView, result);

        // Act - 2
        var hotReloaded = new CompiledViewDescriptor { RelativePath = path };
        compiledViews[0] = hotReloaded;
        viewCompiler.ClearCache();

        result = await viewCompiler.CompileAsync(path);

        // Assert - 2
        Assert.Same(hotReloaded, result);
    }

    private static TestRazorViewCompiler GetViewCompiler(IList<CompiledViewDescriptor> compiledViews = null)
    {
        compiledViews = compiledViews ?? Array.Empty<CompiledViewDescriptor>();

        var viewCompiler = new TestRazorViewCompiler(compiledViews);
        return viewCompiler;
    }

    private class TestRazorViewCompiler : DefaultViewCompiler
    {
        public TestRazorViewCompiler(IList<CompiledViewDescriptor> compiledViews) :
            base(GetApplicationPartManager(new TestViewsFeatureProvider { CompiledViews = compiledViews }), NullLogger<DefaultViewCompiler>.Instance)
        {
        }

        public TestRazorViewCompiler(TestViewsFeatureProvider featureProvider) :
           base(GetApplicationPartManager(featureProvider), NullLogger<DefaultViewCompiler>.Instance)
        {
        }

        private static ApplicationPartManager GetApplicationPartManager(TestViewsFeatureProvider featureProvider)
        {
            var manager = new ApplicationPartManager();
            manager.FeatureProviders.Add(featureProvider);

            return manager;
        }
    }

    private class TestViewsFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        public IList<CompiledViewDescriptor> CompiledViews { get; init; }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
        {
            foreach (var item in CompiledViews)
            {
                feature.ViewDescriptors.Add(item);
            }
        }
    }
}
