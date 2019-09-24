// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
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

        private static TestRazorViewCompiler GetViewCompiler(IList<CompiledViewDescriptor> compiledViews = null)
        {
            compiledViews = compiledViews ?? Array.Empty<CompiledViewDescriptor>();

            var viewCompiler = new TestRazorViewCompiler(compiledViews);
            return viewCompiler;
        }

        private class TestRazorViewCompiler : DefaultViewCompiler
        {
            public TestRazorViewCompiler(IList<CompiledViewDescriptor> compiledViews) :
                base(compiledViews, NullLogger.Instance)
            {
            }
        }
    }
}