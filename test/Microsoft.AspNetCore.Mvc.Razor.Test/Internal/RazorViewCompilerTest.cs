// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class RazorViewCompilerTest
    {
        [Fact]
        public void Constructor_ThrowsIfMultiplePrecompiledViewsHavePathsDifferingOnlyInCase()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var precompiledViews = new[]
            {
                new CompiledViewDescriptor { RelativePath = "/Views/Home/About.cshtml" },
                new CompiledViewDescriptor { RelativePath = "/Views/home/About.cshtml" },
            };
            var message = string.Join(
                Environment.NewLine,
                "The following precompiled view paths differ only in case, which is not supported:",
                precompiledViews[0].RelativePath,
                precompiledViews[1].RelativePath);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => GetViewCompiler(fileProvider, precompiledViews: precompiledViews));
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public async Task CompileAsync_ReturnsResultWithNullAttribute_IfFileIsNotFoundInFileSystem()
        {
            // Arrange
            var path = "/file/does-not-exist";
            var fileProvider = new TestFileProvider();
            var viewCompiler = GetViewCompiler(fileProvider);

            // Act
            var result1 = await viewCompiler.CompileAsync(path);
            var result2 = await viewCompiler.CompileAsync(path);

            // Assert
            Assert.Same(result1, result2);
            Assert.Null(result1.ViewAttribute);
            Assert.Collection(result1.ExpirationTokens,
                token => Assert.Equal(fileProvider.GetChangeToken(path), token));
        }

        [Fact]
        public async Task CompileAsync_AddsChangeTokensForViewStartsIfFileExists()
        {
            // Arrange
            var path = "/file/exists/FilePath.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, "Content");
            var viewCompiler = GetViewCompiler(fileProvider);

            // Act
            var result = await viewCompiler.CompileAsync(path);

            // Assert
            Assert.NotNull(result.ViewAttribute);
            Assert.Collection(result.ExpirationTokens,
                token => Assert.Same(fileProvider.GetChangeToken(path), token),
                token => Assert.Same(fileProvider.GetChangeToken("/file/exists/_ViewImports.cshtml"), token),
                token => Assert.Same(fileProvider.GetChangeToken("/file/_ViewImports.cshtml"), token),
                token => Assert.Same(fileProvider.GetChangeToken("/_ViewImports.cshtml"), token));
        }

        [Theory]
        [InlineData("/Areas/Finances/Views/Home/Index.cshtml")]
        [InlineData(@"Areas\Finances\Views\Home\Index.cshtml")]
        [InlineData(@"\Areas\Finances\Views\Home\Index.cshtml")]
        [InlineData(@"\Areas\Finances\Views/Home\Index.cshtml")]
        public async Task CompileAsync_NormalizesPathSepartorForPaths(string relativePath)
        {
            // Arrange
            var viewPath = "/Areas/Finances/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(viewPath, "some content");
            var viewCompiler = GetViewCompiler(fileProvider);

            // Act - 1
            var result1 = await viewCompiler.CompileAsync(@"Areas\Finances\Views\Home\Index.cshtml");

            // Act - 2
            viewCompiler.Compile = _ => throw new Exception("Can't call me");
            var result2 = await viewCompiler.CompileAsync(relativePath);

            // Assert - 2
            Assert.Same(result1, result2);
        }

        [Fact]
        public async Task CompileAsync_InvalidatesCache_IfChangeTokenExpires()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(path, "some content");
            var viewCompiler = GetViewCompiler(fileProvider);

            // Act 1
            var result1 = await viewCompiler.CompileAsync(path);

            // Assert 1
            Assert.NotNull(result1.ViewAttribute);

            // Act 2
            fileProvider.DeleteFile(path);
            fileProvider.GetChangeToken(path).HasChanged = true;
            viewCompiler.Compile = _ => throw new Exception("Can't call me");
            var result2 = await viewCompiler.CompileAsync(path);

            // Assert 2
            Assert.NotSame(result1, result2);
            Assert.Null(result2.ViewAttribute);
        }

        [Fact]
        public async Task CompileAsync_ReturnsNewResultIfFileWasModified()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(path, "some content");
            var viewCompiler = GetViewCompiler(fileProvider);
            var expected2 = new CompiledViewDescriptor();

            // Act 1
            var result1 = await viewCompiler.CompileAsync(path);

            // Assert 1
            Assert.NotNull(result1.ViewAttribute);

            // Act 2
            fileProvider.GetChangeToken(path).HasChanged = true;
            viewCompiler.Compile = _ => expected2;
            var result2 = await viewCompiler.CompileAsync(path);

            // Assert 2
            Assert.NotSame(result1, result2);
            Assert.Same(expected2, result2);
        }

        [Fact]
        public async Task CompileAsync_ReturnsNewResult_IfAncestorViewImportsWereModified()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(path, "some content");
            var viewCompiler = GetViewCompiler(fileProvider);
            var expected2 = new CompiledViewDescriptor();

            // Act 1
            var result1 = await viewCompiler.CompileAsync(path);

            // Assert 1
            Assert.NotNull(result1.ViewAttribute);

            // Act 2
            fileProvider.GetChangeToken("/Views/_ViewImports.cshtml").HasChanged = true;
            viewCompiler.Compile = _ => expected2;
            var result2 = await viewCompiler.CompileAsync(path);

            // Assert 2
            Assert.NotSame(result1, result2);
            Assert.Same(expected2, result2);
        }

        [Fact]
        public async Task CompileAsync_ReturnsPrecompiledViews()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(path, "some content");
            var precompiledView = new CompiledViewDescriptor
            {
                RelativePath = path,
            };
            var viewCompiler = GetViewCompiler(fileProvider, precompiledViews: new[] { precompiledView });

            // Act
            var result = await viewCompiler.CompileAsync(path);

            // Assert
            Assert.Same(precompiledView, result);
        }

        [Theory]
        [InlineData("/views/home/index.cshtml")]
        [InlineData("/VIEWS/HOME/INDEX.CSHTML")]
        [InlineData("/viEws/HoME/inDex.cshtml")]
        public async Task CompileAsync_PerformsCaseInsensitiveLookupsForPrecompiledViews(string lookupPath)
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(path, "some content");
            var precompiledView = new CompiledViewDescriptor
            {
                RelativePath = path,
            };
            var viewCompiler = GetViewCompiler(fileProvider, precompiledViews: new[] { precompiledView });

            // Act
            var result = await viewCompiler.CompileAsync(lookupPath);

            // Assert
            Assert.Same(precompiledView, result);
        }

        [Fact]
        public async Task CompileAsync_PerformsCaseInsensitiveLookupsForPrecompiledViews_WithNonNormalizedPaths()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(path, "some content");
            var precompiledView = new CompiledViewDescriptor
            {
                RelativePath = path,
            };
            var viewCompiler = GetViewCompiler(fileProvider, precompiledViews: new[] { precompiledView });

            // Act
            var result = await viewCompiler.CompileAsync("Views\\Home\\Index.cshtml");

            // Assert
            Assert.Same(precompiledView, result);
        }

        [Fact]
        public async Task CompileAsync_DoesNotRecompile_IfFileTriggerWasSetForPrecompiledView()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(path, "some content");
            var precompiledView = new CompiledViewDescriptor
            {
                RelativePath = path,
            };
            var viewCompiler = GetViewCompiler(fileProvider, precompiledViews: new[] { precompiledView });

            // Act
            fileProvider.Watch(path);
            fileProvider.GetChangeToken(path).HasChanged = true;
            var result = await viewCompiler.CompileAsync(path);

            // Assert
            Assert.Same(precompiledView, result);
        }

        [Fact]
        public async Task GetOrAdd_AllowsConcurrentCompilationOfMultipleRazorPages()
        {
            // Arrange
            var path1 = "/Views/Home/Index.cshtml";
            var path2 = "/Views/Home/About.cshtml";
            var waitDuration = TimeSpan.FromSeconds(20);
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path1, "Index content");
            fileProvider.AddFile(path2, "About content");
            var resetEvent1 = new AutoResetEvent(initialState: false);
            var resetEvent2 = new ManualResetEvent(initialState: false);
            var cache = GetViewCompiler(fileProvider);
            var compilingOne = false;
            var compilingTwo = false;
            var result1 = new CompiledViewDescriptor();
            var result2 = new CompiledViewDescriptor();

            cache.Compile = path =>
            {
                if (path == path1)
                {
                    compilingOne = true;

                    // Event 2
                    Assert.True(resetEvent1.WaitOne(waitDuration));

                    // Event 3
                    Assert.True(resetEvent2.Set());

                    // Event 6
                    Assert.True(resetEvent1.WaitOne(waitDuration));

                    Assert.True(compilingTwo);

                    return result1;
                }
                else if (path == path2)
                {
                    compilingTwo = true;

                    // Event 4
                    Assert.True(resetEvent2.WaitOne(waitDuration));

                    // Event 5
                    Assert.True(resetEvent1.Set());

                    Assert.True(compilingOne);

                    return result2;
                }
                else
                {
                    throw new Exception();
                }
            };

            // Act
            var task1 = Task.Run(() => cache.CompileAsync(path1));
            var task2 = Task.Run(() => cache.CompileAsync(path2));

            // Event 1
            resetEvent1.Set();

            await Task.WhenAll(task1, task2);

            // Assert
            Assert.True(compilingOne);
            Assert.True(compilingTwo);
            Assert.Same(result1, task1.Result);
            Assert.Same(result2, task2.Result);
        }

        [Fact]
        public async Task CompileAsync_DoesNotCreateMultipleCompilationResults_ForConcurrentInvocations()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var waitDuration = TimeSpan.FromSeconds(20);
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, "some content");
            var resetEvent1 = new ManualResetEvent(initialState: false);
            var resetEvent2 = new ManualResetEvent(initialState: false);
            var compiler = GetViewCompiler(fileProvider);

            compiler.Compile = _ =>
            {
                // Event 2
                resetEvent1.WaitOne(waitDuration);

                // Event 3
                resetEvent2.Set();
                return new CompiledViewDescriptor();
            };

            // Act
            var task1 = Task.Run(() => compiler.CompileAsync(path));
            var task2 = Task.Run(() =>
            {
                // Event 4
                Assert.True(resetEvent2.WaitOne(waitDuration));
                return compiler.CompileAsync(path);
            });

            // Event 1
            resetEvent1.Set();
            await Task.WhenAll(task1, task2);

            // Assert
            var result1 = task1.Result;
            var result2 = task2.Result;
            Assert.Same(result1, result2);
        }

        [Fact]
        public async Task GetOrAdd_CachesCompilationExceptions()
        {
            // Arrange
            var path = "/Views/Home/Index.cshtml";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, "some content");
            var exception = new InvalidTimeZoneException();
            var compiler = GetViewCompiler(fileProvider);
            compiler.Compile = _ => throw exception;

            // Act and Assert - 1
            var actual = await Assert.ThrowsAsync<InvalidTimeZoneException>(
                () => compiler.CompileAsync(path));
            Assert.Same(exception, actual);

            // Act and Assert - 2
            compiler.Compile = _ => throw new Exception("Shouldn't be called");

            actual = await Assert.ThrowsAsync<InvalidTimeZoneException>(
                () => compiler.CompileAsync(path));
            Assert.Same(exception, actual);
        }

        [Fact]
        public void Compile_SucceedsForCSharp7()
        {
            // Arrange
            var content = @"
public class MyTestType
{
    private string _name;

    public string Name
    {
        get => _name;
        set => _name = value ?? throw new System.ArgumentNullException(nameof(value));
    }
}";
            var compiler = GetViewCompiler(new TestFileProvider());
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("razor-content", "filename"));

            // Act
            var result = compiler.CompileAndEmit(codeDocument, content);

            // Assert
            var exportedType = Assert.Single(result.ExportedTypes);
            Assert.Equal("MyTestType", exportedType.Name);
        }

        [Fact]
        public void Compile_ReturnsCompilationFailureWithPathsFromLinePragmas()
        {
            // Arrange
            var viewPath = "some-relative-path";
            var fileContent = "test file content";
            var content = $@"
#line 1 ""{viewPath}""
this should fail";

            var compiler = GetViewCompiler(new TestFileProvider());
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create(fileContent, viewPath));

            // Act & Assert
            var ex = Assert.Throws<CompilationFailedException>(() => compiler.CompileAndEmit(codeDocument, content));

            var compilationFailure = Assert.Single(ex.CompilationFailures);
            Assert.Equal(viewPath, compilationFailure.SourceFilePath);
            Assert.Equal(fileContent, compilationFailure.SourceFileContent);
        }

        [Fact]
        public void Compile_ReturnsGeneratedCodePath_IfLinePragmaIsNotAvailable()
        {
            // Arrange
            var viewPath = "some-relative-path";
            var fileContent = "file content";
            var content = "this should fail";

            var compiler = GetViewCompiler(new TestFileProvider());
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create(fileContent, viewPath));

            // Act & Assert
            var ex = Assert.Throws<CompilationFailedException>(() => compiler.CompileAndEmit(codeDocument, content));

            var compilationFailure = Assert.Single(ex.CompilationFailures);
            Assert.Equal("Generated Code", compilationFailure.SourceFilePath);
            Assert.Equal(content, compilationFailure.SourceFileContent);
        }

        [Fact]
        public void Compile_InvokessCallback()
        {
            // Arrange
            var content = "public class MyTestType  {}";
            var callbackInvoked = false;
            var compiler = GetViewCompiler(
                new TestFileProvider(),
                context =>
                {
                    callbackInvoked = true;
                });
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", "some-path"));

            // Act
            var result = compiler.CompileAndEmit(codeDocument, content);

            // Assert
            Assert.True(callbackInvoked);
        }

        [Fact]
        public void Compile_SucceedsIfReferencesAreAddedInCallback()
        {
            // Arrange
            Action<RoslynCompilationContext> compilationCallback = context =>
            {
                var assemblyLocation = typeof(object).Assembly.Location;

                context.Compilation = context
                    .Compilation
                    .AddReferences(MetadataReference.CreateFromFile(assemblyLocation));
            };

            var applicationPartManager = new ApplicationPartManager();
            var referenceManager = new DefaultRazorReferenceManager(
                applicationPartManager,
                Options.Create(new RazorViewEngineOptions()));
            var compiler = GetViewCompiler(
                compilationCallback: compilationCallback,
                referenceManager: referenceManager);

            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", "some-relative-path.cshtml"));

            // Act
            var result = compiler.CompileAndEmit(codeDocument, "public class Test {}");

            // Assert
            Assert.NotNull(result);
        }

        private static TestRazorViewCompiler GetViewCompiler(
            TestFileProvider fileProvider = null,
            Action<RoslynCompilationContext> compilationCallback = null,
            RazorReferenceManager referenceManager = null,
            IList<CompiledViewDescriptor> precompiledViews = null)
        {
            fileProvider = fileProvider ?? new TestFileProvider();
            var accessor = Mock.Of<IRazorViewEngineFileProviderAccessor>(a => a.FileProvider == fileProvider);

            compilationCallback = compilationCallback ?? (_ => { });
            var options = Options.Create(new RazorViewEngineOptions());
            if (referenceManager == null)
            {
                var applicationPartManager = new ApplicationPartManager();
                var assembly = typeof(RazorViewCompilerTest).Assembly;
                applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
                applicationPartManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());

                referenceManager = new DefaultRazorReferenceManager(applicationPartManager, options);
            }

            precompiledViews = precompiledViews ?? Array.Empty<CompiledViewDescriptor>();

            var projectSystem = new FileProviderRazorProject(accessor);
            var templateEngine = new RazorTemplateEngine(RazorEngine.Create(), projectSystem)
            {
                Options =
                {
                    ImportsFileName = "_ViewImports.cshtml",
                }
            };
            var viewCompiler = new TestRazorViewCompiler(
                fileProvider,
                templateEngine,
                new CSharpCompiler(referenceManager, Mock.Of<IHostingEnvironment>()),
                compilationCallback,
                precompiledViews);
            return viewCompiler;
        }

        private class TestRazorViewCompiler : RazorViewCompiler
        {
            public TestRazorViewCompiler(
                TestFileProvider fileProvider,
                RazorTemplateEngine templateEngine,
                CSharpCompiler csharpCompiler,
                Action<RoslynCompilationContext> compilationCallback,
                IList<CompiledViewDescriptor> precompiledViews,
                Func<string, CompiledViewDescriptor> compile = null) :
                base(fileProvider, templateEngine, csharpCompiler, compilationCallback, precompiledViews, NullLogger.Instance)
            {
                Compile = compile;
                if (Compile == null)
                {
                    Compile = path => new CompiledViewDescriptor
                    {
                        RelativePath = path,
                        ViewAttribute = new RazorViewAttribute(path, typeof(object)),
                    };
                }
            }

            public Func<string, CompiledViewDescriptor> Compile { get; set; }

            protected override CompiledViewDescriptor CompileAndEmit(string relativePath)
            {
                return Compile(relativePath);
            }
        }
    }
}