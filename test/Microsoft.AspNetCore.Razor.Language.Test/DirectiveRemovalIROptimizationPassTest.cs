// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DirectiveRemovalIROptimizationPassTest
    {
        [Fact]
        public void Execute_Custom_RemovesDirectiveIRNodeFromIRDocument()
        {
            // Arrange
            var content = "@custom \"Hello\"";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var defaultEngine = RazorEngine.Create(b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, d => d.AddStringToken()));
            });
            var irDocument = Lower(codeDocument, defaultEngine);
            var pass = new DirectiveRemovalIROptimizationPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Children(irDocument,
                node => Assert.IsType<NamespaceDeclarationIRNode>(node));
            var @namespace = irDocument.Children[0];
            Children(@namespace,
                node => Assert.IsType<ClassDeclarationIRNode>(node));
            var @class = @namespace.Children[0];
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            Assert.Empty(method.Children);
        }

        [Fact]
        public void Execute_MultipleCustomDirectives_RemovesDirectiveIRNodesFromIRDocument()
        {
            // Arrange
            var content = "@custom \"Hello\"" + Environment.NewLine + "@custom \"World\"";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var defaultEngine = RazorEngine.Create(b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, d => d.AddStringToken()));
            });
            var irDocument = Lower(codeDocument, defaultEngine);
            var pass = new DirectiveRemovalIROptimizationPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Children(irDocument,
                node => Assert.IsType<NamespaceDeclarationIRNode>(node));
            var @namespace = irDocument.Children[0];
            Children(@namespace,
                node => Assert.IsType<ClassDeclarationIRNode>(node));
            var @class = @namespace.Children[0];
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            Assert.Empty(method.Children);
        }

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
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

            var irDocument = codeDocument.GetIRDocument();
            Assert.NotNull(irDocument);

            return irDocument;
        }
    }
}
