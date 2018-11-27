// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
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
        public void RazorPageDocumentClassifierPass_LogsErrorIfDirectiveNotAtTopOfFile()
        {
            // Arrange
            var sourceSpan = new SourceSpan(
                "Test.cshtml",
                absoluteIndex: 14 + Environment.NewLine.Length * 2,
                lineIndex: 2,
                characterIndex: 0,
                length: 5 + Environment.NewLine.Length);

            var expectedDiagnostic = RazorExtensionsDiagnosticFactory.CreatePageDirective_MustExistAtTheTopOfFile(sourceSpan);
            var content = @"
@somethingelse
@page
";
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create(content, "Test.cshtml"));

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
            var pageDirectives = irDocument.FindDirectiveReferences(PageDirective.Directive);
            var directive = Assert.Single(pageDirectives);
            var diagnostic = Assert.Single(directive.Node.Diagnostics);
            Assert.Equal(expectedDiagnostic, diagnostic);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_DoesNotLogErrorIfCommentAndWhitespaceBeforeDirective()
        {
            // Arrange
            var content = @"
@* some comment *@
     
@page
";
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create(content, "Test.cshtml"));

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
            var pageDirectives = irDocument.FindDirectiveReferences(PageDirective.Directive);
            var directive = Assert.Single(pageDirectives);
            Assert.Empty(directive.Node.Diagnostics);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_SetsDocumentKind()
        {
            // Arrange
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", "Test.cshtml"));

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
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", "Test.cshtml"));

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
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page+1", "Test.cshtml"));

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
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", "Test.cshtml"));

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
            var properties = new RazorSourceDocumentProperties(filePath: "ignored", relativePath: "Test.cshtml");
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", properties));

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
            Assert.Equal("global::Microsoft.AspNetCore.Mvc.RazorPages.Page", visitor.Class.BaseType);
            Assert.Equal(new[] { "public" }, visitor.Class.Modifiers);
            Assert.Equal("Test", visitor.Class.ClassName);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_NullFilePath_SetsClass()
        {
            // Arrange
            var properties = new RazorSourceDocumentProperties(filePath: null, relativePath: null);
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", properties));

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
            Assert.Equal("global::Microsoft.AspNetCore.Mvc.RazorPages.Page", visitor.Class.BaseType);
            Assert.Equal(new[] { "public" }, visitor.Class.Modifiers);
            Assert.Equal("AspNetCore_74fbaab062bb228ed1ab09c5ff8d6ed2417320e2", visitor.Class.ClassName);
        }

        [Theory]
        [InlineData("/Views/Home/Index.cshtml", "_Views_Home_Index")]
        [InlineData("/Areas/MyArea/Views/Home/About.cshtml", "_Areas_MyArea_Views_Home_About")]
        public void RazorPageDocumentClassifierPass_UsesRelativePathToGenerateTypeName(string relativePath, string expected)
        {
            // Arrange
            var properties = new RazorSourceDocumentProperties(filePath: "ignored", relativePath: relativePath);
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", properties));

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
            var properties = new RazorSourceDocumentProperties(filePath: @"x::\application\Views\Home\Index.cshtml", relativePath: null);
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", properties));

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
            Assert.Equal("x___application_Views_Home_Index", visitor.Class.ClassName);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_SanitizesClassName()
        {
            // Arrange
            var properties = new RazorSourceDocumentProperties(filePath: @"x:\Test.cshtml", relativePath: "path.with+invalid-chars");
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", properties));

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
            Assert.Equal("path_with_invalid_chars", visitor.Class.ClassName);
        }

        [Fact]
        public void RazorPageDocumentClassifierPass_SetsUpExecuteAsyncMethod()
        {
            // Arrange
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page", "Test.cshtml"));

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

        [Fact]
        public void RazorPageDocumentClassifierPass_AddsRouteTemplateMetadata()
        {
            // Arrange
            var properties = new RazorSourceDocumentProperties(filePath: "ignored", relativePath: "Test.cshtml");
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("@page \"some-route\"", properties));

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
            var attributeNode = Assert.IsType<RazorCompiledItemMetadataAttributeIntermediateNode>(visitor.ExtensionNode);
            Assert.Equal("RouteTemplate", attributeNode.Key);
            Assert.Equal("some-route", attributeNode.Value);
        }

        private static RazorEngine CreateEngine()
        {
            return RazorProjectEngine.Create(b =>
            {
                PageDirective.Register(b);
            }).Engine;
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

            public ExtensionIntermediateNode ExtensionNode { get; private set; }

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

            public override void VisitExtension(ExtensionIntermediateNode node)
            {
                ExtensionNode = node;
            }
        }
    }
}
