// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

public class CompilerFailedExceptionFactoryTest
{
    [Fact]
    public void GetCompilationFailedResult_ReadsRazorErrorsFromPage()
    {
        // Arrange
        var viewPath = "/Views/Home/Index.cshtml";

        var fileSystem = new VirtualRazorProjectFileSystem();
        var projectItem = new TestRazorProjectItem(viewPath, "<span name=\"@(User.Id\">");

        var razorEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem).Engine;
        var codeDocument = GetCodeDocument(projectItem);

        // Act
        razorEngine.Process(codeDocument);
        var csharpDocument = codeDocument.GetCSharpDocument();
        var compilationResult = CompilationFailedExceptionFactory.Create(codeDocument, csharpDocument.Diagnostics);

        // Assert
        var failure = Assert.Single(compilationResult.CompilationFailures);
        Assert.Equal(viewPath, failure.SourceFilePath);
        var orderedMessages = failure.Messages.OrderByDescending(messages => messages.Message);
        Assert.Collection(orderedMessages,
            message => Assert.StartsWith(
                @"Unterminated string literal.",
                message.Message),
            message => Assert.StartsWith(
                @"The explicit expression block is missing a closing "")"" character.",
                message.Message));
    }

    [Fact]
    public void GetCompilationFailedResult_WithMissingReferences()
    {
        // Arrange
        var expected = "One or more compilation references may be missing. If you're seeing this in a published application, set 'CopyRefAssembliesToPublishDirectory' to true in your project file to ensure files in the refs directory are published.";
        var compilation = CSharpCompilation.Create("Test", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var syntaxTree = CSharpSyntaxTree.ParseText("@class Test { public string Test { get; set; } }");
        compilation = compilation.AddSyntaxTrees(syntaxTree);
        var emitResult = compilation.Emit(new MemoryStream());

        // Act
        var exception = CompilationFailedExceptionFactory.Create(
            RazorCodeDocument.Create(RazorSourceDocument.Create("Test", "Index.cshtml"), Enumerable.Empty<RazorSourceDocument>()),
            syntaxTree.ToString(),
            "Test",
            emitResult.Diagnostics);

        // Assert
        Assert.Collection(
            exception.CompilationFailures,
            failure => Assert.Equal(expected, failure.FailureSummary));
    }

    [Fact]
    public void GetCompilationFailedResult_UsesPhysicalPath()
    {
        // Arrange
        var viewPath = "/Views/Home/Index.cshtml";
        var physicalPath = @"x:\myapp\views\home\index.cshtml";

        var projectItem = new TestRazorProjectItem(viewPath, "<span name=\"@(User.Id\">", physicalPath: physicalPath);

        var codeDocument = GetCodeDocument(projectItem);
        var csharpDocument = codeDocument.GetCSharpDocument();

        // Act
        var compilationResult = CompilationFailedExceptionFactory.Create(codeDocument, csharpDocument.Diagnostics);

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

        var projectItem = new TestRazorProjectItem(viewPath, fileContent);
        var codeDocument = GetCodeDocument(projectItem);
        var csharpDocument = codeDocument.GetCSharpDocument();

        // Act
        var compilationResult = CompilationFailedExceptionFactory.Create(codeDocument, csharpDocument.Diagnostics);

        // Assert
        var failure = Assert.Single(compilationResult.CompilationFailures);
        Assert.Equal(fileContent, failure.SourceFileContent);
    }

    [Fact]
    public void GetCompilationFailedResult_ReadsContentFromImports()
    {
        // Arrange
        var viewPath = "/Views/Home/Index.cshtml";
        var importsPath = "/Views/_MyImports.cshtml";
        var fileContent = "@ ";
        var importsContent = "@(abc";

        var projectItem = new TestRazorProjectItem(viewPath, fileContent);
        var importsItem = new TestRazorProjectItem(importsPath, importsContent);
        var codeDocument = GetCodeDocument(projectItem, importsItem);
        var csharpDocument = codeDocument.GetCSharpDocument();

        // Act
        var compilationResult = CompilationFailedExceptionFactory.Create(codeDocument, csharpDocument.Diagnostics);

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
                Assert.Equal(importsPath, failure.SourceFilePath);
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

        // Act
        var result = CompilationFailedExceptionFactory.Create(codeDocument, diagnostics);

        // Assert
        Assert.Collection(result.CompilationFailures,
        failure =>
        {
            Assert.Equal(viewPath, failure.SourceFilePath);
            Assert.Equal("View Content", failure.SourceFileContent);
            Assert.Collection(failure.Messages,
                message =>
                {
                    Assert.Equal(diagnostics[0].GetMessage(CultureInfo.CurrentCulture), message.Message);
                    Assert.Equal(viewPath, message.SourceFilePath);
                    Assert.Equal(3, message.StartLine);
                    Assert.Equal(17, message.StartColumn);
                    Assert.Equal(3, message.EndLine);
                    Assert.Equal(18, message.EndColumn);
                },
                message =>
                {
                    Assert.Equal(diagnostics[1].GetMessage(CultureInfo.CurrentCulture), message.Message);
                    Assert.Equal(viewPath, message.SourceFilePath);
                    Assert.Equal(5, message.StartLine);
                    Assert.Equal(6, message.StartColumn);
                    Assert.Equal(5, message.EndLine);
                    Assert.Equal(13, message.EndColumn);
                },
                message =>
                {
                    Assert.Equal(diagnostics[2].GetMessage(CultureInfo.CurrentCulture), message.Message);
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
                    Assert.Equal(diagnostics[3].GetMessage(CultureInfo.CurrentCulture), message.Message);
                    Assert.Equal(viewImportsPath, message.SourceFilePath);
                    Assert.Equal(4, message.StartLine);
                    Assert.Equal(8, message.StartColumn);
                    Assert.Equal(4, message.EndLine);
                    Assert.Equal(12, message.EndColumn);
                });
        });
    }

    [Fact]
    public void GetCompilationFailedResult_ReturnsCompilationResult_WithGroupedMessages()
    {
        // Arrange
        var viewPath = "Views/Home/Index";
        var generatedCodeFileName = "Generated Code";
        var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("view-content", viewPath));
        var assemblyName = "random-assembly-name";

        var diagnostics = new[]
        {
                Diagnostic.Create(
                    GetRoslynDiagnostic("message-1"),
                    Location.Create(
                        viewPath,
                        new TextSpan(10, 5),
                        new LinePositionSpan(new LinePosition(10, 1), new LinePosition(10, 2)))),
                Diagnostic.Create(
                    GetRoslynDiagnostic("message-2"),
                    Location.Create(
                        assemblyName,
                        new TextSpan(1, 6),
                        new LinePositionSpan(new LinePosition(1, 2), new LinePosition(3, 4)))),
                Diagnostic.Create(
                    GetRoslynDiagnostic("message-3"),
                    Location.Create(
                        viewPath,
                        new TextSpan(40, 50),
                        new LinePositionSpan(new LinePosition(30, 5), new LinePosition(40, 12)))),
            };

        // Act
        var compilationResult = CompilationFailedExceptionFactory.Create(
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

    private static DiagnosticDescriptor GetRoslynDiagnostic(string messageFormat)
    {
        return new DiagnosticDescriptor(
            id: "someid",
            title: "sometitle",
            messageFormat: messageFormat,
            category: "some-category",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }

    private static RazorCodeDocument GetCodeDocument(TestRazorProjectItem projectItem, TestRazorProjectItem imports = null)
    {
        var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);
        var fileSystem = new VirtualRazorProjectFileSystem();
        fileSystem.Add(projectItem);

        var codeDocument = RazorCodeDocument.Create(sourceDocument);

        if (imports != null)
        {
            fileSystem.Add(imports);
            codeDocument = RazorCodeDocument.Create(sourceDocument, new[] { RazorSourceDocument.ReadFrom(imports) });
        }

        var razorEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem).Engine;

        razorEngine.Process(codeDocument);
        return codeDocument;
    }

}
