// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorSourceDocumentTest
    {
        [Fact]
        public void ReadFrom()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent();

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml");

            // Assert
            Assert.IsType<StreamSourceDocument>(document);
            Assert.Equal("file.cshtml", document.FilePath);
            Assert.Same(Encoding.UTF8, document.Encoding);
        }

        [Fact]
        public void ReadFrom_WithEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(encoding: Encoding.UTF32);

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml", Encoding.UTF32);

            // Assert
            Assert.Equal("file.cshtml", document.FilePath);
            Assert.Same(Encoding.UTF32, Assert.IsType<StreamSourceDocument>(document).Encoding);
        }

        [Fact]
        public void ReadFrom_EmptyStream_WithEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(content: string.Empty, encoding: Encoding.UTF32);

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml", Encoding.UTF32);

            // Assert
            Assert.Equal("file.cshtml", document.FilePath);
            Assert.Same(Encoding.UTF32, Assert.IsType<StreamSourceDocument>(document).Encoding);
        }

        [Fact]
        public void ReadFrom_UsesProjectItemPhysicalPath()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/test-path", "some-physical-path");

            // Act
            var document = RazorSourceDocument.ReadFrom(projectItem);

            // Assert
            Assert.Equal(projectItem.PhysicalPath, document.FilePath);
            Assert.Equal(projectItem.Content, ReadContent(document));
        }

        [Fact]
        public void ReadFrom_ProjectItem()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/test-path");

            // Act
            var document = RazorSourceDocument.ReadFrom(projectItem);

            // Assert
            Assert.Equal(projectItem.FilePath, document.FilePath);
            Assert.Equal(projectItem.Content, ReadContent(document));
        }

        [Fact]
        public void Create_WithoutEncoding()
        {
            // Arrange
            var content = "Hello world";
            var fileName = "some-file-name";

            // Act
            var document = RazorSourceDocument.Create(content, fileName);

            // Assert
            Assert.Equal(fileName, document.FilePath);
            Assert.Equal(content, ReadContent(document));
            Assert.Same(Encoding.UTF8, document.Encoding);
        }

        [Fact]
        public void Create_WithEncoding()
        {
            // Arrange
            var content = "Hello world";
            var fileName = "some-file-name";
            var encoding = Encoding.UTF32;

            // Act
            var document = RazorSourceDocument.Create(content, fileName, encoding);

            // Assert
            Assert.Equal(fileName, document.FilePath);
            Assert.Equal(content, ReadContent(document));
            Assert.Same(encoding, document.Encoding);
        }

        private static string ReadContent(RazorSourceDocument razorSourceDocument)
        {
            var buffer = new char[razorSourceDocument.Length];
            razorSourceDocument.CopyTo(0, buffer, 0, buffer.Length);

            return new string(buffer);
        }
    }
}
