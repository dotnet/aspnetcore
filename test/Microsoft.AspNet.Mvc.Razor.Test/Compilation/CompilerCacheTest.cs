// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CompilerCacheTest
    {
        [Fact]
        public void GetOrAdd_ReturnsCompilationResultFromFactory()
        {
            // Arrange
            var fileSystem = new TestFileSystem();
            var cache = new CompilerCache(Enumerable.Empty<RazorFileInfoCollection>(), fileSystem);
            var fileInfo = new TestFileInfo
            {
                LastModified = DateTime.FromFileTimeUtc(10000)
            };

            var type = GetType();
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            var runtimeFileInfo = new RelativeFileInfo(fileInfo, "ab");

            // Act
            var actual = cache.GetOrAdd(runtimeFileInfo, _ => expected);

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
            private readonly List<RazorFileInfo> _fileInfos = new List<RazorFileInfo>();

            public ViewCollection()
            {
                FileInfos = _fileInfos;

                var content = new PreCompile().Content;
                var length = Encoding.UTF8.GetByteCount(content);

                Add(new RazorFileInfo()
                {
                    FullTypeName = typeof(PreCompile).FullName,
                    Hash = RazorFileHash.GetHash(GetMemoryStream(content)),
                    LastModified = DateTime.FromFileTimeUtc(10000),
                    Length = length,
                    RelativePath = "ab",
                });
            }

            public void Add(RazorFileInfo fileInfo)
            {
                _fileInfos.Add(fileInfo);
            }
        }

        private static Stream GetMemoryStream(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);

            return new MemoryStream(bytes);
        }

        [Theory]
        [InlineData(10000)]
        [InlineData(11000)]
        [InlineData(10000)] // expected failure: same time and length
        public void GetOrAdd_UsesFilesFromCache_IfTimestampDiffers_ButContentAndLengthAreTheSame(long fileTimeUTC)
        {
            // Arrange
            var instance = new RuntimeCompileIdentical();
            var length = Encoding.UTF8.GetByteCount(instance.Content);
            var collection = new ViewCollection();
            var fileSystem = new TestFileSystem();
            var cache = new CompilerCache(new[] { new ViewCollection() }, fileSystem);

            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = DateTime.FromFileTimeUtc(fileTimeUTC),
                Content = instance.Content
            };

            var runtimeFileInfo = new RelativeFileInfo(fileInfo, "ab");

            var precompiledContent = new PreCompile().Content;
            var razorFileInfo = new RazorFileInfo
            {
                FullTypeName = typeof(PreCompile).FullName,
                Hash = RazorFileHash.GetHash(GetMemoryStream(precompiledContent)),
                LastModified = DateTime.FromFileTimeUtc(10000),
                Length = Encoding.UTF8.GetByteCount(precompiledContent),
                RelativePath = "ab",
            };

            // Act
            var actual = cache.GetOrAdd(runtimeFileInfo,
                                        compile: _ => { throw new Exception("Shouldn't be called."); });

            // Assert
            Assert.Equal(typeof(PreCompile), actual.CompiledType);
        }

        [Theory]
        [InlineData(typeof(RuntimeCompileDifferent), 11000)]
        [InlineData(typeof(RuntimeCompileDifferentLength), 10000)]
        [InlineData(typeof(RuntimeCompileDifferentLength), 11000)]
        public void GetOrAdd_RecompilesFile_IfContentAndLengthAreChanged(
            Type resultViewType,
            long fileTimeUTC)
        {
            // Arrange
            var instance = (View)Activator.CreateInstance(resultViewType);
            var length = Encoding.UTF8.GetByteCount(instance.Content);
            var collection = new ViewCollection();
            var fileSystem = new TestFileSystem();
            var cache = new CompilerCache(new[] { new ViewCollection() }, fileSystem);

            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = DateTime.FromFileTimeUtc(fileTimeUTC),
                Content = instance.Content
            };

            var runtimeFileInfo = new RelativeFileInfo(fileInfo, "ab");

            var precompiledContent = new PreCompile().Content;
            var razorFileInfo = new RazorFileInfo
            {
                FullTypeName = typeof(PreCompile).FullName,
                Hash = RazorFileHash.GetHash(GetMemoryStream(precompiledContent)),
                LastModified = DateTime.FromFileTimeUtc(10000),
                Length = Encoding.UTF8.GetByteCount(precompiledContent),
                RelativePath = "ab",
            };

            // Act
            var actual = cache.GetOrAdd(runtimeFileInfo,
                                        compile: _ => CompilationResult.Successful(resultViewType));

            // Assert
            Assert.Equal(resultViewType, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_UsesValueFromCache_IfViewStartHasNotChanged()
        {
            // Arrange
            var instance = (View)Activator.CreateInstance(typeof(PreCompile));
            var length = Encoding.UTF8.GetByteCount(instance.Content);
            var fileSystem = new TestFileSystem();

            var lastModified = DateTime.UtcNow;

            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = lastModified,
                Content = instance.Content
            };
            var runtimeFileInfo = new RelativeFileInfo(fileInfo, "ab");

            var viewStartContent = "viewstart-content";
            var viewStartFileInfo = new TestFileInfo
            {
                Content = viewStartContent,
                LastModified = DateTime.UtcNow
            };
            fileSystem.AddFile("_ViewStart.cshtml", viewStartFileInfo);
            var viewStartRazorFileInfo = new RazorFileInfo
            {
                Hash = RazorFileHash.GetHash(GetMemoryStream(viewStartContent)),
                LastModified = viewStartFileInfo.LastModified,
                Length = viewStartFileInfo.Length,
                RelativePath = "_ViewStart.cshtml",
                FullTypeName = typeof(RuntimeCompileIdentical).FullName
            };

            var precompiledViews = new ViewCollection();
            precompiledViews.Add(viewStartRazorFileInfo);
            var cache = new CompilerCache(new[] { precompiledViews }, fileSystem);

            // Act
            var actual = cache.GetOrAdd(runtimeFileInfo,
                                        compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert
            Assert.Equal(typeof(PreCompile), actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_IgnoresCachedValueIfFileIsIdentical_ButViewStartWasAdedSinceTheCacheWasCreated()
        {
            // Arrange
            var expectedType = typeof(RuntimeCompileDifferent);
            var lastModified = DateTime.UtcNow;
            var fileSystem = new TestFileSystem();
            var collection = new ViewCollection();
            var precompiledFile = collection.FileInfos[0];
            precompiledFile.RelativePath = "Views\\home\\index.cshtml";
            var cache = new CompilerCache(new[] { collection }, fileSystem);
            var testFile = new TestFileInfo
            {
                Content = new PreCompile().Content,
                LastModified = precompiledFile.LastModified,
                PhysicalPath = precompiledFile.RelativePath
            };
            fileSystem.AddFile(precompiledFile.RelativePath, testFile);
            var relativeFile = new RelativeFileInfo(testFile, testFile.PhysicalPath);

            // Act 1
            var actual1 = cache.GetOrAdd(relativeFile,
                                        compile: _ => { throw new Exception("should not be called"); });

            // Assert 1
            Assert.Equal(typeof(PreCompile), actual1.CompiledType);

            // Act 2
            fileSystem.AddFile("Views\\_ViewStart.cshtml", "");
            var actual2 = cache.GetOrAdd(relativeFile,
                                         compile: _ => CompilationResult.Successful(expectedType));

            // Assert 2
            Assert.Equal(expectedType, actual2.CompiledType);
        }

        [Fact]
        public void GetOrAdd_IgnoresCachedValueIfFileIsIdentical_ButViewStartWasDeletedSinceCacheWasCreated()
        {
            // Arrange
            var expectedType = typeof(RuntimeCompileDifferent);
            var lastModified = DateTime.UtcNow;
            var fileSystem = new TestFileSystem();

            var viewCollection = new ViewCollection();
            var precompiledView = viewCollection.FileInfos[0];
            precompiledView.RelativePath = "Views\\Index.cshtml";
            var viewFileInfo = new TestFileInfo
            {
                Content = new PreCompile().Content,
                LastModified = precompiledView.LastModified,
                PhysicalPath = precompiledView.RelativePath
            };
            fileSystem.AddFile(viewFileInfo.PhysicalPath, viewFileInfo);

            var viewStartFileInfo = new TestFileInfo
            {
                PhysicalPath = "Views\\_ViewStart.cshtml",
                Content = "viewstart-content",
                LastModified = lastModified
            };
            var viewStart = new RazorFileInfo
            {
                FullTypeName = typeof(RuntimeCompileIdentical).FullName,
                RelativePath = viewStartFileInfo.PhysicalPath,
                LastModified = viewStartFileInfo.LastModified,
                Hash = RazorFileHash.GetHash(viewStartFileInfo),
                Length = viewStartFileInfo.Length
            };
            fileSystem.AddFile(viewStartFileInfo.PhysicalPath, viewStartFileInfo);

            viewCollection.Add(viewStart);
            var cache = new CompilerCache(new[] { viewCollection }, fileSystem);
            var fileInfo = new RelativeFileInfo(viewFileInfo, viewFileInfo.PhysicalPath);

            // Act 1
            var actual1 = cache.GetOrAdd(fileInfo,
                                        compile: _ => { throw new Exception("should not be called"); });

            // Assert 1
            Assert.Equal(typeof(PreCompile), actual1.CompiledType);

            // Act 2
            fileSystem.DeleteFile(viewStartFileInfo.PhysicalPath);
            var actual2 = cache.GetOrAdd(fileInfo,
                                         compile: _ => CompilationResult.Successful(expectedType));

            // Assert 2
            Assert.Equal(expectedType, actual2.CompiledType);
        }

        public static IEnumerable<object[]> GetOrAdd_IgnoresCachedValue_IfViewStartWasChangedSinceCacheWasCreatedData
        {
            get
            {
                var viewStartContent = "viewstart-content";
                var contentStream = GetMemoryStream(viewStartContent);
                var lastModified = DateTime.UtcNow;
                int length = Encoding.UTF8.GetByteCount(viewStartContent);
                var path = "Views\\_ViewStart.cshtml";

                var razorFileInfo = new RazorFileInfo
                {
                    Hash = RazorFileHash.GetHash(contentStream),
                    LastModified = lastModified,
                    Length = length,
                    RelativePath = path
                };

                // Length does not match
                var testFileInfo1 = new TestFileInfo
                {
                    Length = 7732
                };

                yield return new object[] { razorFileInfo, testFileInfo1 };

                // Content and last modified do not match
                var testFileInfo2 = new TestFileInfo
                {
                    Length = length,
                    Content = "viewstart-modified",
                    LastModified = lastModified.AddSeconds(100),
                };

                yield return new object[] { razorFileInfo, testFileInfo2 };
            }
        }

        [Theory]
        [MemberData(nameof(GetOrAdd_IgnoresCachedValue_IfViewStartWasChangedSinceCacheWasCreatedData))]
        public void GetOrAdd_IgnoresCachedValue_IfViewStartWasChangedSinceCacheWasCreated(
            RazorFileInfo viewStartRazorFileInfo, TestFileInfo viewStartFileInfo)
        {
            // Arrange
            var expectedType = typeof(RuntimeCompileDifferent);
            var lastModified = DateTime.UtcNow;
            var viewStartLastModified = DateTime.UtcNow;
            var content = "some content";
            var fileInfo = new TestFileInfo
            {
                Length = 1020,
                Content = content,
                LastModified = lastModified,
                PhysicalPath = "Views\\home\\index.cshtml"
            };

            var runtimeFileInfo = new RelativeFileInfo(fileInfo, fileInfo.PhysicalPath);

            var razorFileInfo = new RazorFileInfo
            {
                FullTypeName = typeof(PreCompile).FullName,
                Hash = RazorFileHash.GetHash(fileInfo),
                LastModified = lastModified,
                Length = Encoding.UTF8.GetByteCount(content),
                RelativePath = fileInfo.PhysicalPath,
            };

            var fileSystem = new TestFileSystem();
            fileSystem.AddFile(viewStartRazorFileInfo.RelativePath, viewStartFileInfo);
            var viewCollection = new ViewCollection();
            var cache = new CompilerCache(new[] { viewCollection }, fileSystem);

            // Act
            var actual = cache.GetOrAdd(runtimeFileInfo,
                                        compile: _ => CompilationResult.Successful(expectedType));

            // Assert
            Assert.Equal(expectedType, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_DoesNotCacheCompiledContent_OnCallsAfterInitial()
        {
            // Arrange
            var lastModified = DateTime.UtcNow;
            var cache = new CompilerCache(Enumerable.Empty<RazorFileInfoCollection>(), new TestFileSystem());
            var fileInfo = new TestFileInfo
            {
                PhysicalPath = "test",
                LastModified = lastModified
            };
            var type = GetType();
            var uncachedResult = UncachedCompilationResult.Successful(type, "hello world");

            var runtimeFileInfo = new RelativeFileInfo(fileInfo, "test");

            // Act
            cache.GetOrAdd(runtimeFileInfo, _ => uncachedResult);
            var actual1 = cache.GetOrAdd(runtimeFileInfo, _ => uncachedResult);
            var actual2 = cache.GetOrAdd(runtimeFileInfo, _ => uncachedResult);

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