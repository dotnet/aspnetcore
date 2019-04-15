// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DirectiveRemovalOptimizationPassTest
    {
        [Fact]
        public void Execute_Custom_RemovesDirectiveNodeFromDocument()
        {
            // Arrange
            var content = "@custom \"Hello\"";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var defaultEngine = RazorProjectEngine.Create(b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, d => d.AddStringToken()));
            }).Engine;
            var documentNode = Lower(codeDocument, defaultEngine);
            var pass = new DirectiveRemovalOptimizationPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, documentNode);

            // Assert
            Children(documentNode,
                node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));
            var @namespace = documentNode.Children[0];
            Children(@namespace,
                node => Assert.IsType<ClassDeclarationIntermediateNode>(node));
            var @class = @namespace.Children[0];
            var method = SingleChild<MethodDeclarationIntermediateNode>(@class);
            Assert.Empty(method.Children);
        }

        [Fact]
        public void Execute_MultipleCustomDirectives_RemovesDirectiveNodesFromDocument()
        {
            // Arrange
            var content = "@custom \"Hello\"" + Environment.NewLine + "@custom \"World\"";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var defaultEngine = RazorProjectEngine.Create(b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, d => d.AddStringToken()));
            }).Engine;
            var documentNode = Lower(codeDocument, defaultEngine);
            var pass = new DirectiveRemovalOptimizationPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, documentNode);

            // Assert
            Children(documentNode,
                node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));
            var @namespace = documentNode.Children[0];
            Children(@namespace,
                node => Assert.IsType<ClassDeclarationIntermediateNode>(node));
            var @class = @namespace.Children[0];
            var method = SingleChild<MethodDeclarationIntermediateNode>(@class);
            Assert.Empty(method.Children);
        }

        [Fact]
        public void Execute_DirectiveWithError_PreservesDiagnosticsAndRemovesDirectiveNodeFromDocument()
        {
            // Arrange
            var content = "@custom \"Hello\"";
            var expectedDiagnostic = RazorDiagnostic.Create(new RazorDiagnosticDescriptor("RZ9999", () => "Some diagnostic message.", RazorDiagnosticSeverity.Error), SourceSpan.Undefined);
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var defaultEngine = RazorProjectEngine.Create(b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, d => d.AddStringToken()));
            }).Engine;
            var documentNode = Lower(codeDocument, defaultEngine);

            // Add the diagnostic to the directive node.
            var directiveNode = documentNode.FindDescendantNodes<DirectiveIntermediateNode>().Single();
            directiveNode.Diagnostics.Add(expectedDiagnostic);

            var pass = new DirectiveRemovalOptimizationPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, documentNode);

            // Assert
            var diagnostic = Assert.Single(documentNode.Diagnostics);
            Assert.Equal(expectedDiagnostic, diagnostic);

            Children(documentNode,
                node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));
            var @namespace = documentNode.Children[0];
            Children(@namespace,
                node => Assert.IsType<ClassDeclarationIntermediateNode>(node));
            var @class = @namespace.Children[0];
            var method = SingleChild<MethodDeclarationIntermediateNode>(@class);
            Assert.Empty(method.Children);
        }

        private static DocumentIntermediateNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorDirectiveClassifierPhase)
                {
                    break;
                }
            }

            var documentNode = codeDocument.GetDocumentIntermediateNode();
            Assert.NotNull(documentNode);

            return documentNode;
        }
    }
}
