// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor.Internal;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Runtime;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class CompilerCacheTest
    {
        private const string ViewPath = "view-path";
        private readonly IAssemblyLoadContext TestLoadContext = Mock.Of<IAssemblyLoadContext>();

        [Fact]
        public void GetOrAdd_ReturnsFileNotFoundResult_IfFileIsNotFoundInFileSystem()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(Enumerable.Empty<RazorFileInfoCollection>(), TestLoadContext, fileProvider);
            var type = GetType();

            // Act
            var result = cache.GetOrAdd("/some/path", _ => { throw new Exception("Shouldn't be called"); });

            // Assert
            Assert.Same(CompilerCacheResult.FileNotFound, result);
            Assert.Null(result.CompilationResult);
        }

        [Fact]
        public void GetOrAdd_ReturnsCompilationResultFromFactory()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(Enumerable.Empty<RazorFileInfoCollection>(), TestLoadContext, fileProvider);
            var type = GetType();
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            // Act
            var result = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = Assert.IsType<UncachedCompilationResult>(result.CompilationResult);
            Assert.NotNull(actual);
            Assert.Same(expected, actual);
            Assert.Equal("hello world", actual.CompiledContent);
            Assert.Same(type, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsFileNotFoundIfFileWasDeleted()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(Enumerable.Empty<RazorFileInfoCollection>(), TestLoadContext, fileProvider);
            var type = typeof(RuntimeCompileIdentical);
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            Assert.Same(expected, result1.CompilationResult);

            // Act 2
            // Delete the file from the file system and set it's expiration trigger.
            fileProvider.DeleteFile(ViewPath);
            fileProvider.GetTrigger(ViewPath).IsExpired = true;
            var result2 = cache.GetOrAdd(ViewPath, _ => { throw new Exception("shouldn't be called."); });

            // Assert 2
            Assert.Same(CompilerCacheResult.FileNotFound, result2);
            Assert.Null(result2.CompilationResult);
        }

        [Fact]
        public void GetOrAdd_ReturnsNewResultIfFileWasModified()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(Enumerable.Empty<RazorFileInfoCollection>(), TestLoadContext, fileProvider);
            var type = typeof(RuntimeCompileIdentical);
            var expected1 = UncachedCompilationResult.Successful(type, "hello world");
            var expected2 = UncachedCompilationResult.Successful(type, "different content");

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected1);

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            Assert.Same(expected1, result1.CompilationResult);

            // Act 2
            fileProvider.GetTrigger(ViewPath).IsExpired = true;
            var result2 = cache.GetOrAdd(ViewPath, _ => expected2);

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            Assert.Same(expected2, result2.CompilationResult);
        }

        [Fact]
        public void GetOrAdd_DoesNotQueryFileSystem_IfCachedFileTriggerWasNotSet()
        {
            // Arrange
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(Enumerable.Empty<RazorFileInfoCollection>(), TestLoadContext, fileProvider);
            var type = typeof(RuntimeCompileIdentical);
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            Assert.Same(expected, result1.CompilationResult);

            // Act 2
            var result2 = cache.GetOrAdd(ViewPath, _ => { throw new Exception("shouldn't be called"); });

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            Assert.IsType<CompilationResult>(result2.CompilationResult);
            Assert.Same(type, result2.CompilationResult.CompiledType);
            mockFileProvider.Verify(v => v.GetFileInfo(ViewPath), Times.Once());
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

        private static Stream GetMemoryStream(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);

            return new MemoryStream(bytes);
        }

        [Theory]
        [InlineData(10000)]
        [InlineData(11000)]
        public void GetOrAdd_UsesFilesFromCache_IfTimestampDiffers_ButContentAndLengthAreTheSame(long fileTimeUTC)
        {
            // Arrange
            var instance = new RuntimeCompileIdentical();
            var length = Encoding.UTF8.GetByteCount(instance.Content);
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(new[] { new ViewCollection() }, TestLoadContext, fileProvider);
            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = DateTime.FromFileTimeUtc(fileTimeUTC),
                Content = instance.Content
            };
            fileProvider.AddFile(ViewPath, fileInfo);

            // Act
            var result = cache.GetOrAdd(ViewPath,
                                        compile: _ => { throw new Exception("Shouldn't be called."); });

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
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
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(new[] { new ViewCollection() }, TestLoadContext, fileProvider);

            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = DateTime.FromFileTimeUtc(fileTimeUTC),
                Content = instance.Content
            };
            fileProvider.AddFile(ViewPath, fileInfo);

            // Act
            var result = cache.GetOrAdd(ViewPath,
                                        compile: _ => CompilationResult.Successful(resultViewType));

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
            Assert.Equal(resultViewType, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_UsesValueFromCache_IfGlobalHasNotChanged()
        {
            // Arrange
            var instance = new PreCompile();
            var length = Encoding.UTF8.GetByteCount(instance.Content);
            var fileProvider = new TestFileProvider();

            var lastModified = DateTime.UtcNow;

            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = lastModified,
                Content = instance.Content
            };
            fileProvider.AddFile(ViewPath, fileInfo);

            var globalContent = "global-content";
            var globalFileInfo = new TestFileInfo
            {
                Content = globalContent,
                LastModified = DateTime.UtcNow
            };
            fileProvider.AddFile("_ViewImports.cshtml", globalFileInfo);
            var globalRazorFileInfo = new RazorFileInfo
            {
                Hash = Crc32.Calculate(GetMemoryStream(globalContent)).ToString(CultureInfo.InvariantCulture),
                HashAlgorithmVersion = 1,
                LastModified = globalFileInfo.LastModified,
                Length = globalFileInfo.Length,
                RelativePath = "_ViewImports.cshtml",
                FullTypeName = typeof(RuntimeCompileIdentical).FullName
            };
            var precompiledViews = new ViewCollection();
            precompiledViews.Add(globalRazorFileInfo);
            var cache = new CompilerCache(new[] { precompiledViews }, TestLoadContext, fileProvider);

            // Act
            var result = cache.GetOrAdd(ViewPath,
                                        compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
            Assert.Equal(typeof(PreCompile), actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsFileNotFoundResult_IfPrecompiledViewWasRemovedFromFileSystem()
        {
            // Arrange
            var precompiledViews = new ViewCollection();
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(new[] { precompiledViews }, TestLoadContext, fileProvider);

            // Act
            var result = cache.GetOrAdd(ViewPath,
                                        compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert
            Assert.Same(CompilerCacheResult.FileNotFound, result);
            Assert.Null(result.CompilationResult);
        }

        [Fact]
        public void GetOrAdd_DoesNotReadFileFromFileSystemAfterPrecompiledViewIsVerified()
        {
            // Arrange
            var precompiledViews = new ViewCollection();
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            var precompiledView = precompiledViews.FileInfos[0];
            var fileInfo = new TestFileInfo
            {
                Length = precompiledView.Length,
                LastModified = precompiledView.LastModified,
            };
            fileProvider.AddFile(ViewPath, fileInfo);
            var cache = new CompilerCache(new[] { precompiledViews }, TestLoadContext, fileProvider);

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath,
                                         compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            var actual1 = result1.CompilationResult;
            Assert.NotNull(actual1);
            Assert.Equal(typeof(PreCompile), actual1.CompiledType);
            mockFileProvider.Verify(v => v.GetFileInfo(ViewPath), Times.Once());

            // Act 2
            var result2 = cache.GetOrAdd(ViewPath,
                                         compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            var actual2 = result2.CompilationResult;
            Assert.NotNull(actual2);
            Assert.Equal(typeof(PreCompile), actual2.CompiledType);
            mockFileProvider.Verify(v => v.GetFileInfo(ViewPath), Times.Once());
        }

        [ConditionalTheory]
        // Skipping for now since this is going to change.
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void GetOrAdd_IgnoresCachedValueIfFileIsIdentical_ButViewImportsWasAdedSinceTheCacheWasCreated()
        {
            // Arrange
            var expectedType = typeof(RuntimeCompileDifferent);
            var fileProvider = new TestFileProvider();
            var collection = new ViewCollection();
            var precompiledFile = collection.FileInfos[0];
            precompiledFile.RelativePath = "Views\\home\\index.cshtml";
            var cache = new CompilerCache(new[] { collection }, TestLoadContext, fileProvider);
            var testFile = new TestFileInfo
            {
                Content = new PreCompile().Content,
                LastModified = precompiledFile.LastModified,
                PhysicalPath = precompiledFile.RelativePath
            };
            fileProvider.AddFile(precompiledFile.RelativePath, testFile);
            var relativeFile = new RelativeFileInfo(testFile, testFile.PhysicalPath);

            // Act 1
            var result1 = cache.GetOrAdd(testFile.PhysicalPath,
                                        compile: _ => { throw new Exception("should not be called"); });

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            var actual1 = result1.CompilationResult;
            Assert.NotNull(actual1);
            Assert.Equal(typeof(PreCompile), actual1.CompiledType);

            // Act 2
            var globalTrigger = fileProvider.GetTrigger("Views\\_ViewImports.cshtml");
            globalTrigger.IsExpired = true;
            var result2 = cache.GetOrAdd(testFile.PhysicalPath,
                                         compile: _ => CompilationResult.Successful(expectedType));

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            var actual2 = result2.CompilationResult;
            Assert.NotNull(actual2);
            Assert.Equal(expectedType, actual2.CompiledType);
        }

        [ConditionalTheory]
        // Skipping for now since this is going to change.
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void GetOrAdd_IgnoresCachedValueIfFileIsIdentical_ButGlobalWasDeletedSinceCacheWasCreated()
        {
            // Arrange
            var expectedType = typeof(RuntimeCompileDifferent);
            var lastModified = DateTime.UtcNow;
            var fileProvider = new TestFileProvider();

            var viewCollection = new ViewCollection();
            var precompiledView = viewCollection.FileInfos[0];
            precompiledView.RelativePath = "Views\\Index.cshtml";
            var viewFileInfo = new TestFileInfo
            {
                Content = new PreCompile().Content,
                LastModified = precompiledView.LastModified,
                PhysicalPath = precompiledView.RelativePath
            };
            fileProvider.AddFile(viewFileInfo.PhysicalPath, viewFileInfo);

            var globalFileInfo = new TestFileInfo
            {
                PhysicalPath = "Views\\_ViewImports.cshtml",
                Content = "viewstart-content",
                LastModified = lastModified
            };
            var globalFile = new RazorFileInfo
            {
                FullTypeName = typeof(RuntimeCompileIdentical).FullName,
                RelativePath = globalFileInfo.PhysicalPath,
                LastModified = globalFileInfo.LastModified,
                Hash = RazorFileHash.GetHash(globalFileInfo, hashAlgorithmVersion: 1),
                HashAlgorithmVersion = 1,
                Length = globalFileInfo.Length
            };
            fileProvider.AddFile(globalFileInfo.PhysicalPath, globalFileInfo);

            viewCollection.Add(globalFile);
            var cache = new CompilerCache(new[] { viewCollection }, TestLoadContext, fileProvider);

            // Act 1
            var result1 = cache.GetOrAdd(viewFileInfo.PhysicalPath,
                                        compile: _ => { throw new Exception("should not be called"); });

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            var actual1 = result1.CompilationResult;
            Assert.NotNull(actual1);
            Assert.Equal(typeof(PreCompile), actual1.CompiledType);

            // Act 2
            var trigger = fileProvider.GetTrigger(globalFileInfo.PhysicalPath);
            trigger.IsExpired = true;
            var result2 = cache.GetOrAdd(viewFileInfo.PhysicalPath,
                                         compile: _ => CompilationResult.Successful(expectedType));

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            var actual2 = result2.CompilationResult;
            Assert.NotNull(actual2);
            Assert.Equal(expectedType, actual2.CompiledType);
        }

        public static IEnumerable<object[]> GetOrAdd_IgnoresCachedValue_IfGlobalWasChangedSinceCacheWasCreatedData
        {
            get
            {
                var globalContent = "global-content";
                var contentStream = GetMemoryStream(globalContent);
                var lastModified = DateTime.UtcNow;
                int length = Encoding.UTF8.GetByteCount(globalContent);
                var path = "Views\\_ViewImports.cshtml";

                var razorFileInfo = new RazorFileInfo
                {
                    Hash = Crc32.Calculate(contentStream).ToString(CultureInfo.InvariantCulture),
                    HashAlgorithmVersion = 1,
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
        [MemberData(nameof(GetOrAdd_IgnoresCachedValue_IfGlobalWasChangedSinceCacheWasCreatedData))]
        public void GetOrAdd_IgnoresCachedValue_IfGlobalFileWasChangedSinceCacheWasCreated(
            RazorFileInfo viewStartRazorFileInfo, IFileInfo globalFileInfo)
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

            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(fileInfo.PhysicalPath, fileInfo);
            fileProvider.AddFile(viewStartRazorFileInfo.RelativePath, globalFileInfo);
            var viewCollection = new ViewCollection();
            var cache = new CompilerCache(new[] { viewCollection }, TestLoadContext, fileProvider);

            // Act
            var result = cache.GetOrAdd(fileInfo.PhysicalPath,
                                        compile: _ => CompilationResult.Successful(expectedType));

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
            Assert.Equal(expectedType, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_DoesNotCacheCompiledContent_OnCallsAfterInitial()
        {
            // Arrange
            var lastModified = DateTime.UtcNow;
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(Enumerable.Empty<RazorFileInfoCollection>(), TestLoadContext, fileProvider);
            var fileInfo = new TestFileInfo
            {
                PhysicalPath = "test",
                LastModified = lastModified
            };
            fileProvider.AddFile("test", fileInfo);
            var type = GetType();
            var uncachedResult = UncachedCompilationResult.Successful(type, "hello world");

            // Act
            cache.GetOrAdd("test", _ => uncachedResult);
            var result1 = cache.GetOrAdd("test", _ => { throw new Exception("shouldn't be called."); });
            var result2 = cache.GetOrAdd("test", _ => { throw new Exception("shouldn't be called."); });

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);

            var actual1 = Assert.IsType<CompilationResult>(result1.CompilationResult);
            var actual2 = Assert.IsType<CompilationResult>(result2.CompilationResult);
            Assert.NotSame(uncachedResult, actual1);
            Assert.NotSame(uncachedResult, actual2);
            Assert.Same(type, actual1.CompiledType);
            Assert.Same(type, actual2.CompiledType);
        }

        [Fact]
        public void Match_ReturnsFalse_IfTypeIsAbstract()
        {
            // Arrange
            var type = typeof(AbstractRazorFileInfoCollection);

            // Act
            var result = CompilerCache.Match(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_ReturnsFalse_IfTypeHasGenericParameters()
        {
            // Arrange
            var type = typeof(GenericRazorFileInfoCollection<>);

            // Act
            var result = CompilerCache.Match(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_ReturnsFalse_IfTypeDoesNotHaveDefaultConstructor()
        {
            // Arrange
            var type = typeof(ParameterConstructorRazorFileInfoCollection);

            // Act
            var result = CompilerCache.Match(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_ReturnsFalse_IfTypeDoesNotDeriveFromRazorFileInfoCollection()
        {
            // Arrange
            var type = typeof(NonSubTypeRazorFileInfoCollection);

            // Act
            var result = CompilerCache.Match(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_ReturnsTrue_IfTypeDerivesFromRazorFileInfoCollection()
        {
            // Arrange
            var type = typeof(ViewCollection);

            // Act
            var result = CompilerCache.Match(type);

            // Assert
            Assert.True(result);
        }

        private abstract class AbstractRazorFileInfoCollection : RazorFileInfoCollection
        {

        }

        private class GenericRazorFileInfoCollection<TVal> : RazorFileInfoCollection
        {

        }

        private class ParameterConstructorRazorFileInfoCollection : RazorFileInfoCollection
        {
            public ParameterConstructorRazorFileInfoCollection(string value)
            {
            }
        }

        private class NonSubTypeRazorFileInfoCollection : Controller
        {

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
                    Hash = Crc32.Calculate(GetMemoryStream(content)).ToString(CultureInfo.InvariantCulture),
                    HashAlgorithmVersion = 1,
                    LastModified = DateTime.FromFileTimeUtc(10000),
                    Length = length,
                    RelativePath = ViewPath,
                });
            }

            public void Add(RazorFileInfo fileInfo)
            {
                _fileInfos.Add(fileInfo);
            }

            public override Assembly LoadAssembly(IAssemblyLoadContext loadContext)
            {
                return typeof(ViewCollection).Assembly;
            }
        }
    }
}