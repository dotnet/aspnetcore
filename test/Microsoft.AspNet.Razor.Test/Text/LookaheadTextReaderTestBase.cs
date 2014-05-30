// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Text
{
    public abstract class LookaheadTextReaderTestBase
    {
        protected abstract LookaheadTextReader CreateReader(string testString);

        protected void RunPeekTest(string input, int peekAt = 0)
        {
            RunPeekOrReadTest(input, peekAt, false);
        }

        protected void RunReadTest(string input, int readAt = 0)
        {
            RunPeekOrReadTest(input, readAt, true);
        }

        protected void RunSourceLocationTest(string input, SourceLocation expected, int checkAt = 0)
        {
            RunSourceLocationTest(input, expected, r => AdvanceReader(checkAt, r));
        }

        protected void RunSourceLocationTest(string input, SourceLocation expected, Action<LookaheadTextReader> readerAction)
        {
            // Arrange
            LookaheadTextReader reader = CreateReader(input);
            readerAction(reader);

            // Act
            SourceLocation actual = reader.CurrentLocation;

            // Assert
            Assert.Equal(expected, actual);
        }

        protected void RunEndLookaheadUpdatesSourceLocationTest()
        {
            SourceLocation? expectedLocation = null;
            SourceLocation? actualLocation = null;

            RunLookaheadTest("abc\r\ndef\r\nghi", null,
                             Read(6),
                             CaptureSourceLocation(s => expectedLocation = s),
                             Lookahead(Read(6)),
                             CaptureSourceLocation(s => actualLocation = s));
            // Assert
            Assert.Equal(expectedLocation.Value.AbsoluteIndex, actualLocation.Value.AbsoluteIndex);
            Assert.Equal(expectedLocation.Value.CharacterIndex, actualLocation.Value.CharacterIndex);
            Assert.Equal(expectedLocation.Value.LineIndex, actualLocation.Value.LineIndex);
        }

        protected void RunReadToEndTest()
        {
            // Arrange
            LookaheadTextReader reader = CreateReader("abcdefg");

            // Act
            string str = reader.ReadToEnd();

            // Assert
            Assert.Equal("abcdefg", str);
        }

        protected void RunCancelBacktrackOutsideLookaheadTest()
        {
            // Arrange
            LookaheadTextReader reader = CreateReader("abcdefg");

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => reader.CancelBacktrack());
            Assert.Equal(RazorResources.CancelBacktrack_Must_Be_Called_Within_Lookahead, exception.Message);
        }

        protected Action<StringBuilder, LookaheadTextReader> CaptureSourceLocation(Action<SourceLocation> capture)
        {
            return (_, reader) => { capture(reader.CurrentLocation); };
        }

        protected Action<StringBuilder, LookaheadTextReader> Read(int count)
        {
            return (builder, reader) =>
            {
                for (int i = 0; i < count; i++)
                {
                    Read(builder, reader);
                }
            };
        }

        protected void Read(StringBuilder builder, LookaheadTextReader reader)
        {
            builder.Append((char)reader.Read());
        }

        protected void ReadToEnd(StringBuilder builder, LookaheadTextReader reader)
        {
            builder.Append(reader.ReadToEnd());
        }

        protected void CancelBacktrack(StringBuilder builder, LookaheadTextReader reader)
        {
            reader.CancelBacktrack();
        }

        protected Action<StringBuilder, LookaheadTextReader> Lookahead(params Action<StringBuilder, LookaheadTextReader>[] readerCommands)
        {
            return (builder, reader) =>
            {
                using (reader.BeginLookahead())
                {
                    RunAll(readerCommands, builder, reader);
                }
            };
        }

        protected void RunLookaheadTest(string input, string expected, params Action<StringBuilder, LookaheadTextReader>[] readerCommands)
        {
            // Arrange
            StringBuilder builder = new StringBuilder();
            using (LookaheadTextReader reader = CreateReader(input))
            {
                RunAll(readerCommands, builder, reader);
            }

            if (expected != null)
            {
                Assert.Equal(expected, builder.ToString());
            }
        }

        protected void RunReadUntilTest(Func<LookaheadTextReader, string> readMethod, int expectedRaw, int expectedChar, int expectedLine)
        {
            // Arrange
            LookaheadTextReader reader = CreateReader("a\r\nbcd\r\nefg");

            reader.Read(); // Reader: "\r\nbcd\r\nefg"
            reader.Read(); // Reader: "\nbcd\r\nefg"
            reader.Read(); // Reader: "bcd\r\nefg"

            // Act
            string read = null;
            SourceLocation actualLocation;
            using (reader.BeginLookahead())
            {
                read = readMethod(reader);
                actualLocation = reader.CurrentLocation;
            }

            // Assert
            Assert.Equal(3, reader.CurrentLocation.AbsoluteIndex);
            Assert.Equal(0, reader.CurrentLocation.CharacterIndex);
            Assert.Equal(1, reader.CurrentLocation.LineIndex);
            Assert.Equal(expectedRaw, actualLocation.AbsoluteIndex);
            Assert.Equal(expectedChar, actualLocation.CharacterIndex);
            Assert.Equal(expectedLine, actualLocation.LineIndex);
            Assert.Equal('b', reader.Peek());
            Assert.Equal(read, readMethod(reader));
        }

        protected void RunBufferReadTest(Func<LookaheadTextReader, char[], int, int, int> readMethod)
        {
            // Arrange
            LookaheadTextReader reader = CreateReader("abcdefg");

            reader.Read(); // Reader: "bcdefg"

            // Act
            char[] buffer = new char[4];
            int read = -1;
            SourceLocation actualLocation;
            using (reader.BeginLookahead())
            {
                read = readMethod(reader, buffer, 0, 4);
                actualLocation = reader.CurrentLocation;
            }

            // Assert
            Assert.Equal("bcde", new String(buffer));
            Assert.Equal(4, read);
            Assert.Equal(5, actualLocation.AbsoluteIndex);
            Assert.Equal(5, actualLocation.CharacterIndex);
            Assert.Equal(0, actualLocation.LineIndex);
            Assert.Equal(1, reader.CurrentLocation.CharacterIndex);
            Assert.Equal(0, reader.CurrentLocation.LineIndex);
            Assert.Equal('b', reader.Peek());
        }

        private static void RunAll(Action<StringBuilder, LookaheadTextReader>[] readerCommands, StringBuilder builder, LookaheadTextReader reader)
        {
            foreach (Action<StringBuilder, LookaheadTextReader> readerCommand in readerCommands)
            {
                readerCommand(builder, reader);
            }
        }

        private void RunPeekOrReadTest(string input, int offset, bool isRead)
        {
            using (LookaheadTextReader reader = CreateReader(input))
            {
                AdvanceReader(offset, reader);

                // Act
                int? actual = null;
                if (isRead)
                {
                    actual = reader.Read();
                }
                else
                {
                    actual = reader.Peek();
                }

                Assert.NotNull(actual);

                // Asserts
                AssertReaderValueCorrect(actual.Value, input, offset, "Peek");

                if (isRead)
                {
                    AssertReaderValueCorrect(reader.Peek(), input, offset + 1, "Read");
                }
                else
                {
                    Assert.Equal(actual, reader.Peek());
                }
            }
        }

        private static void AdvanceReader(int offset, LookaheadTextReader reader)
        {
            for (int i = 0; i < offset; i++)
            {
                reader.Read();
            }
        }

        private void AssertReaderValueCorrect(int actual, string input, int expectedOffset, string methodName)
        {
            if (expectedOffset < input.Length)
            {
                Assert.Equal(input[expectedOffset], actual);
            }
            else
            {
                Assert.Equal(-1, actual);
            }
        }
    }
}
