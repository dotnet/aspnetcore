// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class DefaultMetadataIdentifierFeatureTest : RazorProjectEngineTestBase
    {
        protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

        [Fact]
        public void GetIdentifier_ReturnsNull_ForNullRelativePath()
        {
            // Arrange
            var sourceDocument = RazorSourceDocument.Create("content", new RazorSourceDocumentProperties("Test.cshtml", null));
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var feature = new DefaultMetadataIdentifierFeature()
            {
                Engine = CreateProjectEngine().Engine,
            };

            // Act
            var result = feature.GetIdentifier(codeDocument, sourceDocument);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetIdentifier_ReturnsNull_ForEmptyRelativePath()
        {
            // Arrange
            var sourceDocument = RazorSourceDocument.Create("content", new RazorSourceDocumentProperties("Test.cshtml", string.Empty));
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var feature = new DefaultMetadataIdentifierFeature()
            {
                Engine = CreateProjectEngine().Engine,
            };

            // Act
            var result = feature.GetIdentifier(codeDocument, sourceDocument);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Test.cshtml", "/Test.cshtml")]
        [InlineData("/Test.cshtml", "/Test.cshtml")]
        [InlineData("\\Test.cshtml", "/Test.cshtml")]
        [InlineData("\\About\\Test.cshtml", "/About/Test.cshtml")]
        [InlineData("\\About\\Test\\cshtml", "/About/Test/cshtml")]
        public void GetIdentifier_SanitizesRelativePath(string relativePath, string expected)
        {
            // Arrange
            var sourceDocument = RazorSourceDocument.Create("content", new RazorSourceDocumentProperties("Test.cshtml", relativePath));
            var codeDocument = RazorCodeDocument.Create(sourceDocument);

            var feature = new DefaultMetadataIdentifierFeature()
            {
                Engine = CreateProjectEngine().Engine,
            };

            // Act
            var result = feature.GetIdentifier(codeDocument, sourceDocument);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
