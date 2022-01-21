// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

public class DefaultRazorPageFactoryProviderTest
{
    [Fact]
    public void CreateFactory_ReturnsViewDescriptor_ForUnsuccessfulResults()
    {
        // Arrange
        var path = "/file-does-not-exist";
        var expirationTokens = new[]
        {
                Mock.Of<IChangeToken>(),
                Mock.Of<IChangeToken>(),
            };
        var descriptor = new CompiledViewDescriptor
        {
            RelativePath = path,
            ExpirationTokens = expirationTokens,
        };
        var compilerCache = new Mock<IViewCompiler>();
        compilerCache
            .Setup(f => f.CompileAsync(It.IsAny<string>()))
            .ReturnsAsync(descriptor);

        var factoryProvider = new DefaultRazorPageFactoryProvider(GetCompilerProvider(compilerCache.Object));

        // Act
        var result = factoryProvider.CreateFactory(path);

        // Assert
        Assert.False(result.Success);
        Assert.Same(descriptor, result.ViewDescriptor);
    }

    [Fact]
    public void CreateFactory_ReturnsViewDescriptor_ForSuccessfulResults()
    {
        // Arrange
        var relativePath = "/file-exists";
        var expirationTokens = new[]
        {
                Mock.Of<IChangeToken>(),
                Mock.Of<IChangeToken>(),
            };
        var descriptor = new CompiledViewDescriptor
        {
            RelativePath = relativePath,
            Item = TestRazorCompiledItem.CreateForView(typeof(TestRazorPage), relativePath),
            ExpirationTokens = expirationTokens,
        };
        var compilerCache = new Mock<IViewCompiler>();
        compilerCache
            .Setup(f => f.CompileAsync(It.IsAny<string>()))
            .ReturnsAsync(descriptor);

        var factoryProvider = new DefaultRazorPageFactoryProvider(GetCompilerProvider(compilerCache.Object));

        // Act
        var result = factoryProvider.CreateFactory(relativePath);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expirationTokens, descriptor.ExpirationTokens);
    }

    [Fact]
    public void CreateFactory_ProducesDelegateThatSetsPagePath()
    {
        // Arrange
        var relativePath = "/file-exists";
        var descriptor = new CompiledViewDescriptor
        {
            RelativePath = relativePath,
            Item = TestRazorCompiledItem.CreateForView(typeof(TestRazorPage), relativePath),
            ExpirationTokens = Array.Empty<IChangeToken>(),
        };
        var viewCompiler = new Mock<IViewCompiler>();
        viewCompiler
            .Setup(f => f.CompileAsync(It.IsAny<string>()))
            .ReturnsAsync(descriptor);

        var factoryProvider = new DefaultRazorPageFactoryProvider(GetCompilerProvider(viewCompiler.Object));

        // Act
        var result = factoryProvider.CreateFactory(relativePath);

        // Assert
        Assert.True(result.Success);
        var actual = result.RazorPageFactory();
        Assert.Equal("/file-exists", actual.Path);
    }

    private IViewCompilerProvider GetCompilerProvider(IViewCompiler cache)
    {
        var compilerCacheProvider = new Mock<IViewCompilerProvider>();
        compilerCacheProvider
            .Setup(c => c.GetCompiler())
            .Returns(cache);

        return compilerCacheProvider.Object;
    }

    private class TestRazorPage : RazorPage
    {
        public override Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
