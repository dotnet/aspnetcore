// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DefaultRoslynCompilationServiceTest
    {
        [Fact]
        public void Compile_ReturnsCompilationResult()
        {
            // Arrange
            var content = @"
public class MyTestType  {}";

            var compilationService = new DefaultRoslynCompilationService(
                GetDependencyContext(),
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
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(viewPath, fileContent);

            var compilationService = new DefaultRoslynCompilationService(
                GetDependencyContext(),
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

            var compilationService = new DefaultRoslynCompilationService(
                GetDependencyContext(),
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

            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.CreateReadStream())
                .Throws(new Exception());
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, mockFileInfo.Object);

            var compilationService = new DefaultRoslynCompilationService(
                GetDependencyContext(),
                GetOptions(),
                GetFileProviderAccessor(),
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

            var options = GetOptions();
            options.ParseOptions = options.ParseOptions.WithPreprocessorSymbols("MY_CUSTOM_DEFINE");

            var compilationService = new DefaultRoslynCompilationService(
                 GetDependencyContext(),
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
        public void GetCompilationFailedResult_ReturnsCompilationResult_WithGroupedMessages()
        {
            // Arrange
            var viewPath = "Views/Home/Index";
            var generatedCodeFileName = "Generated Code";
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(viewPath, "view-content");
            var options = new RazorViewEngineOptions();
            options.FileProviders.Add(fileProvider);

            var compilationService = new DefaultRoslynCompilationService(
                 GetDependencyContext(),
                 options,
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
            RoslynCompilationContext usedCompilation = null;

            var compilationService = new DefaultRoslynCompilationService(
                 GetDependencyContext(),
                 GetOptions(callback: c => usedCompilation = c),
                 GetFileProviderAccessor(),
                 NullLoggerFactory.Instance);

            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            Assert.NotNull(usedCompilation);
            Assert.Single(usedCompilation.Compilation.SyntaxTrees);
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

        private static RazorViewEngineOptions GetOptions(Action<RoslynCompilationContext> callback = null)
        {
            return new RazorViewEngineOptions
            {
                CompilationCallback = callback ?? (c => { }),
            };
        }

        private IRazorViewEngineFileProviderAccessor GetFileProviderAccessor(IFileProvider fileProvider = null)
        {
            var options = new Mock<IRazorViewEngineFileProviderAccessor>();
            options.SetupGet(o => o.FileProvider)
                .Returns(fileProvider ?? new TestFileProvider());

            return options.Object;
        }

        private DependencyContext GetDependencyContext()
        {
            var assembly = typeof(DefaultRoslynCompilationServiceTest).GetTypeInfo().Assembly;
            return DependencyContext.Load(assembly);
        }
    }
}
