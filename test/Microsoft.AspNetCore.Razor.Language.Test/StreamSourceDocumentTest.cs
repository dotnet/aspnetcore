// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class StreamSourceDocumentTest
    {
        [Fact]
        public void GetChecksum_ReturnsCopiedChecksum()
        {
            // Arrange
            var content = "Hello World!";
            var stream = CreateBOMStream(content, Encoding.UTF8);
            var document = new StreamSourceDocument(stream, Encoding.UTF8, "file.cshtml");

            // Act
            var firstChecksum = document.GetChecksum();
            var secondChecksum = document.GetChecksum();

            // Assert
            Assert.Equal(firstChecksum, secondChecksum);
            Assert.NotSame(firstChecksum, secondChecksum);
        }

        [Fact]
        public void GetChecksum_ComputesCorrectChecksum_UTF8()
        {
            // Arrange
            var content = "Hello World!";
            var stream = CreateBOMStream(content, Encoding.UTF8);
            var document = new StreamSourceDocument(stream, Encoding.UTF8, "file.cshtml");
            var expectedChecksum = new byte[] { 70, 180, 84, 105, 70, 79, 152, 31, 71, 157, 46, 159, 50, 83, 1, 243, 222, 48, 90, 18 };

            // Act
            var checksum = document.GetChecksum();

            // Assert
            Assert.Equal(expectedChecksum, checksum);
        }

        [Fact]
        public void GetChecksum_ComputesCorrectChecksum_UTF32AutoDetect()
        {
            // Arrange
            var content = "Hello World!";
            var stream = CreateBOMStream(content, Encoding.UTF32);
            var document = new StreamSourceDocument(stream, encoding: null, fileName: "file.cshtml");
            var expectedChecksum = new byte[] { 159, 154, 109, 89, 250, 163, 165, 108, 2, 112, 34, 4, 247, 161, 82, 168, 77, 213, 107, 71 };

            // Act
            var checksum = document.GetChecksum();

            // Assert
            Assert.Equal(expectedChecksum, checksum);
        }

        [Fact]
        public void ConstructedWithoutEncoding_DetectsEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(encoding: Encoding.UTF32);

            // Act
            var document = new StreamSourceDocument(content, encoding: null, fileName: "file.cshtml");

            // Assert
            Assert.IsType<StreamSourceDocument>(document);
            Assert.Equal("file.cshtml", document.FileName);
            Assert.Equal(Encoding.UTF32, document.Encoding);
        }

        [Fact]
        public void ConstructedWithoutEncoding_EmptyStream_DetectsEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(content: string.Empty, encoding: Encoding.UTF32);

            // Act
            var document = new StreamSourceDocument(content, encoding: null, fileName: "file.cshtml");

            // Assert
            Assert.IsType<StreamSourceDocument>(document);
            Assert.Equal("file.cshtml", document.FileName);
            Assert.Equal(Encoding.UTF32, document.Encoding);
        }

        [Fact]
        public void FailsOnMismatchedEncoding()
        {
            // Arrange
            var content = TestRazorSourceDocument.CreateStreamContent(encoding: Encoding.UTF32);
            var expectedMessage = Resources.FormatMismatchedContentEncoding(Encoding.UTF8.EncodingName, Encoding.UTF32.EncodingName);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => new StreamSourceDocument(content, Encoding.UTF8, "file.cshtml"));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Theory]
        [InlineData(100000)]
        [InlineData(RazorSourceDocument.LargeObjectHeapLimitInChars)]
        [InlineData(RazorSourceDocument.LargeObjectHeapLimitInChars + 2)]
        [InlineData(RazorSourceDocument.LargeObjectHeapLimitInChars * 2 - 1)]
        [InlineData(RazorSourceDocument.LargeObjectHeapLimitInChars * 2)]
        public void DetectsSizeOfStreamForLargeContent(int contentLength)
        {
            // Arrange
            var content = new string('a', contentLength);
            var stream = TestRazorSourceDocument.CreateStreamContent(content);

            // Act
            var document = new StreamSourceDocument(stream, encoding: null, fileName: "file.cshtml");

            // Assert
            var streamDocument = Assert.IsType<StreamSourceDocument>(document);
            Assert.IsType<LargeTextSourceDocument>(streamDocument._innerSourceDocument);
            Assert.Equal("file.cshtml", document.FileName);
            Assert.Same(Encoding.UTF8, document.Encoding);
            Assert.Equal(content, ReadContent(document));
        }

        private static MemoryStream CreateBOMStream(string content, Encoding encoding)
        {
            var preamble = encoding.GetPreamble();
            var contentBytes = encoding.GetBytes(content);
            var buffer = new byte[preamble.Length + contentBytes.Length];
            preamble.CopyTo(buffer, 0);
            contentBytes.CopyTo(buffer, preamble.Length);
            var stream = new MemoryStream(buffer);
            return stream;
        }

        private static string ReadContent(RazorSourceDocument razorSourceDocument)
        {
            var buffer = new char[razorSourceDocument.Length];
            razorSourceDocument.CopyTo(0, buffer, 0, buffer.Length);

            return new string(buffer);
        }
    }
}
