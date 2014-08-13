// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileSystems;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Test
{
    public class CompilerCacheTest
    {
        [Fact]
        public void GetOrAdd_ReturnsCompilationResultFromFactory()
        {
            // Arrange
            var cache = new CompilerCache();
            var fileInfo = Mock.Of<IFileInfo>();
            var type = GetType();
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            // Act
            var actual = cache.GetOrAdd(fileInfo, () => expected);

            // Assert
            Assert.Same(expected, actual);
            Assert.Equal("hello world", actual.CompiledContent);
            Assert.Same(type, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_DoesNotCacheCompiledContent_OnCallsAfterInitial()
        {
            // Arrange
            var lastModified = DateTime.UtcNow;
            var cache = new CompilerCache();
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.SetupGet(f => f.PhysicalPath)
                    .Returns("test");
            fileInfo.SetupGet(f => f.LastModified)
                    .Returns(lastModified);
            var type = GetType();
            var uncachedResult = UncachedCompilationResult.Successful(type, "hello world");

            // Act
            cache.GetOrAdd(fileInfo.Object, () => uncachedResult);
            var actual1 = cache.GetOrAdd(fileInfo.Object, () => uncachedResult);
            var actual2 = cache.GetOrAdd(fileInfo.Object, () => uncachedResult);

            // Assert
            Assert.NotSame(uncachedResult, actual1);
            Assert.NotSame(uncachedResult, actual2);
            var result = Assert.IsType<CompilationResult>(actual1);
            Assert.Null(actual1.CompiledContent);
            Assert.Same(type, actual1.CompiledType);

            result = Assert.IsType<CompilationResult>(actual2);
            Assert.Null(actual2.CompiledContent);
            Assert.Same(type, actual2.CompiledType);
        }
    }
}