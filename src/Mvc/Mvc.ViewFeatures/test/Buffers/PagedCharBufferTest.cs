// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

public class PagedCharBufferTest
{
    [Fact]
    public void AppendWithChar_AddsCharacterToPage()
    {
        // Arrange
        var charToAppend = 't';
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(charToAppend);

        // Assert
        var page = Assert.Single(buffer.Pages);
        Assert.Equal(1, buffer.Length);
        Assert.Equal(charToAppend, page[buffer.Length - 1]);
    }

    [Fact]
    public void AppendWithChar_AddsCharacterToNewPage()
    {
        // Arrange
        var stringToAppend = new string('a', PagedCharBuffer.PageSize);
        var charToAppend = 't';
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(stringToAppend);
        buffer.Append(charToAppend);

        // Assert
        Assert.Collection(buffer.Pages,
            page => Assert.Equal(stringToAppend.ToCharArray(), page),
            page => Assert.Equal(charToAppend, page[0]));
        Assert.Equal(1 + PagedCharBuffer.PageSize, buffer.Length);
    }

    [Fact]
    public void AppendWithChar_AppendsToTheCurrentPageIfItIsNotFull()
    {
        // Arrange
        var stringToAppend = "abc";
        var charToAppend = 't';
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(stringToAppend);
        buffer.Append(charToAppend);

        // Assert
        var page = Assert.Single(buffer.Pages);
        Assert.Equal(new[] { 'a', 'b', 'c', 't' }, page.Take(4));
        Assert.Equal(4, buffer.Length);
    }

    [Fact]
    public void AppendWithChar_AppendsLastCharacterToTheCurrentPage()
    {
        // Arrange
        var stringToAppend = new string('a', PagedCharBuffer.PageSize - 1);
        var charToAppend = 't';
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(stringToAppend);
        buffer.Append(charToAppend);

        // Assert
        Assert.Equal(PagedCharBuffer.PageSize, buffer.Length);
        var page = Assert.Single(buffer.Pages);
        Assert.Equal(stringToAppend.ToCharArray(), page.Take(PagedCharBuffer.PageSize - 1));
        Assert.Equal('t', page[PagedCharBuffer.PageSize - 1]);
    }

    [Fact]
    public void AppendWithChar_AddsCharacterToNewPage_AfterLengthFallback()
    {
        // Arrange
        // Imitate ArrayPool<char>.Shared after it falls back from its default char[1024] Bucket to the next one.
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource
            .Setup(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(() => new char[2 * PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        var stringToAppend = new string('a', 2 * PagedCharBuffer.PageSize);
        var charToAppend = 't';

        // Act
        buffer.Append(stringToAppend);
        buffer.Append(charToAppend);

        // Assert
        Assert.Equal(2 * PagedCharBuffer.PageSize + 1, buffer.Length);
        Assert.Collection(buffer.Pages,
            page => Assert.Equal(stringToAppend.ToCharArray(), page),
            page => Assert.Equal(charToAppend, page[0]));
    }

    [Fact]
    public void AppendWithChar_AppendsLastCharacterToTheCurrentPage_AfterLengthFallback()
    {
        // Arrange
        // Imitate ArrayPool<char>.Shared after it falls back from its default char[1024] Bucket to the next one.
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource
            .Setup(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(new char[2 * PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        var stringToAppend = new string('a', 2 * PagedCharBuffer.PageSize - 1);
        var charToAppend = 't';

        // Act
        buffer.Append(stringToAppend);
        buffer.Append(charToAppend);

        // Assert
        Assert.Equal(2 * PagedCharBuffer.PageSize, buffer.Length);
        var page = Assert.Single(buffer.Pages);
        Assert.Equal(stringToAppend.ToCharArray(), page.Take(2 * PagedCharBuffer.PageSize - 1));
        Assert.Equal('t', page[2 * PagedCharBuffer.PageSize - 1]);
    }

    [Fact]
    public void AppendWithString_AppendsToPage()
    {
        // Arrange
        var stringToAppend = "abc";
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(stringToAppend);

        // Assert
        Assert.Equal(3, buffer.Length);
        var page = Assert.Single(buffer.Pages);
        Assert.Equal(new[] { 'a', 'b', 'c' }, page.Take(buffer.Length));
    }

    [Fact]
    public void AppendWithString_AppendsToMultiplePages()
    {
        // Arrange
        var length = 2 * PagedCharBuffer.PageSize + 1;
        var expected = Enumerable.Repeat('d', PagedCharBuffer.PageSize);
        var stringToAppend = new string('d', length);
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(stringToAppend);

        // Assert
        Assert.Equal(length, buffer.Length);
        Assert.Collection(
            buffer.Pages,
            page => Assert.Equal(expected, page),
            page => Assert.Equal(expected, page),
            page => Assert.Equal('d', page[0]));
    }

    [Fact]
    public void AppendWithString_AppendsToMultiplePages_AsLengthFallsBack()
    {
        // Arrange
        // Imitate ArrayPool<char>.Shared as it falls back from its default char[1024] Bucket to the next one.
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource
            .SetupSequence(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(new char[PagedCharBuffer.PageSize])
            .Returns(new char[2 * PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        // Request enough space that transition should occur.
        var length = 2 * PagedCharBuffer.PageSize + 1;
        var expected1 = Enumerable.Repeat('d', PagedCharBuffer.PageSize);
        var expected2 = Enumerable.Repeat('d', PagedCharBuffer.PageSize + 1);
        var laterString = new string('d', PagedCharBuffer.PageSize);

        // Act (loop within first Append(string) call).
        buffer.Append('d');
        buffer.Append(laterString);
        buffer.Append(laterString);

        // Assert
        Assert.Equal(length, buffer.Length);
        Assert.Collection(
            buffer.Pages,
            page => Assert.Equal(expected1, page),
            page => Assert.Equal(expected2, page.Take(PagedCharBuffer.PageSize + 1)));
    }

    [Fact]
    public void AppendWithString_AppendsToMultiplePages_AsLengthReturnsToNormal()
    {
        // Arrange
        // Imitate ArrayPool<char>.Shared just after an entry in its default char[1024] Bucket is returned.
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource
            .SetupSequence(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(new char[2 * PagedCharBuffer.PageSize])
            .Returns(new char[PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        // Request enough space that transition should occur.
        var length = 2 * PagedCharBuffer.PageSize + 1;
        var expected = Enumerable.Repeat('d', 2 * PagedCharBuffer.PageSize);
        var laterString = new string('d', PagedCharBuffer.PageSize);

        // Act (loop within second Append(string) call).
        buffer.Append('d');
        buffer.Append(laterString);
        buffer.Append(laterString);

        // Assert
        Assert.Equal(length, buffer.Length);
        Assert.Collection(
            buffer.Pages,
            page => Assert.Equal(expected, page),
            page => Assert.Equal('d', page[0]));
    }

    [Fact]
    public void AppendWithString_AppendsToMultiplePages_AfterLengthFallback()
    {
        // Arrange
        // Imitate ArrayPool<char>.Shared after it falls back from its default char[1024] Bucket to the next one.
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource
            .Setup(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(() => new char[2 * PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        // Request enough space that transition should occur.
        var length = 2 * PagedCharBuffer.PageSize + 1;
        var expected = Enumerable.Repeat('d', 2 * PagedCharBuffer.PageSize);
        var laterString = new string('d', PagedCharBuffer.PageSize);

        // Act (loop within second Append(string) call).
        buffer.Append('d');
        buffer.Append(laterString);
        buffer.Append(laterString);

        // Assert
        Assert.Equal(length, buffer.Length);
        Assert.Collection(
            buffer.Pages,
            page => Assert.Equal(expected, page),
            page => Assert.Equal('d', page[0]));
    }

    [Fact]
    public void AppendWithString_AppendsToTheCurrentPageIfItIsNotEmpty()
    {
        // Arrange
        var string1 = "abc";
        var string2 = "def";
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(string1);
        buffer.Append(string2);

        // Assert
        Assert.Equal(6, buffer.Length);
        var page = Assert.Single(buffer.Pages);
        Assert.Equal(new[] { 'a', 'b', 'c', 'd', 'e', 'f' }, page.Take(buffer.Length));
    }

    [Fact]
    public void AppendWithCharArray_AppendsToPage()
    {
        // Arrange
        var charsToAppend = new[] { 'a', 'b', 'c', 'd' };
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(charsToAppend, 1, 3);

        // Assert
        Assert.Equal(3, buffer.Length);
        var page = Assert.Single(buffer.Pages);
        Assert.Equal(new[] { 'b', 'c', 'd' }, page.Take(buffer.Length));
    }

    [Fact]
    public void AppendWithCharArray_AppendsToMultiplePages()
    {
        // Arrange
        var ch = 'e';
        var length = PagedCharBuffer.PageSize * 2 + 3;
        var charsToAppend = Enumerable.Repeat(ch, 2 * length).ToArray();
        var expected = Enumerable.Repeat(ch, PagedCharBuffer.PageSize);
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append(charsToAppend, 0, length);

        // Assert
        Assert.Equal(length, buffer.Length);
        Assert.Collection(buffer.Pages,
            page => Assert.Equal(expected, page),
            page => Assert.Equal(expected, page),
            page =>
            {
                Assert.Equal(ch, page[0]);
                Assert.Equal(ch, page[1]);
                Assert.Equal(ch, page[2]);
            });
    }

    [Fact]
    public void AppendWithCharArray_AppendsToMultiplePages_AsLengthFallsBack()
    {
        // Arrange
        // Imitate ArrayPool<char>.Shared as it falls back from its default char[1024] Bucket to the next one.
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource
            .SetupSequence(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(new char[PagedCharBuffer.PageSize])
            .Returns(new char[2 * PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        // Request enough space that transition should occur.
        var length = 2 * PagedCharBuffer.PageSize + 1;
        var expected1 = Enumerable.Repeat('d', PagedCharBuffer.PageSize);
        var expected2 = Enumerable.Repeat('d', PagedCharBuffer.PageSize + 1);
        var laterChars = new string('d', PagedCharBuffer.PageSize).ToCharArray();

        // Act (loop within first Append(char[]) call).
        buffer.Append('d');
        buffer.Append(laterChars, 0, laterChars.Length);
        buffer.Append(laterChars, 0, laterChars.Length);

        // Assert
        Assert.Equal(length, buffer.Length);
        Assert.Collection(
            buffer.Pages,
            page => Assert.Equal(expected1, page),
            page => Assert.Equal(expected2, page.Take(PagedCharBuffer.PageSize + 1)));
    }

    [Fact]
    public void AppendWithCharArray_AppendsToMultiplePages_AsLengthReturnsToNormal()
    {
        // Arrange
        // Imitate ArrayPool<char>.Shared just after an entry in its default char[1024] Bucket is returned.
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource
            .SetupSequence(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(new char[2 * PagedCharBuffer.PageSize])
            .Returns(new char[PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        // Request enough space that transition should occur.
        var length = 2 * PagedCharBuffer.PageSize + 1;
        var expected = Enumerable.Repeat('d', 2 * PagedCharBuffer.PageSize);
        var laterChars = new string('d', PagedCharBuffer.PageSize).ToCharArray();

        // Act (loop within second Append(char[]) call).
        buffer.Append('d');
        buffer.Append(laterChars, 0, laterChars.Length);
        buffer.Append(laterChars, 0, laterChars.Length);

        // Assert
        Assert.Equal(length, buffer.Length);
        Assert.Collection(
            buffer.Pages,
            page => Assert.Equal(expected, page),
            page => Assert.Equal('d', page[0]));
    }

    [Fact]
    public void AppendWithCharArray_AppendsToMultiplePages_AfterLengthFallback()
    {
        // Arrange
        // Imitate ArrayPool<char>.Shared after it falls back from its default char[1024] Bucket to the next one.
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource
            .Setup(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(() => new char[2 * PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        // Request enough space that transition should occur.
        var length = 2 * PagedCharBuffer.PageSize + 1;
        var expected = Enumerable.Repeat('d', 2 * PagedCharBuffer.PageSize);
        var laterChars = new string('d', PagedCharBuffer.PageSize).ToCharArray();

        // Act (loop within second Append(char[]) call).
        buffer.Append('d');
        buffer.Append(laterChars, 0, laterChars.Length);
        buffer.Append(laterChars, 0, laterChars.Length);

        // Assert
        Assert.Equal(length, buffer.Length);
        Assert.Collection(
            buffer.Pages,
            page => Assert.Equal(expected, page),
            page => Assert.Equal('d', page[0]));
    }

    [Fact]
    public void AppendWithCharArray_AppendsToCurrentPage()
    {
        // Arrange
        var arrayToAppend = new[] { 'c', 'd', 'e' };
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Append("Ab");
        buffer.Append(arrayToAppend, 0, arrayToAppend.Length);

        // Assert
        Assert.Equal(5, buffer.Length);
        var page = Assert.Single(buffer.Pages);
        Assert.Equal(new[] { 'A', 'b', 'c', 'd', 'e' }, page.Take(buffer.Length));
    }

    [Fact]
    public void Clear_WorksIfBufferHasNotBeenWrittenTo()
    {
        // Arrange
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act
        buffer.Clear();

        // Assert
        Assert.Equal(0, buffer.Length);
    }

    [Fact]
    public void Clear_ReturnsPagesToBufferSource()
    {
        // Arrange
        var bufferSource = new Mock<ICharBufferSource>();
        bufferSource.Setup(s => s.Rent(PagedCharBuffer.PageSize))
            .Returns(new char[PagedCharBuffer.PageSize]);
        var buffer = new PagedCharBuffer(bufferSource.Object);

        // Act
        buffer.Append(new string('a', PagedCharBuffer.PageSize * 3 + 4));
        buffer.Clear();

        // Assert
        Assert.Equal(0, buffer.Length);
        bufferSource.Verify(s => s.Return(It.IsAny<char[]>()), Times.Exactly(3));
    }

    [Fact]
    public void UseAfterClear_Works()
    {
        // Arrange
        var buffer = new PagedCharBuffer(new CharArrayBufferSource());

        // Act - 1
        buffer.Append(new string('a', PagedCharBuffer.PageSize));
        buffer.Append(new string('b', 10));
        buffer.Clear();

        // Assert - 1
        Assert.Equal(0, buffer.Length);
        Assert.Single(buffer.Pages);

        // Act - 2
        buffer.Append("efgh");

        // Assert - 2
        Assert.Equal(4, buffer.Length);
        Assert.Single(buffer.Pages);
        Assert.Equal(new[] { 'e', 'f', 'g', 'h' }, buffer.Pages[0].Take(buffer.Length));
    }
}
