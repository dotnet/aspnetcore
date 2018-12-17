using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test
{
    public class LargeTextSourceDocumentTest
    {
        private const int ChunkTestLength = 10;

        [Fact]
        public void GetChecksum_ReturnsCopiedChecksum()
        {
            // Arrange
            var contentString = "Hello World";
            var stream = TestRazorSourceDocument.CreateStreamContent(contentString);
            var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var document = new LargeTextSourceDocument(reader, 5, Encoding.UTF8, RazorSourceDocumentProperties.Default);

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
            var contentString = "Hello World";
            var stream = TestRazorSourceDocument.CreateStreamContent(contentString);
            var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var document = new LargeTextSourceDocument(reader, 5, Encoding.UTF8, RazorSourceDocumentProperties.Default);
            var expectedChecksum = new byte[] { 10, 77, 85, 168, 215, 120, 229, 2, 47, 171, 112, 25, 119, 197, 216, 64, 187, 196, 134, 208 };

            // Act
            var checksum = document.GetChecksum();

            // Assert
            Assert.Equal(expectedChecksum, checksum);
        }

        [Fact]
        public void GetChecksum_ComputesCorrectChecksum_UTF32()
        {
            // Arrange
            var contentString = "Hello World";
            var stream = TestRazorSourceDocument.CreateStreamContent(contentString, Encoding.UTF32);
            var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var document = new LargeTextSourceDocument(reader, 5, Encoding.UTF32, RazorSourceDocumentProperties.Default);
            var expectedChecksum = new byte[] { 108, 172, 130, 171, 42, 19, 155, 176, 211, 80, 224, 121, 169, 133, 25, 134, 48, 228, 199, 141 };

            // Act
            var checksum = document.GetChecksum();

            // Assert
            Assert.Equal(expectedChecksum, checksum);
        }

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
            var document = new LargeTextSourceDocument(reader, ChunkTestLength, Encoding.UTF8, RazorSourceDocumentProperties.Default);

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
        public void FilePath(string filePath)
        {
            // Arrange
            var stream = TestRazorSourceDocument.CreateStreamContent("abc");
            var reader = new StreamReader(stream, true);

            // Act
            var document = new LargeTextSourceDocument(reader, ChunkTestLength, Encoding.UTF8, new RazorSourceDocumentProperties(filePath: filePath, relativePath: null));

            // Assert
            Assert.Equal(filePath, document.FilePath);
        }

        [Theory]
        [InlineData("test.cshtml")]
        [InlineData(null)]
        public void RelativePath(string relativePath)
        {
            // Arrange
            var stream = TestRazorSourceDocument.CreateStreamContent("abc");
            var reader = new StreamReader(stream, true);

            // Act
            var document = new LargeTextSourceDocument(reader, ChunkTestLength, Encoding.UTF8, new RazorSourceDocumentProperties(filePath: null, relativePath: relativePath));

            // Assert
            Assert.Equal(relativePath, document.RelativePath);
        }

        [Fact]
        public void Lines()
        {
            // Arrange
            var stream = TestRazorSourceDocument.CreateStreamContent("abc\ndef\nghi");
            var reader = new StreamReader(stream, true);

            // Act
            var document = new LargeTextSourceDocument(reader, ChunkTestLength, Encoding.UTF8, RazorSourceDocumentProperties.Default);

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
            var document = new LargeTextSourceDocument(reader, ChunkTestLength, Encoding.UTF8, RazorSourceDocumentProperties.Default);

            // Act
            var destination = new char[1000];
            document.CopyTo(sourceIndex, destination, destinationIndex, count);

            // Assert
            var copy = new string(destination, destinationIndex, count);
            Assert.Equal(expected, copy);
        }
    }
}
