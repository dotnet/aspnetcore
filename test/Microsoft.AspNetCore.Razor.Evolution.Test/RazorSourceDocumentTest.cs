// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorSourceDocumentTest
    {
        [Fact]
        public void Create()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent();

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml");

            // Assert
            Assert.IsType<DefaultRazorSourceDocument>(document);
            Assert.Equal("file.cshtml", document.Filename);
            Assert.Same(Encoding.UTF8, document.Encoding);
        }

        [Fact]
        public void Create_WithEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(encoding: Encoding.UTF32);

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml", Encoding.UTF32);

            // Assert
            Assert.Equal("file.cshtml", document.Filename);
            Assert.Same(Encoding.UTF32, Assert.IsType<DefaultRazorSourceDocument>(document).Encoding);
        }

        [Fact]
        public void ReadFrom_DetectsEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(encoding: Encoding.UTF32);

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml");

            // Assert
            Assert.IsType<DefaultRazorSourceDocument>(document);
            Assert.Equal("file.cshtml", document.Filename);
            Assert.Equal(Encoding.UTF32, document.Encoding);
        }

        [Fact]
        public void ReadFrom_FailsOnMismatchedEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(encoding: Encoding.UTF32);
            var expectedMessage = Resources.FormatMismatchedContentEncoding(Encoding.UTF8.EncodingName, Encoding.UTF32.EncodingName);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => RazorSourceDocument.ReadFrom(content, "file.cshtml", Encoding.UTF8));
            Assert.Equal(expectedMessage, exception.Message);
        }
    }
}
