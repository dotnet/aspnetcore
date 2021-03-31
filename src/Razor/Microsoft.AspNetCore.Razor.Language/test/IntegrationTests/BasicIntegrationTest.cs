// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class BasicIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void Empty()
        {
            // Arrange
            var projectEngine = CreateProjectEngine();
            var projectItem = CreateProjectItemFromFile();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
        }

        [Fact]
        public void HelloWorld()
        {
            // Arrange
            var projectEngine = CreateProjectEngine();
            var projectItem = CreateProjectItemFromFile();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
        }

        [Fact]
        public void CustomDirective()
        {
            // Arrange
            var projectEngine = CreateProjectEngine(b =>
            {
                b.AddDirective(DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine));
            });

            var projectItem = CreateProjectItemFromFile();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
        }
    }
}
