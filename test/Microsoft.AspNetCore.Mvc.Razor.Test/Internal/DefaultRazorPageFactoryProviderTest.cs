// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DefaultRazorPageFactoryProviderTest
    {
        [Fact]
        public void CreateFactory_ReturnsExpirationTokensFromCompilerCache_ForUnsuccessfulResults()
        {
            // Arrange
            var expirationTokens = new[]
            {
                Mock.Of<IChangeToken>(),
                Mock.Of<IChangeToken>(),
            };
            var compilerCache = new Mock<ICompilerCache>();
            compilerCache
                .Setup(f => f.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<RelativeFileInfo, CompilationResult>>()))
                .Returns(new CompilerCacheResult(expirationTokens));
            var compilerCacheProvider = new Mock<ICompilerCacheProvider>();
            compilerCacheProvider
                .SetupGet(c => c.Cache)
                .Returns(compilerCache.Object);
            var factoryProvider = new DefaultRazorPageFactoryProvider(
                Mock.Of<IRazorCompilationService>(),
                compilerCacheProvider.Object);

            // Act
            var result = factoryProvider.CreateFactory("/file-does-not-exist");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(expirationTokens, result.ExpirationTokens);
        }

        [Fact]
        public void CreateFactory_ReturnsExpirationTokensFromCompilerCache_ForSuccessfulResults()
        {
            // Arrange
            var relativePath = "/file-exists";
            var expirationTokens = new[]
            {
                Mock.Of<IChangeToken>(),
                Mock.Of<IChangeToken>(),
            };
            var compilerCache = new Mock<ICompilerCache>();
            compilerCache
                .Setup(f => f.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<RelativeFileInfo, CompilationResult>>()))
                .Returns(new CompilerCacheResult(relativePath, new CompilationResult(typeof(TestRazorPage)), expirationTokens));
            var compilerCacheProvider = new Mock<ICompilerCacheProvider>();
            compilerCacheProvider
                .SetupGet(c => c.Cache)
                .Returns(compilerCache.Object);
            var factoryProvider = new DefaultRazorPageFactoryProvider(
                Mock.Of<IRazorCompilationService>(),
                compilerCacheProvider.Object);

            // Act
            var result = factoryProvider.CreateFactory(relativePath);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(expirationTokens, result.ExpirationTokens);
        }

        [Fact]
        public void CreateFactory_ProducesDelegateThatSetsPagePath()
        {
            // Arrange
            var relativePath = "/file-exists";
            var compilerCache = new Mock<ICompilerCache>();
            compilerCache
                .Setup(f => f.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<RelativeFileInfo, CompilationResult>>()))
                .Returns(new CompilerCacheResult(relativePath, new CompilationResult(typeof(TestRazorPage)), new IChangeToken[0]));
            var compilerCacheProvider = new Mock<ICompilerCacheProvider>();
            compilerCacheProvider
                .SetupGet(c => c.Cache)
                .Returns(compilerCache.Object);
            var factoryProvider = new DefaultRazorPageFactoryProvider(
                Mock.Of<IRazorCompilationService>(),
                compilerCacheProvider.Object);

            // Act
            var result = factoryProvider.CreateFactory(relativePath);

            // Assert
            Assert.True(result.Success);
            var actual = result.RazorPageFactory();
            Assert.Equal("/file-exists", actual.Path);
        }

        private class TestRazorPage : RazorPage
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
