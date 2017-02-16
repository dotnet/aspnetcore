// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public void CSharpDocument_Runtime_PreservesParserErrors()
        {
            // Arrange
            var engine = RazorEngine.Create();

            var document = RazorCodeDocument.Create(TestRazorSourceDocument.Create("@!!!"));

            var expected = RazorDiagnostic.Create(new RazorError(
                LegacyResources.FormatParseError_Unexpected_Character_At_Start_Of_CodeBlock_CS("!"),
                new SourceLocation(1, 0, 1),
                length: 1));

            // Act
            engine.Process(document);

            // Assert
            var csharpDocument = document.GetCSharpDocument();
            var error = Assert.Single(csharpDocument.Diagnostics);
            Assert.Equal(expected, error);
        }

        [Fact]
        public void CSharpDocument_DesignTime_PreservesParserErrors()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime();

            var document = RazorCodeDocument.Create(TestRazorSourceDocument.Create("@{"));

            var expected = RazorDiagnostic.Create(new RazorError(
                LegacyResources.FormatParseError_Expected_EndOfBlock_Before_EOF(LegacyResources.BlockName_Code, "}", "{"),
                new SourceLocation(1, 0, 1),
                length: 1));

            // Act
            engine.Process(document);

            // Assert
            var csharpDocument = document.GetCSharpDocument();
            var error = Assert.Single(csharpDocument.Diagnostics);
            Assert.Equal(expected, error);
        }
    }
}
