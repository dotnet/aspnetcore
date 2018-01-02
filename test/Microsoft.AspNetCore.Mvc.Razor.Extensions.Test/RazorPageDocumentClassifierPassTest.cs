// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class RazorPageDocumentClassifierPassTest
    {
        [Fact]
        public void RazorPageDocumentClassifierPass_LogsErrorForImportedPageDirectives()
        {
            // Arrange
            var sourceSpan = new SourceSpan("import.cshtml", 0, 0, 0, 5);
            var expectedDiagnostic = RazorExtensionsDiagnosticFactory.CreatePageDirective_CannotBeImported(sourceSpan);
            var importDocument = RazorSourceDocument.Create("@page", "import.cshtml");
            var sourceDocument = RazorSourceDocument.Create("<p>Hello World</p>", "main.cshtml");
            var codeDocument = RazorCodeDocument.Create(sourceDocument, new[] { importDocument });
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var pageDirectives = irDocument.FindDirectiveReferences(PageDirective.Directive);
            var directive = Assert.Single(pageDirectives);
            var diagnostic = Assert.Single(directive.Node.Diagnostics);
            Assert.Equal(expectedDiagnostic, diagnostic);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_SetsDocumentKind()
        {
            // Arrange
            var codeDocument = CreateDocument("@page");
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Assert.Equal("mvc.1.0.razor-page", irDocument.DocumentKind);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_NoOpsIfDocumentKindIsAlreadySet()
        {
            // Arrange
            var codeDocument = CreateDocument("@page");
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            irDocument.DocumentKind = "some-value";
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Assert.Equal("some-value", irDocument.DocumentKind);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_NoOpsIfPageDirectiveIsMalformed()
        {
            // Arrange
            var codeDocument = CreateDocument("@page+1");
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            irDocument.DocumentKind = "some-value";
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Assert.Equal("some-value", irDocument.DocumentKind);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_SetsNamespace()
        {
            // Arrange
            var codeDocument = CreateDocument("@page");
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal("AspNetCore", visitor.Namespace.Content);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_SetsClass()
        {
            // Arrange
            var codeDocument = CreateDocument("@page");
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };
            codeDocument.SetRelativePath("Test.cshtml");

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal("global::Microsoft.AspNetCore.Mvc.RazorPages.Page", visitor.Class.BaseType);
            Assert.Equal(new[] { "public" }, visitor.Class.Modifiers);
            Assert.Equal("Test", visitor.Class.ClassName);
        }

        [Theory]
        [InlineData("/Views/Home/Index.cshtml", "_Views_Home_Index")]
        [InlineData("/Areas/MyArea/Views/Home/About.cshtml", "_Areas_MyArea_Views_Home_About")]
        public void RazorPageDocumentClassifierPass_UsesRelativePathToGenerateTypeName(string relativePath, string expected)
        {
            // Arrange
            var codeDocument = CreateDocument("@page");
            codeDocument.SetRelativePath(relativePath);
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal(expected, visitor.Class.ClassName);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_UsesAbsolutePath_IfRelativePathIsNotSet()
        {
            // Arrange
            var expected = "x___application_Views_Home_Index";
            var path = @"x::\application\Views\Home\Index.cshtml";
            var codeDocument = CreateDocument("@page", path);
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal(expected, visitor.Class.ClassName);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_SanitizesClassName()
        {
            // Arrange
            var expected = "path_with_invalid_chars";
            var codeDocument = CreateDocument("@page");
            codeDocument.SetRelativePath("path.with+invalid-chars");
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal(expected, visitor.Class.ClassName);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_SetsUpExecuteAsyncMethod()
        {
            // Arrange
            var codeDocument = CreateDocument("@page");
            var engine = CreateEngine();
            var irDocument = CreateIRDocument(engine, codeDocument);
            var pass = new RazorPageDocumentClassifierPass
            {
                Engine = engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal("ExecuteAsync", visitor.Method.MethodName);
            Assert.Equal("global::System.Threading.Tasks.Task", visitor.Method.ReturnType);
            Assert.Equal(new[] { "public", "async", "override" }, visitor.Method.Modifiers);
        }

        private static RazorCodeDocument CreateDocument(string content, string filePath = null)
        {
            filePath = filePath ?? Path.Combine(Directory.GetCurrentDirectory(), "Test.cshtml");

            var source = RazorSourceDocument.Create(content, filePath);
            return RazorCodeDocument.Create(source);
        }

        private static RazorEngine CreateEngine()
        {
            return RazorEngine.Create(b =>
            {
                PageDirective.Register(b);
            });
        }

        private static DocumentIntermediateNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIntermediateNodeLoweringPhase)
                {
                    break;
                }
            }

            return codeDocument.GetDocumentIntermediateNode();
        }

        private class Visitor : IntermediateNodeWalker
        {
            public NamespaceDeclarationIntermediateNode Namespace { get; private set; }

            public ClassDeclarationIntermediateNode Class { get; private set; }

            public MethodDeclarationIntermediateNode Method { get; private set; }

            public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
            {
                Method = node;
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                Namespace = node;
                base.VisitNamespaceDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                Class = node;
                base.VisitClassDeclaration(node);
            }
        }
    }
}
