// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Text
{
    public class TextReaderExtensionsTest
    {
        [Fact]
        public void ReadUntilWithCharThrowsArgNullIfReaderNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadUntil(null, '@'), "reader");
        }

        [Fact]
        public void ReadUntilInclusiveWithCharThrowsArgNullIfReaderNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadUntil(null, '@', inclusive: true), "reader");
        }

        [Fact]
        public void ReadUntilWithMultipleTerminatorsThrowsArgNullIfReaderNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadUntil(null, '/', '>'), "reader");
        }

        [Fact]
        public void ReadUntilInclusiveWithMultipleTerminatorsThrowsArgNullIfReaderNull()
        {
            // NOTE: Using named parameters would be difficult here, hence the inline comment
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadUntil(null, /* inclusive */ true, '/', '>'), "reader");
        }

        [Fact]
        public void ReadUntilWithPredicateThrowsArgNullIfReaderNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadUntil(null, c => true), "reader");
        }

        [Fact]
        public void ReadUntilInclusiveWithPredicateThrowsArgNullIfReaderNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadUntil(null, c => true, inclusive: true), "reader");
        }

        [Fact]
        public void ReadUntilWithPredicateThrowsArgExceptionIfPredicateNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadUntil(new StringReader("Foo"), (Predicate<char>)null), "condition");
        }

        [Fact]
        public void ReadUntilInclusiveWithPredicateThrowsArgExceptionIfPredicateNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadUntil(new StringReader("Foo"), (Predicate<char>)null, inclusive: true), "condition");
        }

        [Fact]
        public void ReadWhileWithPredicateThrowsArgNullIfReaderNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadWhile(null, c => true), "reader");
        }

        [Fact]
        public void ReadWhileInclusiveWithPredicateThrowsArgNullIfReaderNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadWhile(null, c => true, inclusive: true), "reader");
        }

        [Fact]
        public void ReadWhileWithPredicateThrowsArgNullIfPredicateNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadWhile(new StringReader("Foo"), (Predicate<char>)null), "condition");
        }

        [Fact]
        public void ReadWhileInclusiveWithPredicateThrowsArgNullIfPredicateNull()
        {
            Assert.ThrowsArgumentNull(() => TextReaderExtensions.ReadWhile(new StringReader("Foo"), (Predicate<char>)null, inclusive: true), "condition");
        }

        [Fact]
        public void ReadUntilWithCharReadsAllTextUpToSpecifiedCharacterButNotPast()
        {
            RunReaderTest("foo bar baz @biz", "foo bar baz ", '@', r => r.ReadUntil('@'));
        }

        [Fact]
        public void ReadUntilWithCharWithInclusiveFlagReadsAllTextUpToSpecifiedCharacterButNotPastIfInclusiveFalse()
        {
            RunReaderTest("foo bar baz @biz", "foo bar baz ", '@', r => r.ReadUntil('@', inclusive: false));
        }

        [Fact]
        public void ReadUntilWithCharWithInclusiveFlagReadsAllTextUpToAndIncludingSpecifiedCharacterIfInclusiveTrue()
        {
            RunReaderTest("foo bar baz @biz", "foo bar baz @", 'b', r => r.ReadUntil('@', inclusive: true));
        }

        [Fact]
        public void ReadUntilWithCharReadsToEndIfSpecifiedCharacterNotFound()
        {
            RunReaderTest("foo bar baz", "foo bar baz", -1, r => r.ReadUntil('@'));
        }

        [Fact]
        public void ReadUntilWithMultipleTerminatorsReadsUntilAnyTerminatorIsFound()
        {
            RunReaderTest("<bar/>", "<bar", '/', r => r.ReadUntil('/', '>'));
        }

        [Fact]
        public void ReadUntilWithMultipleTerminatorsHonorsInclusiveFlagWhenFalse()
        {
            // NOTE: Using named parameters would be difficult here, hence the inline comment
            RunReaderTest("<bar/>", "<bar", '/', r => r.ReadUntil(/* inclusive */ false, '/', '>'));
        }

        [Fact]
        public void ReadUntilWithMultipleTerminatorsHonorsInclusiveFlagWhenTrue()
        {
            // NOTE: Using named parameters would be difficult here, hence the inline comment
            RunReaderTest("<bar/>", "<bar/", '>', r => r.ReadUntil(/* inclusive */ true, '/', '>'));
        }

        [Fact]
        public void ReadUntilWithPredicateStopsWhenPredicateIsTrue()
        {
            RunReaderTest("foo bar baz 0 zoop zork zoink", "foo bar baz ", '0', r => r.ReadUntil(c => Char.IsDigit(c)));
        }

        [Fact]
        public void ReadUntilWithPredicateHonorsInclusiveFlagWhenFalse()
        {
            RunReaderTest("foo bar baz 0 zoop zork zoink", "foo bar baz ", '0', r => r.ReadUntil(c => Char.IsDigit(c), inclusive: false));
        }

        [Fact]
        public void ReadUntilWithPredicateHonorsInclusiveFlagWhenTrue()
        {
            RunReaderTest("foo bar baz 0 zoop zork zoink", "foo bar baz 0", ' ', r => r.ReadUntil(c => Char.IsDigit(c), inclusive: true));
        }

        [Fact]
        public void ReadWhileWithPredicateStopsWhenPredicateIsFalse()
        {
            RunReaderTest("012345a67890", "012345", 'a', r => r.ReadWhile(c => Char.IsDigit(c)));
        }

        [Fact]
        public void ReadWhileWithPredicateHonorsInclusiveFlagWhenFalse()
        {
            RunReaderTest("012345a67890", "012345", 'a', r => r.ReadWhile(c => Char.IsDigit(c), inclusive: false));
        }

        [Fact]
        public void ReadWhileWithPredicateHonorsInclusiveFlagWhenTrue()
        {
            RunReaderTest("012345a67890", "012345a", '6', r => r.ReadWhile(c => Char.IsDigit(c), inclusive: true));
        }

        private static void RunReaderTest(string testString, string expectedOutput, int expectedPeek, Func<TextReader, string> action)
        {
            // Arrange
            StringReader reader = new StringReader(testString);

            // Act
            string read = action(reader);

            // Assert
            Assert.Equal(expectedOutput, read);

            if (expectedPeek == -1)
            {
                Assert.True(reader.Peek() == -1, "Expected that the reader would be positioned at the end of the input stream");
            }
            else
            {
                Assert.Equal((char)expectedPeek, (char)reader.Peek());
            }
        }
    }
}
