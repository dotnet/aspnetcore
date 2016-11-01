// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorCodeDocumentExtensionsTest
    {
        [Fact]
        public void GetRazorSyntaxTree_ReturnsSyntaxTree()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorSyntaxTree.Parse(codeDocument.Source);
            codeDocument.Items[typeof(RazorSyntaxTree)] = expected;

            // Act
            var actual = codeDocument.GetSyntaxTree();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetRazorSyntaxTree_SetsSyntaxTree()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorSyntaxTree.Parse(codeDocument.Source);

            // Act
            codeDocument.SetSyntaxTree(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(RazorSyntaxTree)]);
        }

        [Fact]
        public void GetIRDocument_ReturnsIRDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorIRBuilder.Document().Build();
            codeDocument.Items[typeof(DocumentIRNode)] = expected;

            // Act
            var actual = codeDocument.GetIRDocument();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetIRDocument_SetsIRDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorIRBuilder.Document().Build();

            // Act
            codeDocument.SetIRDocument((DocumentIRNode)expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(DocumentIRNode)]);
        }
    }
}
