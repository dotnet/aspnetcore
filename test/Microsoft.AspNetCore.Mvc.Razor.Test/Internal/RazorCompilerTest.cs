// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    public class RazorCompilerTest
    {
        [Fact]
        public void GetCompilationFailedResult_ReadsRazorErrorsFromPage()
        {
            // Arrange
            var viewPath = "/Views/Home/Index.cshtml";
            var razorEngine = RazorEngine.Create();
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(viewPath, "<span name=\"@(User.Id\">");
            var razorProject = new DefaultRazorProject(fileProvider);

            var templateEngine = new MvcRazorTemplateEngine(razorEngine, razorProject);
            var compiler = new RazorCompiler(
                Mock.Of<ICompilationService>(),
                GetCompilerCacheProvider(fileProvider),
                templateEngine);
            var codeDocument = templateEngine.CreateCodeDocument(viewPath);

            // Act
            var csharpDocument = templateEngine.GenerateCode(codeDocument);
            var compilationResult = compiler.GetCompilationFailedResult(codeDocument, csharpDocument.Diagnostics);

            // Assert
            var failure = Assert.Single(compilationResult.CompilationFailures);
            Assert.Equal(viewPath, failure.SourceFilePath);
            Assert.Collection(failure.Messages,
                message => Assert.StartsWith(
                    @"Unterminated string literal.",
                    message.Message),
                message => Assert.StartsWith(
                    @"The explicit expression block is missing a closing "")"" character.",
                    message.Message));
        }

        [Fact]
        public void GetCompilationFailedResult_UsesPhysicalPath()
        {
            // Arrange
            var viewPath = "/Views/Home/Index.cshtml";
            var physicalPath = @"x:\myapp\views\home\index.cshtml";
            var razorEngine = RazorEngine.Create();
            var fileProvider = new TestFileProvider();
            var file = fileProvider.AddFile(viewPath, "<span name=\"@(User.Id\">");
            file.PhysicalPath = physicalPath;
            var razorProject = new DefaultRazorProject(fileProvider);

            var templateEngine = new MvcRazorTemplateEngine(razorEngine, razorProject);
            var compiler = new RazorCompiler(
                Mock.Of<ICompilationService>(),
                GetCompilerCacheProvider(fileProvider),
                templateEngine);
            var codeDocument = templateEngine.CreateCodeDocument(viewPath);

            // Act
            var csharpDocument = templateEngine.GenerateCode(codeDocument);
            var compilationResult = compiler.GetCompilationFailedResult(codeDocument, csharpDocument.Diagnostics);

            // Assert
            var failure = Assert.Single(compilationResult.CompilationFailures);
            Assert.Equal(physicalPath, failure.SourceFilePath);
        }

        [Fact]
        public void GetCompilationFailedResult_ReadsContentFromSourceDocuments()
        {
            // Arrange
            var viewPath = "/Views/Home/Index.cshtml";
            var fileContent =
@"
@if (User.IsAdmin)
{
    <span>
}
</span>";

            var razorEngine = RazorEngine.Create();
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(viewPath, fileContent);
            var razorProject = new DefaultRazorProject(fileProvider);

            var templateEngine = new MvcRazorTemplateEngine(razorEngine, razorProject);
            var compiler = new RazorCompiler(
                Mock.Of<ICompilationService>(),
                GetCompilerCacheProvider(fileProvider),
                templateEngine);
            var codeDocument = templateEngine.CreateCodeDocument(viewPath);

            // Act
            var csharpDocument = templateEngine.GenerateCode(codeDocument);
            var compilationResult = compiler.GetCompilationFailedResult(codeDocument, csharpDocument.Diagnostics);

            // Assert
            var failure = Assert.Single(compilationResult.CompilationFailures);
            Assert.Equal(fileContent, failure.SourceFileContent);
        }

        [Fact]
        public void GetCompilationFailedResult_ReadsContentFromImports()
        {
            // Arrange
            var viewPath = "/Views/Home/Index.cshtml";
            var importsFilePath = @"x:\views\_MyImports.cshtml";
            var fileContent = "@ ";
            var importsContent = "@(abc";

            var razorEngine = RazorEngine.Create();
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(viewPath, fileContent);
            var importsFile = fileProvider.AddFile("/Views/_MyImports.cshtml", importsContent);
            importsFile.PhysicalPath = importsFilePath;
            var razorProject = new DefaultRazorProject(fileProvider);

            var templateEngine = new MvcRazorTemplateEngine(razorEngine, razorProject)
            {
                Options =
                {
                    ImportsFileName = "_MyImports.cshtml",
                }
            };
            var compiler = new RazorCompiler(
                Mock.Of<ICompilationService>(),
                GetCompilerCacheProvider(fileProvider),
                templateEngine);
            var codeDocument = templateEngine.CreateCodeDocument(viewPath);

            // Act
            var csharpDocument = templateEngine.GenerateCode(codeDocument);
            var compilationResult = compiler.GetCompilationFailedResult(codeDocument, csharpDocument.Diagnostics);

            // Assert
            Assert.Collection(
                compilationResult.CompilationFailures,
                failure =>
                {
                    Assert.Equal(viewPath, failure.SourceFilePath);
                    Assert.Collection(failure.Messages,
                        message =>
                        {
                            Assert.Equal(@"A space or line break was encountered after the ""@"" character.  Only valid identifiers, keywords, comments, ""("" and ""{"" are valid at the start of a code block and they must occur immediately following ""@"" with no space in between.",
                                message.Message);
                        });
                },
                failure =>
                {
                    Assert.Equal(importsFilePath, failure.SourceFilePath);
                    Assert.Collection(failure.Messages,
                        message =>
                        {
                            Assert.Equal(@"The explicit expression block is missing a closing "")"" character.  Make sure you have a matching "")"" character for all the ""("" characters within this block, and that none of the "")"" characters are being interpreted as markup.",
                            message.Message);
                        });
                });
        }

        [Fact]
        public void GetCompilationFailedResult_GroupsMessages()
        {
            // Arrange
            var viewPath = "views/index.razor";
            var viewImportsPath = "views/global.import.cshtml";
            var codeDocument = RazorCodeDocument.Create(
                Create(viewPath, "View Content"),
                new[] { Create(viewImportsPath, "Global Import Content") });
            var diagnostics = new[]
            {
                GetRazorDiagnostic("message-1", new SourceLocation(1, 2, 17), length: 1),
                GetRazorDiagnostic("message-2", new SourceLocation(viewPath, 1, 4, 6), length: 7),
                GetRazorDiagnostic("message-3", SourceLocation.Undefined, length: -1),
                GetRazorDiagnostic("message-4", new SourceLocation(viewImportsPath, 1, 3, 8), length: 4),
            };
            var fileProvider = new TestFileProvider();
            var compiler = new RazorCompiler(
                Mock.Of<ICompilationService>(),
                GetCompilerCacheProvider(fileProvider),
                new MvcRazorTemplateEngine(RazorEngine.Create(), new DefaultRazorProject(fileProvider)));

            // Act
            var result = compiler.GetCompilationFailedResult(codeDocument, diagnostics);

            // Assert
            Assert.Collection(result.CompilationFailures,
            failure =>
            {
                Assert.Equal(viewPath, failure.SourceFilePath);
                Assert.Equal("View Content", failure.SourceFileContent);
                Assert.Collection(failure.Messages,
                    message =>
                    {
                        Assert.Equal(diagnostics[0].GetMessage(), message.Message);
                        Assert.Equal(viewPath, message.SourceFilePath);
                        Assert.Equal(3, message.StartLine);
                        Assert.Equal(17, message.StartColumn);
                        Assert.Equal(3, message.EndLine);
                        Assert.Equal(18, message.EndColumn);
                    },
                    message =>
                    {
                        Assert.Equal(diagnostics[1].GetMessage(), message.Message);
                        Assert.Equal(viewPath, message.SourceFilePath);
                        Assert.Equal(5, message.StartLine);
                        Assert.Equal(6, message.StartColumn);
                        Assert.Equal(5, message.EndLine);
                        Assert.Equal(13, message.EndColumn);
                    },
                    message =>
                    {
                        Assert.Equal(diagnostics[2].GetMessage(), message.Message);
                        Assert.Equal(viewPath, message.SourceFilePath);
                        Assert.Equal(0, message.StartLine);
                        Assert.Equal(-1, message.StartColumn);
                        Assert.Equal(0, message.EndLine);
                        Assert.Equal(-2, message.EndColumn);
                    });
            },
            failure =>
            {
                Assert.Equal(viewImportsPath, failure.SourceFilePath);
                Assert.Equal("Global Import Content", failure.SourceFileContent);
                Assert.Collection(failure.Messages,
                    message =>
                    {
                        Assert.Equal(diagnostics[3].GetMessage(), message.Message);
                        Assert.Equal(viewImportsPath, message.SourceFilePath);
                        Assert.Equal(4, message.StartLine);
                        Assert.Equal(8, message.StartColumn);
                        Assert.Equal(4, message.EndLine);
                        Assert.Equal(12, message.EndColumn);
                    });
            });
        }

        private ICompilerCacheProvider GetCompilerCacheProvider(TestFileProvider fileProvider)
        {
            var compilerCache = new CompilerCache(fileProvider);
            var compilerCacheProvider = new Mock<ICompilerCacheProvider>();
            compilerCacheProvider.SetupGet(p => p.Cache).Returns(compilerCache);

            return compilerCacheProvider.Object;
        }

        private static RazorSourceDocument Create(string path, string template)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(template));
            return RazorSourceDocument.ReadFrom(stream, path);
        }

        private static RazorDiagnostic GetRazorDiagnostic(string message, SourceLocation sourceLocation, int length)
        {
            var diagnosticDescriptor = new RazorDiagnosticDescriptor("test-id", () => message, RazorDiagnosticSeverity.Error);
            var sourceSpan = new SourceSpan(sourceLocation, length);

            return RazorDiagnostic.Create(diagnosticDescriptor, sourceSpan);
        }
    }
}
