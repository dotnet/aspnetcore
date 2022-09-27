// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class NonDisposableStreamTest
{
    [Fact]
    public void InnerStreamIsOpenOnClose()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var nonDisposableStream = new NonDisposableStream(innerStream);

        // Act
        nonDisposableStream.Close();

        // Assert
        Assert.True(innerStream.CanRead);
    }

    [Fact]
    public void InnerStreamIsNotFlushedOnClose()
    {
        // Arrange
        var stream = FlushReportingStream.GetThrowingStream();

        var nonDisposableStream = new NonDisposableStream(stream);

        // Act & Assert
        nonDisposableStream.Close();
    }

    [Fact]
    public void InnerStreamIsOpenOnDispose()
    {
        // Arrange
        var innerStream = new MemoryStream();
        var nonDisposableStream = new NonDisposableStream(innerStream);

        // Act
        nonDisposableStream.Dispose();

        // Assert
        Assert.True(innerStream.CanRead);
    }

    [Fact]
    public void InnerStreamIsNotFlushedOnDispose()
    {
        var stream = FlushReportingStream.GetThrowingStream();
        var nonDisposableStream = new NonDisposableStream(stream);

        // Act & Assert
        nonDisposableStream.Dispose();
    }

    [Fact]
    public void InnerStreamIsNotFlushedOnFlush()
    {
        // Arrange
        var stream = FlushReportingStream.GetThrowingStream();

        var nonDisposableStream = new NonDisposableStream(stream);

        // Act & Assert
        nonDisposableStream.Flush();
    }

    [Fact]
    public async Task InnerStreamIsNotFlushedOnFlushAsync()
    {
        // Arrange
        var stream = FlushReportingStream.GetThrowingStream();

        var nonDisposableStream = new NonDisposableStream(stream);

        // Act & Assert
        await nonDisposableStream.FlushAsync();
    }
}
