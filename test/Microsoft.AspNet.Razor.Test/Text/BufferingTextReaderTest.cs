// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Text
{
    public class BufferingTextReaderTest : LookaheadTextReaderTestBase
    {
        private const string TestString = "abcdefg";

        private class DisposeTestMockTextReader : TextReader
        {
            public bool Disposed { get; set; }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                Disposed = true;
            }
        }

        protected override LookaheadTextReader CreateReader(string testString)
        {
            return new BufferingTextReader(new StringReader(testString));
        }

        [Fact]
        public void ConstructorRequiresNonNullSourceReader()
        {
            Assert.Throws<ArgumentNullException>("source", () => new BufferingTextReader(null));
        }

        [Fact]
        public void PeekReturnsCurrentCharacterWithoutAdvancingPosition()
        {
            RunPeekTest("abc", peekAt: 2);
        }

        [Fact]
        public void PeekReturnsNegativeOneAtEndOfSourceReader()
        {
            RunPeekTest("abc", peekAt: 3);
        }

        [Fact]
        public void ReadReturnsCurrentCharacterAndAdvancesToNextCharacter()
        {
            RunReadTest("abc", readAt: 2);
        }

        [Fact]
        public void EndingLookaheadReturnsReaderToPreviousLocation()
        {
            RunLookaheadTest("abcdefg", "abcb",
                             Read,
                             Lookahead(
                                 Read,
                                 Read),
                             Read);
        }

        [Fact]
        public void MultipleLookaheadsCanBePerformed()
        {
            RunLookaheadTest("abcdefg", "abcbcdc",
                             Read,
                             Lookahead(
                                 Read,
                                 Read),
                             Read,
                             Lookahead(
                                 Read,
                                 Read),
                             Read);
        }

        [Fact]
        public void LookaheadsCanBeNested()
        {
            RunLookaheadTest("abcdefg", "abcdefebc",
                             Read, // Appended: "a" Reader: "bcdefg"
                             Lookahead( // Reader: "bcdefg"
                                 Read, // Appended: "b" Reader: "cdefg";
                                 Read, // Appended: "c" Reader: "defg";
                                 Read, // Appended: "d" Reader: "efg";
                                 Lookahead( // Reader: "efg"
                                     Read, // Appended: "e" Reader: "fg";
                                     Read // Appended: "f" Reader: "g";
                                     ), // Reader: "efg"
                                 Read // Appended: "e" Reader: "fg";
                                 ), // Reader: "bcdefg"
                             Read, // Appended: "b" Reader: "cdefg";
                             Read); // Appended: "c" Reader: "defg";
        }

        [Fact]
        public void SourceLocationIsZeroWhenInitialized()
        {
            RunSourceLocationTest("abcdefg", SourceLocation.Zero, checkAt: 0);
        }

        [Fact]
        public void CharacterAndAbsoluteIndicesIncreaseAsCharactersAreRead()
        {
            RunSourceLocationTest("abcdefg", new SourceLocation(4, 0, 4), checkAt: 4);
        }

        [Fact]
        public void CharacterAndAbsoluteIndicesIncreaseAsSlashRInTwoCharacterNewlineIsRead()
        {
            RunSourceLocationTest("f\r\nb", new SourceLocation(2, 0, 2), checkAt: 2);
        }

        [Fact]
        public void CharacterIndexResetsToZeroAndLineIndexIncrementsWhenSlashNInTwoCharacterNewlineIsRead()
        {
            RunSourceLocationTest("f\r\nb", new SourceLocation(3, 1, 0), checkAt: 3);
        }

        [Fact]
        public void CharacterIndexResetsToZeroAndLineIndexIncrementsWhenSlashRInSingleCharacterNewlineIsRead()
        {
            RunSourceLocationTest("f\rb", new SourceLocation(2, 1, 0), checkAt: 2);
        }

        [Fact]
        public void CharacterIndexResetsToZeroAndLineIndexIncrementsWhenSlashNInSingleCharacterNewlineIsRead()
        {
            RunSourceLocationTest("f\nb", new SourceLocation(2, 1, 0), checkAt: 2);
        }

        [Fact]
        public void EndingLookaheadResetsRawCharacterAndLineIndexToValuesWhenLookaheadBegan()
        {
            RunEndLookaheadUpdatesSourceLocationTest();
        }

        [Fact]
        public void OnceBufferingBeginsReadsCanContinuePastEndOfBuffer()
        {
            RunLookaheadTest("abcdefg", "abcbcdefg",
                             Read,
                             Lookahead(Read(2)),
                             Read(2),
                             ReadToEnd);
        }

        [Fact]
        public void DisposeDisposesSourceReader()
        {
            RunDisposeTest(r => r.Dispose());
        }

#if !ASPNETCORE50
        [Fact]
        public void CloseDisposesSourceReader()
        {
            RunDisposeTest(r => r.Close());
        }
#endif

        [Fact]
        public void ReadWithBufferSupportsLookahead()
        {
            RunBufferReadTest((reader, buffer, index, count) => reader.Read(buffer, index, count));
        }

        [Fact]
        public void ReadBlockSupportsLookahead()
        {
            RunBufferReadTest((reader, buffer, index, count) => reader.ReadBlock(buffer, index, count));
        }

        [Fact]
        public void ReadLineSupportsLookahead()
        {
            RunReadUntilTest(r => r.ReadLine(), expectedRaw: 8, expectedChar: 0, expectedLine: 2);
        }

        [Fact]
        public void ReadToEndSupportsLookahead()
        {
            RunReadUntilTest(r => r.ReadToEnd(), expectedRaw: 11, expectedChar: 3, expectedLine: 2);
        }

        [Fact]
        public void ReadLineMaintainsCorrectCharacterPosition()
        {
            RunSourceLocationTest("abc\r\ndef", new SourceLocation(5, 1, 0), r => r.ReadLine());
        }

        [Fact]
        public void ReadToEndWorksAsInNormalTextReader()
        {
            RunReadToEndTest();
        }

        [Fact]
        public void CancelBacktrackStopsNextEndLookaheadFromBacktracking()
        {
            RunLookaheadTest("abcdefg", "abcdefg",
                             Lookahead(
                                 Read(2),
                                 CancelBacktrack
                                 ),
                             ReadToEnd);
        }

        [Fact]
        public void CancelBacktrackThrowsInvalidOperationExceptionIfCalledOutsideOfLookahead()
        {
            RunCancelBacktrackOutsideLookaheadTest();
        }

        [Fact]
        public void CancelBacktrackOnlyCancelsBacktrackingForInnermostNestedLookahead()
        {
            RunLookaheadTest("abcdefg", "abcdabcdefg",
                             Lookahead(
                                 Read(2),
                                 Lookahead(
                                     Read,
                                     CancelBacktrack
                                     ),
                                 Read
                                 ),
                             ReadToEnd);
        }

        [Fact]
        public void BacktrackBufferIsClearedWhenEndReachedAndNoCurrentLookaheads()
        {
            // Arrange
            var source = new StringReader(TestString);
            var reader = new BufferingTextReader(source);

            reader.Read(); // Reader: "bcdefg"
            using (reader.BeginLookahead())
            {
                reader.Read(); // Reader: "cdefg"
            } // Reader: "bcdefg"
            reader.Read(); // Reader: "cdefg"
            Assert.NotNull(reader.Buffer); // Verify our assumption that the buffer still exists

            // Act
            reader.Read();

            // Assert
            Assert.False(reader.Buffering, "The buffer was not reset when the end was reached");
            Assert.Equal(0, reader.Buffer.Length);
        }

        private static void RunDisposeTest(Action<LookaheadTextReader> triggerAction)
        {
            // Arrange
            var source = new DisposeTestMockTextReader();
            var reader = new BufferingTextReader(source);

            // Act
            triggerAction(reader);

            // Assert
            Assert.True(source.Disposed);
        }
    }
}
