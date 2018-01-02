// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorCodeDocumentExtensionsTest
    {
        [Fact]
        public void GetIdentifier_ReturnsIdentifier()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = "test";
            codeDocument.Items[RazorCodeDocumentExtensions.IdentifierKey] = expected;

            // Act
            var actual = codeDocument.GetIdentifier();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetIdentifier_SetsIdentifier()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = "test";

            // Act
            codeDocument.SetIdentifier(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[RazorCodeDocumentExtensions.IdentifierKey]);
        }

        [Fact]
        public void GetImportIdentifiers_ReturnsIdentifiers()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new string[] { "test1", "test2" };
            codeDocument.Items[RazorCodeDocumentExtensions.ImportIdentifiersKey] = expected;

            // Act
            var actual = codeDocument.GetImportIdentifiers();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetImportIdentifiers_SetsIdentifiers()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new string[] { "test1", "test2" };

            // Act
            codeDocument.SetImportIdentifiers(expected);

            // Assert
            Assert.Equal(expected, codeDocument.Items[RazorCodeDocumentExtensions.ImportIdentifiersKey]);

            // Not the same, but equal, it makes a defensive copy.
            Assert.NotSame(expected, codeDocument.Items[RazorCodeDocumentExtensions.ImportIdentifiersKey]);
        }

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
        public void GetAndSetImportSyntaxTrees_ReturnsSyntaxTrees()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new[] { RazorSyntaxTree.Parse(codeDocument.Source), };
            codeDocument.SetImportSyntaxTrees(expected);

            // Act
            var actual = codeDocument.GetImportSyntaxTrees();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void GetIRDocument_ReturnsIRDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new DocumentIntermediateNode();
            codeDocument.Items[typeof(DocumentIntermediateNode)] = expected;

            // Act
            var actual = codeDocument.GetDocumentIntermediateNode();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetIRDocument_SetsIRDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = new DocumentIntermediateNode();

            // Act
            codeDocument.SetDocumentIntermediateNode((DocumentIntermediateNode)expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(DocumentIntermediateNode)]);
        }

        [Fact]
        public void GetCSharpDocument_ReturnsCSharpDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorCSharpDocument.Create("", RazorCodeGenerationOptions.CreateDefault(), Array.Empty<RazorDiagnostic>());
            codeDocument.Items[typeof(RazorCSharpDocument)] = expected;

            // Act
            var actual = codeDocument.GetCSharpDocument();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetCSharpDocument_SetsCSharpDocument()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = RazorCSharpDocument.Create("", RazorCodeGenerationOptions.CreateDefault(), Array.Empty<RazorDiagnostic>());

            // Act
            codeDocument.SetCSharpDocument(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(RazorCSharpDocument)]);
        }

        [Fact]
        public void GetTagHelperContext_ReturnsTagHelperContext()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = TagHelperDocumentContext.Create(null, new TagHelperDescriptor[0]);
            codeDocument.Items[typeof(TagHelperDocumentContext)] = expected;

            // Act
            var actual = codeDocument.GetTagHelperContext();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetTagHelperContext_SetsTagHelperContext()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            var expected = TagHelperDocumentContext.Create(null, new TagHelperDescriptor[0]);

            // Act
            codeDocument.SetTagHelperContext(expected);

            // Assert
            Assert.Same(expected, codeDocument.Items[typeof(TagHelperDocumentContext)]);
        }
    }
}
