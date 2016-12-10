// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Evolution.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultDirectiveIRPassTest
    {
        [Fact]
        public void Execute_MutatesIRDocument()
        {
            // Arrange
            var content =
@"@inherits Hello<World[]>
@functions {
    var value = true;
}";
            var originalIRDocument = Lower(content);
            var pass = new DefaultDirectiveIRPass();

            // Act
            var irDocument = pass.Execute(codeDocument: null, irDocument: originalIRDocument);

            // Assert
            Assert.Same(originalIRDocument, irDocument);
        }

        [Fact]
        public void Execute_Inherits_SetsClassDeclarationBaseType()
        {
            // Arrange
            var content = "@inherits Hello<World[]>";
            var originalIRDocument = Lower(content);
            var pass = new DefaultDirectiveIRPass();

            // Act
            var irDocument = pass.Execute(codeDocument: null, irDocument: originalIRDocument);

            // Assert
            Children(irDocument,
                node => Assert.IsType<ChecksumIRNode>(node),
                node => Assert.IsType<NamespaceDeclarationIRNode>(node));
            var @namespace = irDocument.Children[1];
            Children(@namespace,
                node => Assert.IsType<UsingStatementIRNode>(node),
                node => Assert.IsType<UsingStatementIRNode>(node),
                node => Assert.IsType<ClassDeclarationIRNode>(node));
            var @class = (ClassDeclarationIRNode)@namespace.Children[2];
            Assert.Equal(@class.BaseType, "Hello<World[]>");
        }

        [Fact]
        public void Execute_Functions_ExistsAtClassDeclarationAndMethodLevel()
        {
            // Arrange
            var content = "@functions { var value = true; }";
            var originalIRDocument = Lower(content);
            var pass = new DefaultDirectiveIRPass();

            // Act
            var irDocument = pass.Execute(codeDocument: null, irDocument: originalIRDocument);

            // Assert
            Children(irDocument,
                node => Assert.IsType<ChecksumIRNode>(node),
                node => Assert.IsType<NamespaceDeclarationIRNode>(node));
            var @namespace = irDocument.Children[1];
            Children(@namespace,
                node => Assert.IsType<UsingStatementIRNode>(node),
                node => Assert.IsType<UsingStatementIRNode>(node),
                node => Assert.IsType<ClassDeclarationIRNode>(node));
            var @class = @namespace.Children[2];
            Children(@class,
                node => Assert.IsType<RazorMethodDeclarationIRNode>(node),
                node => CSharpStatement(" var value = true; ", node));
            var method = (RazorMethodDeclarationIRNode)@class.Children[0];
            Children(method,
                node => Html(string.Empty, node),
                node => Directive("functions", node,
                    directiveChild => CSharpStatement(" var value = true; ", directiveChild)),
                node => Html(string.Empty, node));
        }

        private static DocumentIRNode Lower(string content)
        {
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var engine = RazorEngine.Create();

            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIRLoweringPhase)
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
