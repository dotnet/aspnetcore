// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class FileStreamResultExecutorTest
{
    [Fact]
    public async Task ExecuteAsync_DisposesStreamAsync()
    {
        // Arrange
        var executor = CreateExecutor();

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext() { HttpContext = httpContext };

        var stream = new AsyncOnlyStream();
        var result = new FileStreamResult(stream, "text/plain");

        // Act
        await executor.ExecuteAsync(actionContext, result);

        // Assert
        Assert.True(stream.DidDisposeAsync);
    }

    private static FileStreamResultExecutor CreateExecutor()
    {
        return new FileStreamResultExecutor(NullLoggerFactory.Instance);
    }

    private class AsyncOnlyStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Must use ReadAsync");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

        protected override void Dispose(bool disposing) => throw new NotSupportedException("Must use DisposeAsync");

        public bool DidDisposeAsync { get; private set; }

        public override ValueTask DisposeAsync()
        {
            DidDisposeAsync = true;
            return ValueTask.CompletedTask;
        }
    }
}
