// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
