// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RequestDecompression.Tests;

public class SizeLimitedStreamTests
{
    [Fact]
    public void Ctor_NullInnerStream_Throws()
    {
        // Arrange
        Stream innerStream = null;

        // Act + Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            using var sizeLimitedStream = new SizeLimitedStream(innerStream, 1);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadAsync_InnerStreamExceedsSizeLimit_Throws(bool exceedsLimit)
    {
        // Arrange
        var sizeLimit = 10;
        var bytes = new byte[sizeLimit + (exceedsLimit ? 1 : 0)];

        using var innerStream = new MemoryStream(bytes);
        using var sizeLimitedStream = new SizeLimitedStream(innerStream, sizeLimit);

        var buffer = new byte[bytes.Length];

        // Act
        var exception = await Record.ExceptionAsync(async () =>
        {
            while (await sizeLimitedStream.ReadAsync(buffer) > 0) { }
        });

        // Assert
        AssertStreamReadingException(exception, exceedsLimit);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Read_InnerStreamExceedsSizeLimit_Throws(bool exceedsLimit)
    {
        // Arrange
        var sizeLimit = 10;
        var bytes = new byte[sizeLimit + (exceedsLimit ? 1 : 0)];

        using var innerStream = new MemoryStream(bytes);
        using var sizeLimitedStream = new SizeLimitedStream(innerStream, sizeLimit);

        var buffer = new byte[bytes.Length];

        // Act
        var exception = Record.Exception(() =>
        {
            while (sizeLimitedStream.Read(buffer, 0, buffer.Length) > 0) { }
        });

        // Assert
        AssertStreamReadingException(exception, exceedsLimit);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BeginRead_InnerStreamExceedsSizeLimit_Throws(bool exceedsLimit)
    {
        // Arrange
        var sizeLimit = 10;
        var bytes = new byte[sizeLimit + (exceedsLimit ? 1 : 0)];

        using var innerStream = new MemoryStream(bytes);
        using var sizeLimitedStream = new SizeLimitedStream(innerStream, sizeLimit);

        var buffer = new byte[bytes.Length];

        // Act
        var exception = Record.Exception(() =>
        {
            var asyncResult = sizeLimitedStream.BeginRead(buffer, 0, buffer.Length, (o) => { }, null);
            sizeLimitedStream.EndRead(asyncResult);
        });

        // Assert
        AssertStreamReadingException(exception, exceedsLimit);
    }

    private static void AssertStreamReadingException(Exception exception, bool exceedsLimit)
    {
        if (exceedsLimit)
        {
            Assert.NotNull(exception);
            Assert.IsAssignableFrom<InvalidOperationException>(exception);
            Assert.Equal("The maximum number of bytes have been read.", exception.Message);
        }
        else
        {
            Assert.Null(exception);
        }
    }
}
