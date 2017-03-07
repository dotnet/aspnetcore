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
        public void Execute_Inherits_SetsClassDeclarationBaseType()
        {
            // Arrange
            var content = "@inherits Hello<World[]>";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var defaultEngine = RazorEngine.Create();
            var pass = new DefaultDirectiveIRPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, irDocument);

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
        public void Execute_Functions_MovesStatementToClassLevel()
        {
            // Arrange
            var content = "@functions { var value = true; }";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var defaultEngine = RazorEngine.Create();
            var pass = new DefaultDirectiveIRPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, irDocument);

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
            Assert.Empty(method.Children);
        }

        [Fact]
        public void Execute_Section_WrapsStatementInDefineSection()
        {
            // Arrange
            var content = "@section Header { <p>Hello World</p> }";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var defaultEngine = RazorEngine.Create();
            var pass = new DefaultDirectiveIRPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, irDocument);

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
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            Children(method,
                node => CSharpStatement("DefineSection(\"Header\", async () => {", node),
                node => Html(" <p>Hello World</p> ", node),
                node => CSharpStatement("});", node));
        }

        [Fact]
        public void Execute_Section_DesignTime_WrapsStatementInBackwardsCompatibleDefineSection()
        {
            // Arrange
            var content = "@section Header { <p>Hello World</p> }";
            var designTimeEngine = RazorEngine.CreateDesignTime();
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, designTimeEngine);
            var defaultEngine = RazorEngine.Create();
            var pass = new DefaultDirectiveIRPass()
            {
                Engine = defaultEngine,
            };

            // Act
            pass.Execute(codeDocument, irDocument);

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
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            Children(method,
                node => CSharpStatement("DefineSection(\"Header\", async (__razor_section_writer) => {", node),
                node => Html(" <p>Hello World</p> ", node),
                node => CSharpStatement("});", node));
        }

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();

            return Lower(codeDocument, engine);
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