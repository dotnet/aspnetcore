// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
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
            var path = "/file-does-not-exist";
            var expirationTokens = new[]
            {
                Mock.Of<IChangeToken>(),
                Mock.Of<IChangeToken>(),
            };
            var compilerCache = new Mock<ICompilerCache>();
            compilerCache
                .Setup(f => f.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<string, CompilerCacheContext>>()))
                .Returns(new CompilerCacheResult(path, expirationTokens));

            var factoryProvider = new DefaultRazorPageFactoryProvider(CreateCompiler(compilerCache.Object));

            // Act
            var result = factoryProvider.CreateFactory(path);

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
                .Setup(f => f.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<string, CompilerCacheContext>>()))
                .Returns(new CompilerCacheResult(relativePath, new CompilationResult(typeof(TestRazorPage)), expirationTokens));

            var factoryProvider = new DefaultRazorPageFactoryProvider(CreateCompiler(compilerCache.Object));

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
                .Setup(f => f.GetOrAdd(It.IsAny<string>(), It.IsAny<Func<string, CompilerCacheContext>>()))
                .Returns(new CompilerCacheResult(relativePath, new CompilationResult(typeof(TestRazorPage)), new IChangeToken[0]));
            
            var factoryProvider = new DefaultRazorPageFactoryProvider(CreateCompiler(compilerCache.Object));

            // Act
            var result = factoryProvider.CreateFactory(relativePath);

            // Assert
            Assert.True(result.Success);
            var actual = result.RazorPageFactory();
            Assert.Equal("/file-exists", actual.Path);
        }

        private RazorCompiler CreateCompiler(ICompilerCache cache)
        {
            var compilerCacheProvider = new Mock<ICompilerCacheProvider>();
            compilerCacheProvider
                .SetupGet(c => c.Cache)
                .Returns(cache);

            return new RazorCompiler(
                Mock.Of<ICompilationService>(),
                compilerCacheProvider.Object,
                new MvcRazorTemplateEngine(RazorEngine.Create(), new DefaultRazorProject(new TestFileProvider())));
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
