// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
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

            var compilationService = GetRoslynCompilationService();
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

            var compilationService = GetRoslynCompilationService(fileProvider: fileProvider);
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
            var content = "this should fail";

            var compilationService = GetRoslynCompilationService();
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

            var compilationService = GetRoslynCompilationService(fileProvider: fileProvider);
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
            var compilationService = GetRoslynCompilationService(options: options);
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
            var compilationService = GetRoslynCompilationService(options: options, fileProvider: fileProvider);
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
            // Arrange
            var content = "public class MyTestType  {}";
            RoslynCompilationContext usedCompilation = null;
            var options = GetOptions(c => usedCompilation = c);
            var compilationService = GetRoslynCompilationService(options: options);

            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            Assert.NotNull(usedCompilation);
            Assert.Single(usedCompilation.Compilation.SyntaxTrees);
        }

        [Fact]
        public void Compile_DoesNotThrowIfReferencesWereClearedInCallback()
        {
            // Arrange
            var options = GetOptions(context =>
            {
                context.Compilation = context.Compilation.RemoveAllReferences();
            });
            var content = "public class MyTestType  {}";
            var compilationService = GetRoslynCompilationService(options: options);
            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path.cshtml");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            Assert.Single(result.CompilationFailures);
        }

        [Fact]
        public void Compile_SucceedsIfReferencesAreAddedInCallback()
        {
            // Arrange
            var options = GetOptions(context =>
            {
                var assemblyLocation = typeof(object).GetTypeInfo().Assembly.Location;

                context.Compilation = context
                    .Compilation
                    .AddReferences(MetadataReference.CreateFromFile(assemblyLocation));
            });
            var content = "public class MyTestType  {}";
            var applicationPartManager = new ApplicationPartManager();
            var compilationService = GetRoslynCompilationService(applicationPartManager, options);

            var relativeFileInfo = new RelativeFileInfo(
                new TestFileInfo { PhysicalPath = "SomePath" },
                "some-relative-path.cshtml");

            // Act
            var result = compilationService.Compile(relativeFileInfo, content);

            // Assert
            Assert.Null(result.CompilationFailures);
            Assert.NotNull(result.CompiledType);
        }

        [Fact]
        public void GetCompilationReferences_CombinesApplicationPartAndOptionMetadataReferences()
        {
            // Arrange
            var options = new RazorViewEngineOptions();
            var objectAssemblyLocation = typeof(object).GetTypeInfo().Assembly.Location;
            var objectAssemblyMetadataReference = MetadataReference.CreateFromFile(objectAssemblyLocation);
            options.AdditionalCompilationReferences.Add(objectAssemblyMetadataReference);
            var applicationPartManager = GetApplicationPartManager();
            var compilationService = new TestRoslynCompilationService(applicationPartManager, options);
            var feature = new MetadataReferenceFeature();
            applicationPartManager.PopulateFeature(feature);
            var partReferences = feature.MetadataReferences;
            var expectedReferences = new List<MetadataReference>();
            expectedReferences.AddRange(partReferences);
            expectedReferences.Add(objectAssemblyMetadataReference);
            var expectedReferenceDisplays = expectedReferences.Select(reference => reference.Display);

            // Act
            var references = compilationService.GetCompilationReferencesPublic();
            var referenceDisplays = references.Select(reference => reference.Display);

            // Assert
            Assert.NotNull(references);
            Assert.NotEmpty(references);
            Assert.Equal(expectedReferenceDisplays, referenceDisplays);
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

        private static IRazorViewEngineFileProviderAccessor GetFileProviderAccessor(IFileProvider fileProvider = null)
        {
            var options = new Mock<IRazorViewEngineFileProviderAccessor>();
            options.SetupGet(o => o.FileProvider)
                .Returns(fileProvider ?? new TestFileProvider());

            return options.Object;
        }

        private static IOptions<RazorViewEngineOptions> GetAccessor(RazorViewEngineOptions options)
        {
            var optionsAccessor = new Mock<IOptions<RazorViewEngineOptions>>();
            optionsAccessor.SetupGet(a => a.Value).Returns(options);
            return optionsAccessor.Object;
        }

        private static ApplicationPartManager GetApplicationPartManager()
        {
            var applicationPartManager = new ApplicationPartManager();
            var assembly = typeof(DefaultRoslynCompilationServiceTest).GetTypeInfo().Assembly;
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
            applicationPartManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());

            return applicationPartManager;
        }

        private static DefaultRoslynCompilationService GetRoslynCompilationService(
            ApplicationPartManager partManager = null,
            RazorViewEngineOptions options = null,
            IFileProvider fileProvider = null)
        {
            partManager = partManager ?? GetApplicationPartManager();
            options = options ?? GetOptions();

            return new DefaultRoslynCompilationService(
                partManager,
                GetAccessor(options),
                GetFileProviderAccessor(fileProvider),
                NullLoggerFactory.Instance);
        }

        private class TestRoslynCompilationService : DefaultRoslynCompilationService
        {
            public TestRoslynCompilationService(ApplicationPartManager partManager, RazorViewEngineOptions options)
                : base(partManager, GetAccessor(options), GetFileProviderAccessor(), NullLoggerFactory.Instance)
            {
            }

            public IList<MetadataReference> GetCompilationReferencesPublic() => GetCompilationReferences();
        }
    }
}
