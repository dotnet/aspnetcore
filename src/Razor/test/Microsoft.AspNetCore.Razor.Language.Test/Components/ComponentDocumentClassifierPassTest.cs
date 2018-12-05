// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    public class ComponentDocumentClassifierPassTest
    {
        [Fact]
        public void Execute_SetsDocumentKind()
        {
            // Arrange
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", "Test.razor"));
            codeDocument.SetInputDocumentKind(InputDocumentKind.Component);

            var projectEngine = CreateProjectEngine();
            var irDocument = CreateIRDocument(projectEngine, codeDocument);
            var pass = new ComponentDocumentClassifierPass
            {
                Engine = projectEngine.Engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Assert.Equal(ComponentDocumentClassifierPass.ComponentDocumentKind, irDocument.DocumentKind);
        }

        [Fact]
        public void ComponentDocumentClassifierPass_SetsNamespace()
        {
            // Arrange
            var properties = new RazorSourceDocumentProperties(filePath: "/MyApp/Test.razor", relativePath: "Test.razor");
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", properties));
            codeDocument.SetInputDocumentKind(InputDocumentKind.Component);

            var projectEngine = CreateProjectEngine();
            var irDocument = CreateIRDocument(projectEngine, codeDocument);
            var pass = new ComponentDocumentClassifierPass
            {
                Engine = projectEngine.Engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal("MyApp", visitor.Namespace.Content);
        }

        [Fact]
        public void ComponentDocumentClassifierPass_SetsClass()
        {
            // Arrange
            var properties = new RazorSourceDocumentProperties(filePath: "/MyApp/Test.razor", relativePath: "Test.razor");
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", properties));
            codeDocument.SetInputDocumentKind(InputDocumentKind.Component);

            var projectEngine = CreateProjectEngine();
            var irDocument = CreateIRDocument(projectEngine, codeDocument);
            var pass = new ComponentDocumentClassifierPass
            {
                Engine = projectEngine.Engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal($"global::{CodeGenerationConstants.RazorComponent.FullTypeName}", visitor.Class.BaseType);
            Assert.Equal(new[] { "public", "sealed" }, visitor.Class.Modifiers);
            Assert.Equal("Test", visitor.Class.ClassName);
        }

        [Fact]
        public void ComponentDocumentClassifierPass_UsesRelativePathToGenerateTypeNameAndNamespace()
        {
            // Arrange
            var relativePath = "/Pages/Announcements/Banner.razor";
            var properties = new RazorSourceDocumentProperties(filePath: $"/MyApp{relativePath}", relativePath: relativePath);
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", properties));
            codeDocument.SetInputDocumentKind(InputDocumentKind.Component);

            var projectEngine = CreateProjectEngine();
            var irDocument = CreateIRDocument(projectEngine, codeDocument);
            var pass = new ComponentDocumentClassifierPass
            {
                Engine = projectEngine.Engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal("Banner", visitor.Class.ClassName);
            Assert.Equal("MyApp.Pages.Announcements", visitor.Namespace.Content);
        }

        [Fact]
        public void ComponentDocumentClassifierPass_SanitizesClassName()
        {
            // Arrange
            var properties = new RazorSourceDocumentProperties(filePath: @"x:\path.with+invalid-chars.razor", relativePath: "path.with+invalid-chars.razor");
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", properties));
            codeDocument.SetInputDocumentKind(InputDocumentKind.Component);

            var projectEngine = CreateProjectEngine();
            var irDocument = CreateIRDocument(projectEngine, codeDocument);
            var pass = new ComponentDocumentClassifierPass
            {
                Engine = projectEngine.Engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal("path_with_invalid_chars", visitor.Class.ClassName);
        }

        [Fact]
        public void ComponentDocumentClassifierPass_SetsUpMainMethod()
        {
            // Arrange
            var codeDocument = RazorCodeDocument.Create(RazorSourceDocument.Create("some-content", "Test.razor"));
            codeDocument.SetInputDocumentKind(InputDocumentKind.Component);

            var projectEngine = CreateProjectEngine();
            var irDocument = CreateIRDocument(projectEngine, codeDocument);
            var pass = new ComponentDocumentClassifierPass
            {
                Engine = projectEngine.Engine
            };

            // Act
            pass.Execute(codeDocument, irDocument);
            var visitor = new Visitor();
            visitor.Visit(irDocument);

            // Assert
            Assert.Equal(CodeGenerationConstants.RazorComponent.BuildRenderTree, visitor.Method.MethodName);
            Assert.Equal("void", visitor.Method.ReturnType);
            Assert.Equal(new[] { "public", "override" }, visitor.Method.Modifiers);
        }

        private static RazorProjectEngine CreateProjectEngine() => RazorProjectEngine.Create();

        private static DocumentIntermediateNode CreateIRDocument(RazorProjectEngine projectEngine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < projectEngine.Phases.Count; i++)
            {
                var phase = projectEngine.Phases[i];
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

        private class TestComponentDocumentClassifierPass : ComponentDocumentClassifierPass
        {
            public new bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
                => base.IsMatch(codeDocument, documentNode);
        }
    }
}
