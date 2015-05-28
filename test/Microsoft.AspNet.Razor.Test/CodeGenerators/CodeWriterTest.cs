// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    public class CodeWriterTest
    {
        // The length of the newline string written by writer.WriteLine.
        private static readonly int WriterNewLineLength = Environment.NewLine.Length;

        public static IEnumerable<object[]> NewLines
        {
            get
            {
                return new object[][]
                {
                    new object[] { "\r" },
                    new object[] { "\n" },
                    new object[] { "\r\n" },
                };
            }
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithWrite()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("1234");

            // Assert
            var location = writer.GetCurrentSourceLocation();
            var expected = new SourceLocation(absoluteIndex: 4, lineIndex: 0, characterIndex: 4);

            Assert.Equal(expected, location);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithIndent()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.WriteLine();
            writer.Indent(size: 3);

            // Assert
            var location = writer.GetCurrentSourceLocation();
            var expected = new SourceLocation(absoluteIndex: 3 + WriterNewLineLength, lineIndex: 1, characterIndex: 3);

            Assert.Equal(expected, location);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithWriteLine()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.WriteLine("1234");

            // Assert
            var location = writer.GetCurrentSourceLocation();

            var expected = new SourceLocation(absoluteIndex: 4 + WriterNewLineLength, lineIndex: 1, characterIndex: 0);

            Assert.Equal(expected, location);
        }

        [Theory]
        [MemberData("NewLines")]
        public void CodeWriter_TracksPosition_WithWriteLine_WithNewLineInContent(string newLine)
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.WriteLine("1234" + newLine + "12");

            // Assert
            var location = writer.GetCurrentSourceLocation();

            var expected = new SourceLocation(
                absoluteIndex: 6 + newLine.Length + WriterNewLineLength,
                lineIndex: 2,
                characterIndex: 0);

            Assert.Equal(expected, location);
        }

        [Theory]
        [MemberData("NewLines")]
        public void CodeWriter_TracksPosition_WithWrite_WithNewlineInContent(string newLine)
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("1234" + newLine + "123" + newLine + "12");

            // Assert
            var location = writer.GetCurrentSourceLocation();

            var expected = new SourceLocation(
                absoluteIndex: 9 + newLine.Length + newLine.Length,
                lineIndex: 2,
                characterIndex: 2);

            Assert.Equal(expected, location);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithWrite_WithNewlineInContent_RepeatedN()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("1234\n\n123");

            // Assert
            var location = writer.GetCurrentSourceLocation();

            var expected = new SourceLocation(
                absoluteIndex: 9,
                lineIndex: 2,
                characterIndex: 3);

            Assert.Equal(expected, location);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithWrite_WithMixedNewlineInContent()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("1234\r123\r\n12\n1");

            // Assert
            var location = writer.GetCurrentSourceLocation();

            var expected = new SourceLocation(
                absoluteIndex: 14,
                lineIndex: 3,
                characterIndex: 1);

            Assert.Equal(expected, location);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithNewline_SplitAcrossWrites()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("1234\r");
            var location1 = writer.GetCurrentSourceLocation();

            writer.Write("\n");
            var location2 = writer.GetCurrentSourceLocation();

            // Assert
            var expected1 = new SourceLocation(absoluteIndex: 5, lineIndex: 1, characterIndex: 0);
            Assert.Equal(expected1, location1);

            var expected2 = new SourceLocation(absoluteIndex: 6, lineIndex: 1, characterIndex: 0);
            Assert.Equal(expected2, location2);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithTwoNewline_SplitAcrossWrites_R()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("1234\r");
            var location1 = writer.GetCurrentSourceLocation();

            writer.Write("\r");
            var location2 = writer.GetCurrentSourceLocation();

            // Assert
            var expected1 = new SourceLocation(absoluteIndex: 5, lineIndex: 1, characterIndex: 0);
            Assert.Equal(expected1, location1);

            var expected2 = new SourceLocation(absoluteIndex: 6, lineIndex: 2, characterIndex: 0);
            Assert.Equal(expected2, location2);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithTwoNewline_SplitAcrossWrites_N()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("1234\n");
            var location1 = writer.GetCurrentSourceLocation();

            writer.Write("\n");
            var location2 = writer.GetCurrentSourceLocation();

            // Assert
            var expected1 = new SourceLocation(absoluteIndex: 5, lineIndex: 1, characterIndex: 0);
            Assert.Equal(expected1, location1);

            var expected2 = new SourceLocation(absoluteIndex: 6, lineIndex: 2, characterIndex: 0);
            Assert.Equal(expected2, location2);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithTwoNewline_SplitAcrossWrites_Reversed()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("1234\n");
            var location1 = writer.GetCurrentSourceLocation();

            writer.Write("\r");
            var location2 = writer.GetCurrentSourceLocation();

            // Assert
            var expected1 = new SourceLocation(absoluteIndex: 5, lineIndex: 1, characterIndex: 0);
            Assert.Equal(expected1, location1);

            var expected2 = new SourceLocation(absoluteIndex: 6, lineIndex: 2, characterIndex: 0);
            Assert.Equal(expected2, location2);
        }

        [Fact]
        public void CodeWriter_TracksPosition_WithNewline_SplitAcrossWrites_AtBeginning()
        {
            // Arrange
            var writer = new CodeWriter();

            // Act
            writer.Write("\r");
            var location1 = writer.GetCurrentSourceLocation();

            writer.Write("\n");
            var location2 = writer.GetCurrentSourceLocation();

            // Assert
            var expected1 = new SourceLocation(absoluteIndex: 1, lineIndex: 1, characterIndex: 0);
            Assert.Equal(expected1, location1);

            var expected2 = new SourceLocation(absoluteIndex: 2, lineIndex: 1, characterIndex: 0);
            Assert.Equal(expected2, location2);
        }
    }
}