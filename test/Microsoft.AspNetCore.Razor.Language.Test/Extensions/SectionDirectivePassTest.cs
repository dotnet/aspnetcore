// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class SectionDirectivePassTest
    {
        [Fact]
        public void Execute_SkipsDocumentWithNoClassNode()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new SectionDirectivePass()
            {
                Engine = engine,
            };

            var sourceDocument = TestRazorSourceDocument.Create("@section Header { <p>Hello World</p> }");
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = new DocumentIRNode();
            irDocument.Children.Add(new DirectiveIRNode() { Descriptor = SectionDirective.Directive, });

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Children(
                irDocument,
                node => Assert.IsType<DirectiveIRNode>(node));
        }

        [Fact]
        public void Execute_WrapsStatementInDefineSection()
        {
            // Arrange
            var engine = CreateEngine();
            var pass = new SectionDirectivePass()
            {
                Engine = engine,
            };

            var content = "@section Header { <p>Hello World</p> }";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = Lower(codeDocument, engine);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Children(
                irDocument,
                node => Assert.IsType<NamespaceDeclarationIRNode>(node));

            var @namespace = irDocument.Children[0];
            Children(
                @namespace,
                node => Assert.IsType<ClassDeclarationIRNode>(node));

            var @class = @namespace.Children[0];
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            Children(
                method,
                node => CSharpCode("DefineSection(\"Header\", async () => {", node),
                node => Html(" <p>Hello World</p> ", node),
                node => CSharpCode("});", node));
        }

        [Fact]
        public void Execute_DesignTime_WrapsStatementInBackwardsCompatibleDefineSection()
        {
            // Arrange
            var engine = CreateDesignTimeEngine();
            var pass = new SectionDirectivePass()
            {
                Engine = engine,
            };

            var content = "@section Header { <p>Hello World</p> }";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var irDocument = Lower(codeDocument, engine);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            Children(
                irDocument,
                node => Assert.IsType<NamespaceDeclarationIRNode>(node));

            var @namespace = irDocument.Children[0];
            Children(
                @namespace,
                node => Assert.IsType<ClassDeclarationIRNode>(node));

            var @class = @namespace.Children[0];
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            Children(
                method,
                node => CSharpCode("DefineSection(\"Header\", async (__razor_section_writer) => {", node),
                node => Html(" <p>Hello World</p> ", node),
                node => CSharpCode("});", node));
        }

        private static RazorEngine CreateEngine()
        {
            return RazorEngine.Create(b =>
            {
                SectionDirective.Register(b);
            });
        }

        private static RazorEngine CreateDesignTimeEngine()
        {
            return RazorEngine.CreateDesignTime(b =>
            {
                SectionDirective.Register(b);
            });
        }

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorDocumentClassifierPhase)
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
