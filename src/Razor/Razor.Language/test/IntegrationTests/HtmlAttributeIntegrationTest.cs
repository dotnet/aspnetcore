// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class HtmlAttributeIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void HtmlWithDataDashAttribute()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
        }

        [Fact]
        public void HtmlWithConditionalAttribute()
        {
            // Arrange
            var projectItem = CreateProjectItemFromFile();

            // Act
            var compiled = CompileToCSharp(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentIntermediateNode());
        }
    }
}
