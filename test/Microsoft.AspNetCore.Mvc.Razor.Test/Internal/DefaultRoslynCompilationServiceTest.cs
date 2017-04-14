// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class DefaultRoslynCompilationServiceTest
    {
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
            var compilationService = GetRoslynCompilationService();

            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", "test.cshtml"));

            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = content
            };

            // Act
            var result = compilationService.Compile(codeDocument, csharpDocument);

            // Assert
            Assert.Equal("MyTestType", result.CompiledType.Name);
            Assert.Null(result.CompilationFailures);
        }

        [Fact]
        public void Compile_ReturnsCompilationResult()
        {
            // Arrange
            var content = @"
public class MyTestType  {}";

            var compilationService = GetRoslynCompilationService();

            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", "test.cshtml"));

            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = content
            };

            // Act
            var result = compilationService.Compile(codeDocument, csharpDocument);

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

            var compilationService = GetRoslynCompilationService();
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create(fileContent, viewPath));

            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = content
            };

            // Act
            var result = compilationService.Compile(codeDocument, csharpDocument);

            // Assert
            Assert.IsType<CompilationResult>(result);
            Assert.Null(result.CompiledType);
            var compilationFailure = Assert.Single(result.CompilationFailures);
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

            var compilationService = GetRoslynCompilationService();
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create(fileContent, viewPath));

            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = content
            };

            // Act
            var result = compilationService.Compile(codeDocument, csharpDocument);

            // Assert
            Assert.IsType<CompilationResult>(result);
            Assert.Null(result.CompiledType);

            var compilationFailure = Assert.Single(result.CompilationFailures);
            Assert.Equal("Generated Code", compilationFailure.SourceFilePath);
            Assert.Equal(content, compilationFailure.SourceFileContent);
        }

        [Fact]
        public void Compile_UsesApplicationsCompilationSettings_ForParsingAndCompilation()
        {
            // Arrange
            var viewPath = "some-relative-path";
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
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", viewPath));

            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = content
            };

            // Act
            var result = compilationService.Compile(codeDocument, csharpDocument);

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
            var compilationService = GetRoslynCompilationService();
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("view-content", viewPath));
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
                codeDocument,
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

            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", "some-relative-path"));

            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = content
            };

            // Act
            var result = compilationService.Compile(codeDocument, csharpDocument);

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
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", "some-relative-path.cshtml"));

            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = content
            };

            // Act
            var result = compilationService.Compile(codeDocument, csharpDocument);

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

            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("Hello world", "some-relative-path.cshtml"));

            var csharpDocument = new RazorCSharpDocument()
            {
                GeneratedCode = content
            };

            // Act
            var result = compilationService.Compile(codeDocument, csharpDocument);

            // Assert
            Assert.Null(result.CompilationFailures);
            Assert.NotNull(result.CompiledType);
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
            RazorViewEngineOptions options = null)
        {
            partManager = partManager ?? GetApplicationPartManager();
            options = options ?? GetOptions();
            var optionsAccessor = GetAccessor(options);
            var referenceManager = new RazorReferenceManager(partManager, optionsAccessor);
            var compiler = new CSharpCompiler(referenceManager, optionsAccessor);

            return new DefaultRoslynCompilationService(
                compiler,
                optionsAccessor,
                NullLoggerFactory.Instance);
        }
    }
}
