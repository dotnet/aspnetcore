// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var content = TestRazorSourceDocument.CreateContent();

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml");

            // Assert
            Assert.Equal("file.cshtml", document.Filename);
            Assert.Null(Assert.IsType<DefaultRazorSourceDocument>(document).Encoding);
        }

        [Fact]
        public void Create_WithEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateContent(encoding: Encoding.UTF32);

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml", Encoding.UTF32);

            // Assert
            Assert.Equal("file.cshtml", document.Filename);
            Assert.Same(Encoding.UTF32, Assert.IsType<DefaultRazorSourceDocument>(document).Encoding);
        }
    }
}
