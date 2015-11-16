// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class CompilerCacheTest
    {
        private const string ViewPath = "/Views/Home/Index.cshtml";
        private const string PrecompiledViewsPath = "/Views/Home/Precompiled.cshtml";
        private readonly IDictionary<string, Type> _precompiledViews = new Dictionary<string, Type>
        {
            { PrecompiledViewsPath, typeof(PreCompile) }
        };

        public static TheoryData ViewImportsPaths =>
            new TheoryData<string>
            {
                "/Views/Home/_ViewImports.cshtml",
                "/Views/_ViewImports.cshtml",
                "/_ViewImports.cshtml",
            };

        [Fact]
        public void GetOrAdd_ReturnsFileNotFoundResult_IfFileIsNotFoundInFileSystem()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(fileProvider);

            // Act
            var result = cache.GetOrAdd("/some/path", ThrowsIfCalled);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void GetOrAdd_ReturnsCompilationResultFromFactory()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var type = typeof(TestView);
            var expected = new CompilationResult(type);

            // Act
            var result = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert
            Assert.True(result.Success);
            Assert.Same(type, result.CompilationResult.CompiledType);
        }

        [Theory]
        [InlineData("/Areas/Finances/Views/Home/Index.cshtml")]
        [InlineData(@"Areas\Finances\Views\Home\Index.cshtml")]
        [InlineData(@"\Areas\Finances\Views\Home\Index.cshtml")]
        [InlineData(@"\Areas\Finances\Views/Home\Index.cshtml")]
        public void GetOrAdd_NormalizesPathSepartorForPaths(string relativePath)
        {
            // Arrange
            var viewPath = "/Areas/Finances/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(viewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var type = typeof(TestView);
            var expected = new CompilationResult(type);

            // Act - 1
            var result1 = cache.GetOrAdd(@"Areas\Finances\Views\Home\Index.cshtml", _ => expected);

            // Assert - 1
            Assert.Same(type, result1.CompilationResult.CompiledType);

            // Act - 2
            var result2 = cache.GetOrAdd(relativePath, ThrowsIfCalled);

            // Assert - 2
            Assert.Same(type, result2.CompilationResult.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsFailedCompilationResult_IfFileWasRemovedFromFileSystem()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var type = typeof(TestView);
            var expected = new CompilationResult(type);

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert 1
            Assert.True(result1.Success);
            Assert.Same(expected.CompiledType, result1.CompilationResult.CompiledType);

            // Act 2
            // Delete the file from the file system and set it's expiration token.
            fileProvider.DeleteFile(ViewPath);
            fileProvider.GetChangeToken(ViewPath).HasChanged = true;
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.False(result2.Success);
        }

        [Fact]
        public void GetOrAdd_ReturnsNewResultIfFileWasModified()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var expected1 = new CompilationResult(typeof(TestView));
            var expected2 = new CompilationResult(typeof(DifferentView));

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected1);

            // Assert 1
            Assert.True(result1.Success);
            Assert.Same(typeof(TestView), result1.CompilationResult.CompiledType);

            // Act 2
            // Verify we're getting cached results.
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.True(result2.Success);
            Assert.Same(expected1.CompiledType, result2.CompilationResult.CompiledType);

            // Act 3
            fileProvider.GetChangeToken(ViewPath).HasChanged = true;
            var result3 = cache.GetOrAdd(ViewPath, _ => expected2);

            // Assert 3
            Assert.True(result3.Success);
            Assert.Same(expected2.CompiledType, result3.CompilationResult.CompiledType);
        }

        [Theory]
        [MemberData(nameof(ViewImportsPaths))]
        public void GetOrAdd_ReturnsNewResult_IfAncestorViewImportsWereModified(string globalImportPath)
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var expected1 = new CompilationResult(typeof(TestView));
            var expected2 = new CompilationResult(typeof(DifferentView));

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected1);

            // Assert 1
            Assert.True(result1.Success);
            Assert.Same(expected1.CompiledType, result1.CompilationResult.CompiledType);

            // Act 2
            // Verify we're getting cached results.
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.True(result2.Success);
            Assert.Same(expected1.CompiledType, result2.CompilationResult.CompiledType);

            // Act 3
            fileProvider.GetChangeToken(globalImportPath).HasChanged = true;
            var result3 = cache.GetOrAdd(ViewPath, _ => expected2);

            // Assert 2
            Assert.True(result3.Success);
            Assert.Same(expected2.CompiledType, result3.CompilationResult.CompiledType);
        }

        [Fact]
        public void GetOrAdd_DoesNotQueryFileSystem_IfCachedFileTriggerWasNotSet()
        {
            // Arrange
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var type = typeof(TestView);
            var expected = new CompilationResult(type);

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert 1
            Assert.True(result1.Success);
            Assert.Same(type, result1.CompilationResult.CompiledType);

            // Act 2
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.True(result2.Success);
            Assert.Same(type, result2.CompilationResult.CompiledType);
            mockFileProvider.Verify(v => v.GetFileInfo(ViewPath), Times.Once());
        }

        [Fact]
        public void GetOrAdd_UsesViewsSpecifiedFromRazorFileInfoCollection()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(fileProvider, _precompiledViews);

            // Act
            var result = cache.GetOrAdd(PrecompiledViewsPath, ThrowsIfCalled);

            // Assert
            Assert.True(result.Success);
            Assert.Same(typeof(PreCompile), result.CompilationResult.CompiledType);
        }

        [Fact]
        public void GetOrAdd_DoesNotRecompile_IfFileTriggerWasSetForPrecompiledFile()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(fileProvider, _precompiledViews);

            // Act
            fileProvider.Watch(PrecompiledViewsPath);
            fileProvider.GetChangeToken(PrecompiledViewsPath).HasChanged = true;
            var result = cache.GetOrAdd(PrecompiledViewsPath, ThrowsIfCalled);

            // Assert
            Assert.True(result.Success);
            Assert.Same(typeof(PreCompile), result.CompilationResult.CompiledType);
        }

        [Theory]
        [MemberData(nameof(ViewImportsPaths))]
        public void GetOrAdd_DoesNotRecompile_IfFileTriggerWasSetForViewImports(string globalImportPath)
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(fileProvider, _precompiledViews);

            // Act
            fileProvider.Watch(globalImportPath);
            fileProvider.GetChangeToken(globalImportPath).HasChanged = true;
            var result = cache.GetOrAdd(PrecompiledViewsPath, ThrowsIfCalled);

            // Assert
            Assert.True(result.Success);
            Assert.Same(typeof(PreCompile), result.CompilationResult.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsRuntimeCompiledAndPrecompiledViews()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider, _precompiledViews);
            var expected = new CompilationResult(typeof(TestView));

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert 1
            Assert.Same(typeof(TestView), result1.CompilationResult.CompiledType);

            // Act 2
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.True(result2.Success);
            Assert.Same(typeof(TestView), result2.CompilationResult.CompiledType);

            // Act 3
            var result3 = cache.GetOrAdd(PrecompiledViewsPath, ThrowsIfCalled);

            // Assert 3
            Assert.True(result2.Success);
            Assert.Same(typeof(PreCompile), result3.CompilationResult.CompiledType);
        }

        [Theory]
        [InlineData("/Areas/Finances/Views/Home/Index.cshtml")]
        [InlineData(@"Areas\Finances\Views\Home\Index.cshtml")]
        [InlineData(@"\Areas\Finances\Views\Home\Index.cshtml")]
        [InlineData(@"\Areas\Finances\Views/Home\Index.cshtml")]
        public void GetOrAdd_NormalizesPathSepartorForPathsThatArePrecompiled(string relativePath)
        {
            // Arrange
            var expected = typeof(PreCompile);
            var viewPath = "/Areas/Finances/Views/Home/Index.cshtml";
            var cache = new CompilerCache(
                new TestFileProvider(),
                new Dictionary<string, Type>
                {
                    { viewPath, expected }
                });

            // Act
            var result = cache.GetOrAdd(relativePath, ThrowsIfCalled);

            // Assert
            Assert.Same(expected, result.CompilationResult.CompiledType);
        }

        [Theory]
        [InlineData(@"Areas\Finances\Views\Home\Index.cshtml")]
        [InlineData(@"\Areas\Finances\Views\Home\Index.cshtml")]
        [InlineData(@"\Areas\Finances\Views/Home\Index.cshtml")]
        public void ConstructorNormalizesPrecompiledViewPath(string viewPath)
        {
            // Arrange
            var expected = typeof(PreCompile);
            var cache = new CompilerCache(
                new TestFileProvider(),
                new Dictionary<string, Type>
                {
                    { viewPath, expected }
                });

            // Act
            var result = cache.GetOrAdd("/Areas/Finances/Views/Home/Index.cshtml", ThrowsIfCalled);

            // Assert
            Assert.Same(expected, result.CompilationResult.CompiledType);
        }

        [Fact]
        public async Task GetOrAdd_AllowsConcurrentCompilationOfMultipleRazorPages()
        {
            // Arrange
            var waitDuration = TimeSpan.FromSeconds(20);
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile("/Views/Home/Index.cshtml", "Index content");
            fileProvider.AddFile("/Views/Home/About.cshtml", "About content");
            var resetEvent1 = new AutoResetEvent(initialState: false);
            var resetEvent2 = new ManualResetEvent(initialState: false);
            var cache = new CompilerCache(fileProvider);
            var compilingOne = false;
            var compilingTwo = false;

            // Act
            var task1 = Task.Run(() =>
            {
                return cache.GetOrAdd("/Views/Home/Index.cshtml", file =>
                {
                    compilingOne = true;

                    // Event 2
                    resetEvent1.WaitOne(waitDuration);

                    // Event 3
                    resetEvent2.Set();

                    // Event 6
                    resetEvent1.WaitOne(waitDuration);

                    Assert.True(compilingTwo);
                    return new CompilationResult(typeof(TestView));
                });
            });

            var task2 = Task.Run(() =>
            {
                // Event 4
                return cache.GetOrAdd("/Views/Home/About.cshtml", file =>
                {
                    compilingTwo = true;

                    // Event 4
                    resetEvent2.WaitOne(waitDuration);

                    // Event 5
                    resetEvent1.Set();

                    Assert.True(compilingOne);
                    return new CompilationResult(typeof(DifferentView));
                });
            });

            // Event 1
            resetEvent1.Set();

            await Task.WhenAll(task1, task2);

            // Assert
            var result1 = task1.Result;
            var result2 = task2.Result;
            Assert.True(compilingOne);
            Assert.True(compilingTwo);
        }

        [Fact]
        public async Task GetOrAdd_DoesNotCreateMultipleCompilationResults_ForConcurrentInvocations()
        {
            // Arrange
            var waitDuration = TimeSpan.FromSeconds(20);
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var resetEvent1 = new ManualResetEvent(initialState: false);
            var resetEvent2 = new ManualResetEvent(initialState: false);
            var cache = new CompilerCache(fileProvider);

            // Act
            var task1 = Task.Run(() =>
            {
                return cache.GetOrAdd(ViewPath, file =>
                {
                    // Event 2
                    resetEvent1.WaitOne(waitDuration);

                    // Event 3
                    resetEvent2.Set();
                    return new CompilationResult(typeof(TestView));
                });
            });

            var task2 = Task.Run(() =>
            {
                // Event 4
                resetEvent2.WaitOne(waitDuration);
                return cache.GetOrAdd(ViewPath, ThrowsIfCalled);
            });

            // Event 1
            resetEvent1.Set();
            await Task.WhenAll(task1, task2);

            // Assert
            var result1 = task1.Result;
            var result2 = task2.Result;
            Assert.Same(result1.CompilationResult.CompiledType, result2.CompilationResult.CompiledType);
        }

        private class TestView
        {
        }

        private class PreCompile
        {
        }

        public class DifferentView
        {
        }

        private CompilationResult ThrowsIfCalled(RelativeFileInfo file)
        {
            throw new Exception("Shouldn't be called");
        }
    }
}