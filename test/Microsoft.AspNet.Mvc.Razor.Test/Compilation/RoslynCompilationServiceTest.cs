// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileProviders;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class RoslynCompilationServiceTest
    {
        private const string ConfigurationName = "Release";

        [Fact]
        public void Compile_ReturnsCompilationResult()
        {
            // Arrange
            var content = @"
public class MyTestType  {}";
            var applicationEnvironment = PlatformServices.Default.Application;
            var libraryExporter = CompilationServices.Default.LibraryExporter;
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns(string.Empty);

            var compilationService = new RoslynCompilationService(
                applicationEnvironment,
                libraryExporter,
                mvcRazorHost.Object,
                GetOptions(),
                GetFileProviderAccessor(),
                NullLoggerFactory.Instance);
            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            Assert.Equal("MyTestType", result.CompiledType.Name);
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
            var applicationEnvironment = PlatformServices.Default.Application;
            var libraryExporter = CompilationServices.Default.LibraryExporter;
            var mvcRazorHost = Mock.Of<IMvcRazorHost>();
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(viewPath, fileContent);

            var compilationService = new RoslynCompilationService(
                applicationEnvironment,
                libraryExporter,
                mvcRazorHost,
                GetOptions(),
                GetFileProviderAccessor(fileProvider),
                NullLoggerFactory.Instance);
            var relativeFileInfo = new RelativeFileInfo(fileInfo, "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            Assert.IsType<CompilationResult>(result);
            Assert.Null(result.CompiledType);
            var compilationFailure = Assert.Single(result.CompilationFailures);
            Assert.Equal(relativeFileInfo.RelativePath, compilationFailure.SourceFilePath);
            Assert.Equal(fileContent, compilationFailure.SourceFileContent);
        }

        [Fact]
        public void Compile_ReturnsGeneratedCodePath_IfLinePragmaIsNotAvailable()
        {
            // Arrange
            var fileContent = "file content";
            var content = @"this should fail";
            var applicationEnvironment = PlatformServices.Default.Application;
            var libraryExporter = CompilationServices.Default.LibraryExporter;
            var mvcRazorHost = Mock.Of<IMvcRazorHost>();

            var compilationService = new RoslynCompilationService(
                applicationEnvironment,
                libraryExporter,
                mvcRazorHost,
                GetOptions(),
                GetFileProviderAccessor(),
                NullLoggerFactory.Instance);
            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { Content = fileContent },
                "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            Assert.IsType<CompilationResult>(result);
            Assert.Null(result.CompiledType);

            var compilationFailure = Assert.Single(result.CompilationFailures);
            Assert.Equal("Generated Code", compilationFailure.SourceFilePath);
            Assert.Equal(content, compilationFailure.SourceFileContent);
        }

        [Fact]
        public void Compile_DoesNotThrow_IfFileCannotBeRead()
        {
            // Arrange
            var path = "some-relative-path";
            var content = $@"
#line 1 ""{path}""
this should fail";
            var applicationEnvironment = PlatformServices.Default.Application;
            var libraryExporter = CompilationServices.Default.LibraryExporter;
            var mvcRazorHost = Mock.Of<IMvcRazorHost>();

            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.CreateReadStream())
                .Throws(new Exception());
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, mockFileInfo.Object);

            var compilationService = new RoslynCompilationService(
                applicationEnvironment,
                libraryExporter,
                mvcRazorHost,
                GetOptions(),
                GetFileProviderAccessor(fileProvider),
                NullLoggerFactory.Instance);

            var relativeFileInfo = new RelativeFileInfo(mockFileInfo.Object, path);

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            Assert.IsType<CompilationResult>(result);
            Assert.Null(result.CompiledType);
            var compilationFailure = Assert.Single(result.CompilationFailures);
            Assert.Equal(path, compilationFailure.SourceFilePath);
            Assert.Null(compilationFailure.SourceFileContent);
        }

        [Fact]
        public void Compile_UsesApplicationsCompilationSettings_ForParsingAndCompilation()
        {
            // Arrange
            var content = @"
#if MY_CUSTOM_DEFINE
public class MyCustomDefinedClass {}
#else
public class MyNonCustomDefinedClass {}
#endif
";
            var applicationEnvironment = PlatformServices.Default.Application;
            var libraryExporter = CompilationServices.Default.LibraryExporter;
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                .Returns("My");

            var options = GetOptions();
            options.Value.ParseOptions = options.Value.ParseOptions.WithPreprocessorSymbols("MY_CUSTOM_DEFINE");

            var compilationService = new RoslynCompilationService(
                applicationEnvironment,
                libraryExporter,
                mvcRazorHost.Object,
                options,
                GetFileProviderAccessor(),
                NullLoggerFactory.Instance);
            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            Assert.NotNull(result.CompiledType);
            Assert.Equal("MyCustomDefinedClass", result.CompiledType.Name);
        }

        [Fact]
        public void Compile_ReturnsSingleTypeThatStartsWithMainClassNamePrefix()
        {
            // Arrange
            var content = @"
public class RazorPrefixType  {}
public class NotRazorPrefixType {}";
            var applicationEnvironment = PlatformServices.Default.Application;
            var libraryExporter = CompilationServices.Default.LibraryExporter;
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns("RazorPrefix");

            var compilationService = new RoslynCompilationService(
                applicationEnvironment,
                libraryExporter,
                mvcRazorHost.Object,
                GetOptions(),
                GetFileProviderAccessor(),
                NullLoggerFactory.Instance);

            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            Assert.NotNull(result.CompiledType);
            Assert.Equal("RazorPrefixType", result.CompiledType.Name);
        }

        [Fact]
        public void GetCompilationFailedResult_ReturnsCompilationResult_WithGroupedMessages()
        {
            // Arrange
            var viewPath = "Views/Home/Index";
            var generatedCodeFileName = "Generated Code";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(viewPath, "view-content");
            var options = new RazorViewEngineOptions();
            options.FileProviders.Add(fileProvider);
            var optionsAccessor = new Mock<IOptions<RazorViewEngineOptions>>();
            optionsAccessor.SetupGet(o => o.Value)
                .Returns(options);
            var compilationService = new RoslynCompilationService(
                PlatformServices.Default.Application,
                CompilationServices.Default.LibraryExporter,
                Mock.Of<IMvcRazorHost>(),
                optionsAccessor.Object,
                GetFileProviderAccessor(fileProvider),
                NullLoggerFactory.Instance);

            var assemblyName = "random-assembly-name";

            var diagnostics = new[]
            {
                Diagnostic.Create(
                    GetDiagnosticDescriptor("message-1"),
                    Location.Create(
                        viewPath,
                        new TextSpan(10, 5),
                        new LinePositionSpan(new LinePosition(10, 1), new LinePosition(10, 2)))),
                Diagnostic.Create(
                    GetDiagnosticDescriptor("message-2"),
                    Location.Create(
                        assemblyName,
                        new TextSpan(1, 6),
                        new LinePositionSpan(new LinePosition(1, 2), new LinePosition(3, 4)))),
                Diagnostic.Create(
                    GetDiagnosticDescriptor("message-3"),
                    Location.Create(
                        viewPath,
                        new TextSpan(40, 50),
                        new LinePositionSpan(new LinePosition(30, 5), new LinePosition(40, 12)))),
            };

            // Act
            var compilationResult = compilationService.GetCompilationFailedResult(
                viewPath,
                "compilation-content",
                assemblyName,
                diagnostics);

            // Assert
            Assert.Collection(compilationResult.CompilationFailures,
                failure =>
                {
                    Assert.Equal(viewPath, failure.SourceFilePath);
                    Assert.Equal("view-content", failure.SourceFileContent);
                    Assert.Collection(failure.Messages,
                        message =>
                        {
                            Assert.Equal("message-1", message.Message);
                            Assert.Equal(viewPath, message.SourceFilePath);
                            Assert.Equal(11, message.StartLine);
                            Assert.Equal(2, message.StartColumn);
                            Assert.Equal(11, message.EndLine);
                            Assert.Equal(3, message.EndColumn);
                        },
                        message =>
                        {
                            Assert.Equal("message-3", message.Message);
                            Assert.Equal(viewPath, message.SourceFilePath);
                            Assert.Equal(31, message.StartLine);
                            Assert.Equal(6, message.StartColumn);
                            Assert.Equal(41, message.EndLine);
                            Assert.Equal(13, message.EndColumn);
                        });
                },
                failure =>
                {
                    Assert.Equal(generatedCodeFileName, failure.SourceFilePath);
                    Assert.Equal("compilation-content", failure.SourceFileContent);
                    Assert.Collection(failure.Messages,
                        message =>
                        {
                            Assert.Equal("message-2", message.Message);
                            Assert.Equal(assemblyName, message.SourceFilePath);
                            Assert.Equal(2, message.StartLine);
                            Assert.Equal(3, message.StartColumn);
                            Assert.Equal(4, message.EndLine);
                            Assert.Equal(5, message.EndColumn);
                        });
                });
        }

        [Fact]
        public void Compile_RunsCallback()
        {
            var content = "public class MyTestType  {}";
            var applicationEnvironment = PlatformServices.Default.Application;
            var libraryExporter = CompilationServices.Default.LibraryExporter;
            RoslynCompilationContext usedCompilation = null;
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns(string.Empty);

            var compilationService = new RoslynCompilationService(
                applicationEnvironment,
                libraryExporter,
                mvcRazorHost.Object,
                GetOptions(callback: c => usedCompilation = c),
                GetFileProviderAccessor(),
                NullLoggerFactory.Instance);

            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            Assert.NotNull(usedCompilation);
            Assert.NotNull(usedCompilation.Compilation);
            Assert.Equal(1, usedCompilation.Compilation.SyntaxTrees.Length);
        }

        private static DiagnosticDescriptor GetDiagnosticDescriptor(string messageFormat)
        {
            return new DiagnosticDescriptor(
                id: "someid",
                title: "sometitle",
                messageFormat: messageFormat,
                category: "some-category",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }

        private static IOptions<RazorViewEngineOptions> GetOptions(Action<RoslynCompilationContext> callback = null)
        {
            var razorViewEngineOptions = new RazorViewEngineOptions
            {
                CompilationCallback = callback ?? (c => { }),
            };
            var options = new Mock<IOptions<RazorViewEngineOptions>>();
            options
                .SetupGet(o => o.Value)
                .Returns(razorViewEngineOptions);

            return options.Object;
        }

        private IRazorViewEngineFileProviderAccessor GetFileProviderAccessor(IFileProvider fileProvider = null)
        {
            var options = new Mock<IRazorViewEngineFileProviderAccessor>();
            options.SetupGet(o => o.FileProvider)
                .Returns(fileProvider ?? new TestFileProvider());

            return options.Object;
        }
    }
}
