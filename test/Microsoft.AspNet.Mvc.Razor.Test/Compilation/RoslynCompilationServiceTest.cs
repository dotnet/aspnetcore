// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.AspNet.FileProviders;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    public class RoslynCompilationServiceTest
    {
        [Fact]
        public void Compile_ReturnsUncachedCompilationResultWithCompiledContent()
        {
            // Arrange
            var content = @"
public class MyTestType  {}";
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryExporter = GetLibraryExporter();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationName,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions());
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns(string.Empty);

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryExporter,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost.Object,
                                                                  GetOptions());
            var relativeFileInfo = new RelativeFileInfo(new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            var uncachedResult = Assert.IsType<UncachedCompilationResult>(result);
            Assert.Equal("MyTestType", result.CompiledType.Name);
            Assert.Equal(content, uncachedResult.CompiledContent);
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
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryExporter = GetLibraryExporter();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationName,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions());
            var mvcRazorHost = Mock.Of<IMvcRazorHost>();
            var fileProvider = new TestFileProvider();
            var fileInfo = fileProvider.AddFile(viewPath, fileContent);

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryExporter,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost,
                                                                  GetOptions(fileProvider));
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
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryExporter = GetLibraryExporter();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationName,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions());
            var mvcRazorHost = Mock.Of<IMvcRazorHost>();

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryExporter,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost,
                                                                  GetOptions());
            var relativeFileInfo = new RelativeFileInfo(new TestFileInfo { Content = fileContent },
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
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryExporter = GetLibraryExporter();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationName,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions());
            var mvcRazorHost = Mock.Of<IMvcRazorHost>();

            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.CreateReadStream())
                        .Throws(new Exception());
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(path, mockFileInfo.Object);

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryExporter,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost,
                                                                  GetOptions(fileProvider));
            
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
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryExporter = GetLibraryExporter();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationName,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions { Defines = new[] { "MY_CUSTOM_DEFINE" } });
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns("My");

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryExporter,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost.Object,
                                                                  GetOptions());
            var relativeFileInfo = new RelativeFileInfo(new TestFileInfo { PhysicalPath = "SomePath" },
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
            var applicationEnvironment = GetApplicationEnvironment();
            var accessor = GetLoadContextAccessor();
            var libraryExporter = GetLibraryExporter();

            var compilerOptionsProvider = new Mock<ICompilerOptionsProvider>();
            compilerOptionsProvider.Setup(p => p.GetCompilerOptions(applicationEnvironment.ApplicationName,
                                                                    applicationEnvironment.RuntimeFramework,
                                                                    applicationEnvironment.Configuration))
                                   .Returns(new CompilerOptions());
            var mvcRazorHost = new Mock<IMvcRazorHost>();
            mvcRazorHost.SetupGet(m => m.MainClassNamePrefix)
                        .Returns("RazorPrefix");

            var compilationService = new RoslynCompilationService(applicationEnvironment,
                                                                  accessor,
                                                                  libraryExporter,
                                                                  compilerOptionsProvider.Object,
                                                                  mvcRazorHost.Object,
                                                                  GetOptions());

            var relativeFileInfo = new RelativeFileInfo(new TestFileInfo { PhysicalPath = "SomePath" },
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
            var options = new Mock<IOptions<RazorViewEngineOptions>>();
            options.SetupGet(o => o.Value)
                .Returns(new RazorViewEngineOptions
                {
                    FileProvider = fileProvider
                });
            var compilationService = new RoslynCompilationService(
                GetApplicationEnvironment(),
                GetLoadContextAccessor(),
                GetLibraryExporter(),
                Mock.Of<ICompilerOptionsProvider>(),
                Mock.Of<IMvcRazorHost>(),
                options.Object);

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

        private static ILibraryExporter GetLibraryExporter()
        {
            var fileReference = new Mock<IMetadataFileReference>();
            fileReference.SetupGet(f => f.Path)
                         .Returns(typeof(string).Assembly.Location);
            var libraryExport = new LibraryExport(fileReference.Object);

            var libraryExporter = new Mock<ILibraryExporter>();
            libraryExporter.Setup(l => l.GetAllExports(It.IsAny<string>()))
                          .Returns(libraryExport);
            return libraryExporter.Object;
        }

        private static IAssemblyLoadContextAccessor GetLoadContextAccessor()
        {
            var loadContext = new Mock<IAssemblyLoadContext>();
            loadContext.Setup(s => s.LoadStream(It.IsAny<Stream>(), It.IsAny<Stream>()))
                       .Returns((Stream stream, Stream pdb) =>
                       {
                           var memoryStream = (MemoryStream)stream;
                           return Assembly.Load(memoryStream.ToArray());
                       });

            var accessor = new Mock<IAssemblyLoadContextAccessor>();
            accessor.Setup(a => a.GetLoadContext(typeof(RoslynCompilationService).Assembly))
                    .Returns(loadContext.Object);
            return accessor.Object;
        }

        private IApplicationEnvironment GetApplicationEnvironment()
        {
            var applicationEnvironment = new Mock<IApplicationEnvironment>();
            applicationEnvironment.SetupGet(a => a.ApplicationName)
                                  .Returns("MyApp");
            applicationEnvironment.SetupGet(a => a.RuntimeFramework)
                                  .Returns(new FrameworkName("ASPNET", new Version(5, 0)));
            applicationEnvironment.SetupGet(a => a.Configuration)
                                  .Returns("Debug");
            applicationEnvironment.SetupGet(a => a.ApplicationBasePath)
                                  .Returns("MyBasePath");

            return applicationEnvironment.Object;
        }

        private static IOptions<RazorViewEngineOptions> GetOptions(IFileProvider fileProvider = null)
        {
            var razorViewEngineOptions = new RazorViewEngineOptions
            {
                FileProvider = fileProvider ?? new TestFileProvider()
            };
            var options = new Mock<IOptions<RazorViewEngineOptions>>();
            options.SetupGet(o => o.Value)
                .Returns(razorViewEngineOptions);

            return options.Object;
        }
    }
}