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
        public void ReadFrom()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent();

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml");

            // Assert
            Assert.IsType<DefaultRazorSourceDocument>(document);
            Assert.Equal("file.cshtml", document.FileName);
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
            Assert.Equal("file.cshtml", document.FileName);
            Assert.Same(Encoding.UTF32, Assert.IsType<DefaultRazorSourceDocument>(document).Encoding);
        }

        [Fact]
        public void ReadFrom_EmptyStream_WithEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(content: string.Empty, encoding: Encoding.UTF32);

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml", Encoding.UTF32);

            // Assert
            Assert.Equal("file.cshtml", document.FileName);
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
            Assert.Equal("file.cshtml", document.FileName);
            Assert.Equal(Encoding.UTF32, document.Encoding);
        }

        [Fact]
        public void ReadFrom_EmptyStream_DetectsEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(content: string.Empty, encoding: Encoding.UTF32);

            // Act
            var document = RazorSourceDocument.ReadFrom(content, "file.cshtml");

            // Assert
            Assert.IsType<DefaultRazorSourceDocument>(document);
            Assert.Equal("file.cshtml", document.FileName);
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

        [Theory]
        [InlineData(100000)]
        [InlineData(RazorSourceDocument.LargeObjectHeapLimitInChars)]
        [InlineData(RazorSourceDocument.LargeObjectHeapLimitInChars + 2)]
        [InlineData(RazorSourceDocument.LargeObjectHeapLimitInChars * 2 - 1)]
        [InlineData(RazorSourceDocument.LargeObjectHeapLimitInChars * 2)]
        public void ReadFrom_LargeContent(int contentLength)
        {
            // Arrange
            var content = new string('a', contentLength);
            var stream = TestRazorSourceDocument.CreateStreamContent(content);

            // Act
            var document = RazorSourceDocument.ReadFrom(stream, "file.cshtml");

            // Assert
            Assert.IsType<LargeTextRazorSourceDocument>(document);
            Assert.Equal("file.cshtml", document.FileName);
            Assert.Same(Encoding.UTF8, document.Encoding);
            Assert.Equal(content, ReadContent(document));
        }

        [Fact]
        public void ReadFrom_ProjectItem()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/test-path");

            // Act
            var document = RazorSourceDocument.ReadFrom(projectItem);

            // Assert
            Assert.Equal(projectItem.Path, document.FileName);
            Assert.Equal(projectItem.Content, ReadContent(document));
        }

        [Fact]
        public void ReadFrom_UsesProjectItemPhysicalPath()
        {
            // Arrange
            var projectItem = new TestRazorProjectItem("/test-path", "some-physical-path");

            // Act
            var document = RazorSourceDocument.ReadFrom(projectItem);

            // Assert
            Assert.Equal(projectItem.PhysicalPath, document.FileName);
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
            Assert.Equal(fileName, document.FileName);
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
            Assert.Equal(fileName, document.FileName);
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
