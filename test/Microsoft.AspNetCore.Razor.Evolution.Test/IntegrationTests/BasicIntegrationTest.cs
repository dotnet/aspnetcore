// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    }
}
