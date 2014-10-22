// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Razor.Parser;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Text
{
    public class TextReaderExtensionsTest
    {
        [Fact]
        public void ReadUntilWithCharThrowsArgNullIfReaderNull()
        {
            Assert.Throws<ArgumentNullException>("reader", () => TextReaderExtensions.ReadUntil(null, '@'));
        }

        [Fact]
        public void ReadUntilInclusiveWithCharThrowsArgNullIfReaderNull()
        {
            Assert.Throws<ArgumentNullException>("reader", () => TextReaderExtensions.ReadUntil(null, '@', inclusive: true));
        }

        [Fact]
        public void ReadUntilWithMultipleTerminatorsThrowsArgNullIfReaderNull()
        {
            Assert.Throws<ArgumentNullException>("reader", () => TextReaderExtensions.ReadUntil(null, '/', '>'));
        }

        [Fact]
        public void ReadUntilInclusiveWithMultipleTerminatorsThrowsArgNullIfReaderNull()
        {
            // NOTE: Using named parameters would be difficult here, hence the inline comment
            Assert.Throws<ArgumentNullException>("reader", () => TextReaderExtensions.ReadUntil(null, /* inclusive */ true, '/', '>'));
        }

        [Fact]
        public void ReadUntilWithPredicateThrowsArgNullIfReaderNull()
        {
            Assert.Throws<ArgumentNullException>("reader", () => TextReaderExtensions.ReadUntil(null, c => true));
        }

        [Fact]
        public void ReadUntilInclusiveWithPredicateThrowsArgNullIfReaderNull()
        {
            Assert.Throws<ArgumentNullException>("reader", () => TextReaderExtensions.ReadUntil(null, c => true, inclusive: true));
        }

        [Fact]
        public void ReadUntilWithPredicateThrowsArgExceptionIfPredicateNull()
        {
            Assert.Throws<ArgumentNullException>("condition", () => TextReaderExtensions.ReadUntil(new StringReader("Foo"), (Predicate<char>)null));
        }

        [Fact]
        public void ReadUntilInclusiveWithPredicateThrowsArgExceptionIfPredicateNull()
        {
            Assert.Throws<ArgumentNullException>("condition", () => TextReaderExtensions.ReadUntil(new StringReader("Foo"), (Predicate<char>)null, inclusive: true));
        }

        [Fact]
        public void ReadWhileWithPredicateThrowsArgNullIfReaderNull()
        {
            Assert.Throws<ArgumentNullException>("reader", () => TextReaderExtensions.ReadWhile(null, c => true));
        }

        [Fact]
        public void ReadWhileInclusiveWithPredicateThrowsArgNullIfReaderNull()
        {
            Assert.Throws<ArgumentNullException>("reader", () => TextReaderExtensions.ReadWhile(null, c => true, inclusive: true));
        }

        [Fact]
        public void ReadWhileWithPredicateThrowsArgNullIfPredicateNull()
        {
            Assert.Throws<ArgumentNullException>("condition", () => TextReaderExtensions.ReadWhile(new StringReader("Foo"), (Predicate<char>)null));
        }

        [Fact]
        public void ReadWhileInclusiveWithPredicateThrowsArgNullIfPredicateNull()
        {
            Assert.Throws<ArgumentNullException>("condition", () => TextReaderExtensions.ReadWhile(new StringReader("Foo"), (Predicate<char>)null, inclusive: true));
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
            var reader = new StringReader(testString);

            // Act
            var read = action(reader);

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
