using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test
{
    public class LargeTextRazorSourceDocumentTest
    {
        private const int ChunkTestLength = 10;

        [Theory]
        [InlineData(ChunkTestLength - 1)]
        [InlineData(ChunkTestLength)]
        [InlineData(ChunkTestLength + 1)]
        [InlineData(ChunkTestLength * 2 - 1)]
        [InlineData(ChunkTestLength * 2)]
        [InlineData(ChunkTestLength * 2 + 1)]
        public void Indexer_ProvidesCharacterAccessToContent(int contentLength)
        {
            // Arrange
            var content = new char[contentLength];

            for (var i = 0; i < contentLength - 1; i++)
            {
                content[i] = 'a';
            }
            content[contentLength - 1] = 'b';
            var contentString = new string(content);

            var stream = TestRazorSourceDocument.CreateStreamContent(new string(content));
            var reader = new StreamReader(stream, true);
            var document = new LargeTextRazorSourceDocument(reader, ChunkTestLength, Encoding.UTF8, "file.cshtml");

            // Act
            var output = new char[contentLength];
            for (var i = 0; i < document.Length; i++)
            {
                output[i] = document[i];
            }
            var outputString = new string(output);

            // Assert
            Assert.Equal(contentLength, document.Length);
            Assert.Equal(contentString, outputString);
        }

        [Theory]
        [InlineData("test.cshtml")]
        [InlineData(null)]
        public void Filename(string fileName)
        {
            // Arrange
            var stream = TestRazorSourceDocument.CreateStreamContent("abc");
            var reader = new StreamReader(stream, true);

            // Act
            var document = new LargeTextRazorSourceDocument(reader, ChunkTestLength, Encoding.UTF8, fileName);

            // Assert
            Assert.Equal(fileName, document.FileName);
        }

        [Fact]
        public void Lines()
        {
            // Arrange
            var stream = TestRazorSourceDocument.CreateStreamContent("abc\ndef\nghi");
            var reader = new StreamReader(stream, true);

            // Act
            var document = new LargeTextRazorSourceDocument(reader, ChunkTestLength, Encoding.UTF8, "file.cshtml");

            // Assert
            Assert.Equal(3, document.Lines.Count);
        }

        [Theory]
        [InlineData("", 0, 0, 0)]                                   // Nothing to copy
        [InlineData("a", 0, 100, 1)]                                // Destination index different from start
        [InlineData("j", ChunkTestLength - 1, 0, 1)]                // One char just before the chunk limit
        [InlineData("k", ChunkTestLength, 0, 1)]                    // One char one the chunk limit
        [InlineData("l", ChunkTestLength + 1, 0, 1)]                // One char just after the chunk limit
        [InlineData("jk", ChunkTestLength - 1, 0, 2)]               // Two char that are on both chunk sides
        [InlineData("abcdefghijklmnopqrstuvwxy", 0, 100, 25)]       // Everything except the last
        [InlineData("abcdefghijklmnopqrstuvwxyz", 0, 0, 26)]        // Copy all
        [InlineData("xyz", 23, 0, 3)]                               // The last chars
        public void CopyTo(string expected, int sourceIndex, int destinationIndex, int count)
        {
            // Arrange
            var stream = TestRazorSourceDocument.CreateStreamContent("abcdefghijklmnopqrstuvwxyz");

            var reader = new StreamReader(stream, true);
            var document = new LargeTextRazorSourceDocument(reader, ChunkTestLength, Encoding.UTF8, "file.cshtml");

            // Act
            var destination = new char[1000];
            document.CopyTo(sourceIndex, destination, destinationIndex, count);

            // Assert
            var copy = new string(destination, destinationIndex, count);
            Assert.Equal(expected, copy);
        }
    }
}
