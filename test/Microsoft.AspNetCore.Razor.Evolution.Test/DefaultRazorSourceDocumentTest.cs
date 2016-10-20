// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultRazorSourceDocumentTest
    {
        [Fact]
        public void Filename()
        {
            // Arrange
            var content = CreateContent();

            // Act
            var document = new DefaultRazorSourceDocument(content, Encoding.UTF8, filename: "file.cshtml");

            // Assert
            Assert.Equal("file.cshtml", document.Filename);
        }

        [Fact]
        public void Filename_Null()
        {
            // Arrange
            var content = CreateContent();

            // Act
            var document = new DefaultRazorSourceDocument(content, Encoding.UTF8, filename: null);

            // Assert
            Assert.Null(document.Filename);
        }

        [Fact]
        public void CreateReader_WithEncoding()
        {
            // Arrange
            var content = CreateContent("Hi", encoding: Encoding.UTF8);
            var document = new DefaultRazorSourceDocument(content, Encoding.UTF8, filename: null);

            // Act
            using (var reader = document.CreateReader())
            {
                // Assert
                Assert.Equal("Hi", reader.ReadToEnd());
            }
        }

        [Fact]
        public void CreateReader_Null_DetectsEncoding()
        {
            // Arrange
            var content = CreateContent("Hi", encoding: Encoding.UTF32);
            var document = new DefaultRazorSourceDocument(content, encoding: null, filename: null);

            // Act
            using (var reader = document.CreateReader())
            {
                // Assert
                Assert.Equal("Hi", reader.ReadToEnd());
            }
        }

        [Fact]
        public void CreateReader_DisposeReader_DoesNotDirtyDocument()
        {
            // Arrange
            var content = CreateContent("Hi", encoding: Encoding.UTF32);
            var document = new DefaultRazorSourceDocument(content, encoding: null, filename: null);

            // Act & Assert
            //
            // (we should be able to do this twice to prove that the underlying data isn't disposed)
            for (var i = 0; i < 2; i++)
            {
                using (var reader = document.CreateReader())
                {
                    // Assert
                    Assert.Equal("Hi", reader.ReadToEnd());
                }
            }
        }

        private static MemoryStream CreateContent(string content = "Hello, World!", Encoding encoding = null)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
            {
                writer.Write(content);
            }

            return stream;
        }
    }
}
