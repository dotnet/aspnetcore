// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            var fileInfo = new Mock<IFileInfo>();

            fileInfo
                .SetupGet(i => i.LastModified)
                .Returns(DateTime.FromFileTimeUtc(10000));

            var type = GetType();
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            var runtimeFileInfo = new RelativeFileInfo()
            {
                FileInfo = fileInfo.Object,
                RelativePath = "ab",
            };

            // Act
            var actual = cache.GetOrAdd(runtimeFileInfo, false, () => expected);

            // Assert
            Assert.Same(expected, actual);
            Assert.Equal("hello world", actual.CompiledContent);
            Assert.Same(type, actual.CompiledType);
        }

        private abstract class View
        {
            public abstract string Content { get; }
        }

        private class PreCompile : View
        {
            public override string Content { get { return "Hello World it's @DateTime.Now"; } }
        }

        private class RuntimeCompileIdentical : View
        {
            public override string Content { get { return new PreCompile().Content; } }
        }

        private class RuntimeCompileDifferent : View
        {
            public override string Content { get { return new PreCompile().Content.Substring(1) + " "; } }
        }

        private class RuntimeCompileDifferentLength : View
        {
            public override string Content
            {
                get
                {
                    return new PreCompile().Content + " longer because it was modified at runtime";
                }
            }
        }

        private class ViewCollection : RazorFileInfoCollection
        {
            public ViewCollection()
            {
                var fileInfos = new List<RazorFileInfo>();
                FileInfos = fileInfos;

                var content = new PreCompile().Content;
                var length = Encoding.UTF8.GetByteCount(content);

                fileInfos.Add(new RazorFileInfo()
                {
                    FullTypeName = typeof(PreCompile).FullName,
                    Hash = RazorFileHash.GetHash(GetMemoryStream(content)),
                    LastModified = DateTime.FromFileTimeUtc(10000),
                    Length = length,
                    RelativePath = "ab",
                });
            }
        }

        private static Stream GetMemoryStream(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);

            return new MemoryStream(bytes);
        }

        [Theory]
        [InlineData(typeof(RuntimeCompileIdentical), 10000, false)]
        [InlineData(typeof(RuntimeCompileIdentical), 11000, false)]
        [InlineData(typeof(RuntimeCompileDifferent), 10000, false)] // expected failure: same time and length
        [InlineData(typeof(RuntimeCompileDifferent), 11000, true)]
        [InlineData(typeof(RuntimeCompileDifferentLength), 10000, true)]
        [InlineData(typeof(RuntimeCompileDifferentLength), 10000, true)]
        public void FileWithTheSameLengthAndDifferentTime_DoesNot_OverridesPrecompilation(
            Type resultViewType,
            long fileTimeUTC,
            bool swapsPreCompile)
        {
            // Arrange
            var instance = (View)Activator.CreateInstance(resultViewType);
            var length = Encoding.UTF8.GetByteCount(instance.Content);

            var collection = new ViewCollection();
            var cache = new CompilerCache(new[] { new ViewCollection() });

            var fileInfo = new Mock<IFileInfo>();
            fileInfo
                .SetupGet(i => i.Length)
                .Returns(length);
            fileInfo
                .SetupGet(i => i.LastModified)
                .Returns(DateTime.FromFileTimeUtc(fileTimeUTC));
            fileInfo.Setup(i => i.CreateReadStream())
                .Returns(GetMemoryStream(instance.Content));

            var preCompileType = typeof(PreCompile);

            var runtimeFileInfo = new RelativeFileInfo()
            {
                FileInfo = fileInfo.Object,
                RelativePath = "ab",
            };

            // Act
            var actual = cache.GetOrAdd(runtimeFileInfo,
                                        enableInstrumentation: false,
                                        compile: () => CompilationResult.Successful(resultViewType));

            // Assert
            if (swapsPreCompile)
            {
                Assert.Equal(actual.CompiledType, resultViewType);
            }
            else
            {
                Assert.Equal(actual.CompiledType, typeof(PreCompile));
            }
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

            var runtimeFileInfo = new RelativeFileInfo()
            {
                FileInfo = fileInfo.Object,
                RelativePath = "test",
            };

            // Act
            cache.GetOrAdd(runtimeFileInfo, false, () => uncachedResult);
            var actual1 = cache.GetOrAdd(runtimeFileInfo, false, () => uncachedResult);
            var actual2 = cache.GetOrAdd(runtimeFileInfo, false, () => uncachedResult);

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

        [Fact]
        public void GetOrAdd_IgnoresCache_IfCachedItemIsNotInstrumentedAndEnableInstrumentationIsTrue()
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
            var uncachedResult1 = UncachedCompilationResult.Successful(type, "hello world");
            var uncachedResult2 = UncachedCompilationResult.Successful(typeof(object), "hello world");
            var uncachedResult3 = UncachedCompilationResult.Successful(typeof(Guid), "hello world");

            var runtimeFileInfo = new RelativeFileInfo()
            {
                FileInfo = fileInfo.Object,
                RelativePath = "test",
            };

            // Act
            cache.GetOrAdd(runtimeFileInfo, false, () => uncachedResult1);
            var actual1 = cache.GetOrAdd(runtimeFileInfo, true, () => uncachedResult2);
            var actual2 = cache.GetOrAdd(runtimeFileInfo, false, () => uncachedResult3);

            // Assert
            Assert.Same(uncachedResult2, actual1);
            Assert.Same(typeof(object), actual1.CompiledType);

            Assert.NotSame(actual2, uncachedResult3);
            Assert.Same(typeof(object), actual2.CompiledType);
        }
    }
}