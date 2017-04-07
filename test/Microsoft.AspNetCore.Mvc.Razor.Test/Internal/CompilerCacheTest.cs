// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class CompilerCacheTest
    {
        private const string ViewPath = "/Views/Home/Index.cshtml";
        private const string PrecompiledViewsPath = "/Views/Home/Precompiled.cshtml";
        private static readonly string[] _viewImportsPath = new[]
        {
            "/Views/Home/_ViewImports.cshtml",
            "/Views/_ViewImports.cshtml",
            "/_ViewImports.cshtml",
        };
        private readonly IDictionary<string, Type> _precompiledViews = new Dictionary<string, Type>
        {
            { PrecompiledViewsPath, typeof(PreCompile) }
        };

        public static TheoryData ViewImportsPaths
        {
            get
            {
                var theoryData = new TheoryData<string>();
                foreach (var path in _viewImportsPath)
                {
                    theoryData.Add(path);
                }

                return theoryData;
            }
        }

        [Fact]
        public void GetOrAdd_ReturnsFileNotFoundResult_IfFileIsNotFoundInFileSystem()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(fileProvider);
            var compilerCacheContext = new CompilerCacheContext(
                new NotFoundProjectItem("", "/path"),
                Enumerable.Empty<RazorProjectItem>(),
                _ => throw new Exception("Shouldn't be called."));

            // Act
            var result = cache.GetOrAdd("/some/path", _ => compilerCacheContext);

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
            var expected = new CompilationResult(typeof(TestView));

            // Act
            var result = cache.GetOrAdd(ViewPath, CreateContextFactory(expected));

            // Assert
            Assert.True(result.Success);
            Assert.Equal(typeof(TestView), result.CompiledType);
            Assert.Equal(ViewPath, result.RelativePath);
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
            var expected = new CompilationResult(typeof(TestView));

            // Act - 1
            var result1 = cache.GetOrAdd(@"Areas\Finances\Views\Home\Index.cshtml", CreateContextFactory(expected));

            // Assert - 1
            Assert.Equal(typeof(TestView), result1.CompiledType);

            // Act - 2
            var result2 = cache.GetOrAdd(relativePath, ThrowsIfCalled);

            // Assert - 2
            Assert.Equal(typeof(TestView), result2.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsFailedCompilationResult_IfFileWasRemovedFromFileSystem()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var expected = new CompilationResult(typeof(TestView));
            var projectItem = new DefaultRazorProjectItem(fileInfo, "", ViewPath);
            var cacheContext = new CompilerCacheContext(projectItem, Enumerable.Empty<RazorProjectItem>(), _ => expected);

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => cacheContext);

            // Assert 1
            Assert.True(result1.Success);
            Assert.Equal(typeof(TestView), result1.CompiledType);

            // Act 2
            // Delete the file from the file system and set it's expiration token.
            cacheContext = new CompilerCacheContext(
                new NotFoundProjectItem("", ViewPath),
                Enumerable.Empty<RazorProjectItem>(),
                _ => throw new Exception("Shouldn't be called."));
            fileProvider.GetChangeToken(ViewPath).HasChanged = true;
            var result2 = cache.GetOrAdd(ViewPath, _ => cacheContext);

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
            var result1 = cache.GetOrAdd(ViewPath, CreateContextFactory(expected1));

            // Assert 1
            Assert.True(result1.Success);
            Assert.Equal(typeof(TestView), result1.CompiledType);

            // Act 2
            // Verify we're getting cached results.
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.True(result2.Success);
            Assert.Equal(typeof(TestView), result1.CompiledType);

            // Act 3
            fileProvider.GetChangeToken(ViewPath).HasChanged = true;
            var result3 = cache.GetOrAdd(ViewPath, CreateContextFactory(expected2));

            // Assert 3
            Assert.True(result3.Success);
            Assert.Equal(typeof(DifferentView), result3.CompiledType);
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
            var result1 = cache.GetOrAdd(ViewPath, CreateContextFactory(expected1));

            // Assert 1
            Assert.True(result1.Success);
            Assert.Equal(typeof(TestView), result1.CompiledType);

            // Act 2
            // Verify we're getting cached results.
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.True(result2.Success);
            Assert.Equal(typeof(TestView), result1.CompiledType);

            // Act 3
            fileProvider.GetChangeToken(globalImportPath).HasChanged = true;
            var result3 = cache.GetOrAdd(ViewPath, CreateContextFactory(expected2));

            // Assert 2
            Assert.True(result3.Success);
            Assert.Equal(typeof(DifferentView), result3.CompiledType);
        }

        [Fact]
        public void GetOrAdd_DoesNotQueryFileSystem_IfCachedFileTriggerWasNotSet()
        {
            // Arrange
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var expected = new CompilationResult(typeof(TestView));

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, CreateContextFactory(expected));

            // Assert 1
            Assert.True(result1.Success);
            Assert.Equal(typeof(TestView), result1.CompiledType);

            // Act 2
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.True(result2.Success);
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
            Assert.Equal(typeof(PreCompile), result.CompiledType);
            Assert.Same(PrecompiledViewsPath, result.RelativePath);
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
            Assert.True(result.IsPrecompiled);
            Assert.Equal(typeof(PreCompile), result.CompiledType);
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
            Assert.Equal(typeof(PreCompile), result.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsRuntimeCompiled()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider, _precompiledViews);
            var expected = new CompilationResult(typeof(TestView));

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, CreateContextFactory(expected));

            // Assert 1
            Assert.Equal(typeof(TestView), result1.CompiledType);

            // Act 2
            var result2 = cache.GetOrAdd(ViewPath, ThrowsIfCalled);

            // Assert 2
            Assert.True(result2.Success);
            Assert.Equal(typeof(TestView), result2.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsPrecompiledViews()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(fileProvider, _precompiledViews);
            var expected = new CompilationResult(typeof(TestView));

            // Act
            var result1 = cache.GetOrAdd(PrecompiledViewsPath, ThrowsIfCalled);

            // Assert
            Assert.Equal(typeof(PreCompile), result1.CompiledType);
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
            Assert.Equal(typeof(PreCompile), result.CompiledType);
            Assert.Equal(viewPath, result.RelativePath);
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
            Assert.Equal(typeof(PreCompile), result.CompiledType);
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

            Func<CompilerCacheContext, CompilationResult> compile1 = _ =>
            {
                compilingOne = true;

                // Event 2
                Assert.True(resetEvent1.WaitOne(waitDuration));

                // Event 3
                Assert.True(resetEvent2.Set());

                // Event 6
                Assert.True(resetEvent1.WaitOne(waitDuration));

                Assert.True(compilingTwo);
                return new CompilationResult(typeof(TestView));
            };

            Func<CompilerCacheContext, CompilationResult> compile2 = _ =>
            {
                compilingTwo = true;

                // Event 4
                Assert.True(resetEvent2.WaitOne(waitDuration));

                // Event 5
                Assert.True(resetEvent1.Set());

                Assert.True(compilingOne);
                return new CompilationResult(typeof(DifferentView));
            };


            // Act
            var task1 = Task.Run(() =>
            {
                return cache.GetOrAdd("/Views/Home/Index.cshtml", path =>
                {
                    var projectItem = new DefaultRazorProjectItem(new TestFileInfo(), "", path);
                    return new CompilerCacheContext(projectItem, Enumerable.Empty<RazorProjectItem>(), compile1);
                });
            });

            var task2 = Task.Run(() =>
            {
                // Event 4
                return cache.GetOrAdd("/Views/Home/About.cshtml", path =>
                {
                    var projectItem = new DefaultRazorProjectItem(new TestFileInfo(), "", path);
                    return new CompilerCacheContext(projectItem, Enumerable.Empty<RazorProjectItem>(), compile2);
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

            Func<CompilerCacheContext, CompilationResult> compile = _ =>
            {
                // Event 2
                resetEvent1.WaitOne(waitDuration);

                // Event 3
                resetEvent2.Set();
                return new CompilationResult(typeof(TestView));
            };

            // Act
            var task1 = Task.Run(() =>
            {
                return cache.GetOrAdd(ViewPath, path =>
                {
                    var projectItem = new DefaultRazorProjectItem(new TestFileInfo(), "", path);
                    return new CompilerCacheContext(projectItem, Enumerable.Empty<RazorProjectItem>(), compile);
                });
            });

            var task2 = Task.Run(() =>
            {
                // Event 4
                Assert.True(resetEvent2.WaitOne(waitDuration));
                return cache.GetOrAdd(ViewPath, ThrowsIfCalled);
            });

            // Event 1
            resetEvent1.Set();
            await Task.WhenAll(task1, task2);

            // Assert
            var result1 = task1.Result;
            var result2 = task2.Result;
            Assert.Same(result1.CompiledType, result2.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ThrowsIfNullFileProvider()
        {
            // Arrange
            var expected =
                $"'{typeof(RazorViewEngineOptions).FullName}.{nameof(RazorViewEngineOptions.FileProviders)}' must " +
                $"not be empty. At least one '{typeof(IFileProvider).FullName}' is required to locate a view for " +
                "rendering.";
            var fileProvider = new NullFileProvider();
            var cache = new CompilerCache(fileProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => cache.GetOrAdd(ViewPath, _ => { throw new InvalidTimeZoneException(); }));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public void GetOrAdd_CachesCompilationExceptions()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var exception = new InvalidTimeZoneException();

            // Act and Assert - 1
            var actual = Assert.Throws<InvalidTimeZoneException>(() =>
                cache.GetOrAdd(ViewPath, _ => ThrowsIfCalled(ViewPath, exception)));
            Assert.Same(exception, actual);

            // Act and Assert - 2
            actual = Assert.Throws<InvalidTimeZoneException>(() => cache.GetOrAdd(ViewPath, ThrowsIfCalled));
            Assert.Same(exception, actual);
        }

        [Fact]
        public void GetOrAdd_ReturnsSuccessfulCompilationResultIfTriggerExpires()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var changeToken = fileProvider.AddChangeToken(ViewPath);
            var cache = new CompilerCache(fileProvider);

            // Act and Assert - 1
            Assert.Throws<InvalidTimeZoneException>(() =>
                cache.GetOrAdd(ViewPath, _ => { throw new InvalidTimeZoneException(); }));

            // Act - 2
            changeToken.HasChanged = true;
            var result = cache.GetOrAdd(ViewPath, CreateContextFactory(new CompilationResult(typeof(TestView))));

            // Assert - 2
            Assert.Same(typeof(TestView), result.CompiledType);
        }

        [Fact]
        public void GetOrAdd_CachesExceptionsInCompilationResult()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(fileProvider);
            var diagnosticMessages = new[]
            {
                new DiagnosticMessage("message", "message", ViewPath, 1, 1, 1, 1)
            };
            var compilationResult = new CompilationResult(new[]
            {
                new CompilationFailure(ViewPath, "some content", "compiled content", diagnosticMessages)
            });
            var context = CreateContextFactory(compilationResult);

            // Act and Assert - 1
            var ex = Assert.Throws<CompilationFailedException>(() => cache.GetOrAdd(ViewPath, context));
            Assert.Same(compilationResult.CompilationFailures, ex.CompilationFailures);

            // Act and Assert - 2
            ex = Assert.Throws<CompilationFailedException>(() => cache.GetOrAdd(ViewPath, ThrowsIfCalled));
            Assert.Same(compilationResult.CompilationFailures, ex.CompilationFailures);
        }

        private class TestView : RazorPage
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private class PreCompile : RazorPage
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        public class DifferentView : RazorPage
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }

        private CompilerCacheContext ThrowsIfCalled(string path) =>
            ThrowsIfCalled(path, new Exception("Shouldn't be called"));

        private CompilerCacheContext ThrowsIfCalled(string path, Exception exception)
        {
            exception = exception ?? new Exception("Shouldn't be called");
            var projectItem = new DefaultRazorProjectItem(new TestFileInfo(), "", path);

            return new CompilerCacheContext(
                projectItem,
                Enumerable.Empty<RazorProjectItem>(),
                 _ => throw exception);
        }

        private Func<string, CompilerCacheContext> CreateContextFactory(CompilationResult compile)
        {
            return path => CreateCacheContext(compile, path);
        }

        private CompilerCacheContext CreateCacheContext(CompilationResult compile, string path = ViewPath)
        {
            var projectItem = new DefaultRazorProjectItem(new TestFileInfo(), "", path);

            var imports = new List<RazorProjectItem>();
            foreach (var importFilePath in _viewImportsPath)
            {
                var importProjectItem = new DefaultRazorProjectItem(new TestFileInfo(), "", importFilePath);

                imports.Add(importProjectItem);
            }

            return new CompilerCacheContext(projectItem, imports, _ => compile);
        }
    }
}