// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class FunctionsDirectivePassTest
    {
        [Fact]
        public void Execute_SkipsDocumentWithNoClassNode()
        {
            // Arrange
            var projectEngine = CreateProjectEngine();
            var pass = new FunctionsDirectivePass()
            {
                Engine = projectEngine.Engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create("@functions { var value = true; }");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIntermediateNode();
            irDocument.Children.Add(new DirectiveIntermediateNode() { Directive = FunctionsDirective.Directive, });

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Children(
                irDocument,
                node => Assert.IsType<DirectiveIntermediateNode>(node));
        }

        [Fact]
        public void Execute_AddsStatementsToClassLevel()
        {
            // Arrange
            var projectEngine = CreateProjectEngine();
            var pass = new FunctionsDirectivePass()
            {
                Engine = projectEngine.Engine,
            };
            
            var sourceDocument = TestRazorSourceDocument.Create("@functions { var value = true; }");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = Lower(codeDocument, projectEngine);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Children(
                irDocument,
                node => Assert.IsType<NamespaceDeclarationIntermediateNode>(node));

            var @namespace = irDocument.Children[0];
            Children(
                @namespace,
                node => Assert.IsType<ClassDeclarationIntermediateNode>(node));

            var @class = @namespace.Children[0];
            Children(
                @class,
                node => Assert.IsType<MethodDeclarationIntermediateNode>(node),
                node => CSharpCode(" var value = true; ", node));

            var method = @class.Children[0];
            Children(
                method,
                node => Assert.IsType<DirectiveIntermediateNode>(node));
        }

        private static RazorProjectEngine CreateProjectEngine()
        {
            return RazorProjectEngine.Create();
        }

        private static DocumentIntermediateNode Lower(RazorCodeDocument codeDocument, RazorProjectEngine projectEngine)
        {
            for (var i = 0; i < projectEngine.Phases.Count; i++)
            {
                var phase = projectEngine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorDocumentClassifierPhase)
                {
                    break;
                }
            }

            var irDocument = codeDocument.GetDocumentIntermediateNode();
            Assert.NotNull(irDocument);

            return irDocument;
        }
    }
}
