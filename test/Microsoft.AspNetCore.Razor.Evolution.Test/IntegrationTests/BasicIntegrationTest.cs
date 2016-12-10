// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    public class BasicIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void Empty()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        [Fact]
        public void HelloWorld()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        [Fact]
        public void CustomDirective()
        {
            // Arrange
            var engine = RazorEngine.Create(b =>
            {
                b.AddDirective(DirectiveDescriptorBuilder.Create("test_directive").Build());
            });

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        [Fact]
        public void BuildEngine_CallProcess()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = RazorCodeDocument.Create(TestRazorSourceDocument.Create());

            // Act
            engine.Process(document);

            // Assert
            Assert.NotNull(document.GetSyntaxTree());
            Assert.NotNull(document.GetIRDocument());
        }

        [Fact]
        public void Process_CustomDirective()
        {
            // Arrange
            var engine = RazorEngine.Create(b =>
            {
                b.AddDirective(DirectiveDescriptorBuilder.Create("test_directive").Build());
            });

            var document = RazorCodeDocument.Create(TestRazorSourceDocument.Create("@test_directive"));

            // Act
            engine.Process(document);

            // Assert
            var syntaxTree = document.GetSyntaxTree();

            // This is fragile for now, but we don't want to invest in the legacy API until we're ready
            // to replace it properly.
            var directiveBlock = (Block)syntaxTree.Root.Children[1];
            var directiveSpan = (Span)directiveBlock.Children[1];
            Assert.Equal("test_directive", directiveSpan.Content);

            var irDocument = document.GetIRDocument();
            var irNamespace = irDocument.Children[1];
            var irClass = irNamespace.Children[2];
            var irMethod = irClass.Children[0];
            var irDirective = (DirectiveIRNode)irMethod.Children[1];
            Assert.Equal("test_directive", irDirective.Name);
        }
    }
}
