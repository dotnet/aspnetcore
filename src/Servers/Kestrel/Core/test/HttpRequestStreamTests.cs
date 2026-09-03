// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpRequestStreamTests
{
    [Fact]
    public void CanReadReturnsTrue()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.True(stream.CanRead);
    }

    [Fact]
    public void CanSeekReturnsFalse()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.False(stream.CanSeek);
    }

    [Fact]
    public void CanWriteReturnsFalse()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.False(stream.CanWrite);
    }

    [Fact]
    public void SeekThrows()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void LengthThrows()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.Throws<NotSupportedException>(() => stream.Length);
    }

    [Fact]
    public void SetLengthThrows()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
    }

    [Fact]
    public void PositionThrows()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.Throws<NotSupportedException>(() => stream.Position);
        Assert.Throws<NotSupportedException>(() => stream.Position = 0);
    }

    [Fact]
    public void WriteThrows()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
    }

    [Fact]
    public void WriteByteThrows()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
    }

    [Fact]
    public async Task WriteAsyncThrows()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(new byte[1], 0, 1));
    }

    [Fact]
    // Read-only streams should support Flush according to https://github.com/dotnet/corefx/pull/27327#pullrequestreview-98384813
    public void FlushDoesNotThrow()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        stream.Flush();
    }

    [Fact]
    public async Task FlushAsyncDoesNotThrow()
    {
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        await stream.FlushAsync();
    }

    [Fact]
    public async Task SynchronousReadsThrowIfDisallowedByIHttpBodyControlFeature()
    {
        var allowSynchronousIO = false;

        var bodyControl = new TestBodyControlFeature { AllowSynchronousIO = allowSynchronousIO };
        var messageBody = new TestMessageBody();

        var pipeReader = new HttpRequestPipeReader();
        var stream = new HttpRequestStream(bodyControl, pipeReader);
        pipeReader.StartAcceptingReads(messageBody);

        Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 1));

        var ioEx = Assert.Throws<InvalidOperationException>(() => stream.Read(new byte[1], 0, 1));
        Assert.Equal("Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.", ioEx.Message);

        var ioEx2 = Assert.Throws<InvalidOperationException>(() => stream.CopyTo(Stream.Null));
        Assert.Equal("Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.", ioEx2.Message);

        allowSynchronousIO = true;
        bodyControl.AllowSynchronousIO = allowSynchronousIO;
        Assert.Equal(0, stream.Read(new byte[1], 0, 1));
    }

    [Fact]
    public async Task AbortCausesReadToCancel()
    {
        var pipeReader = new HttpRequestPipeReader();

        var stream = new HttpRequestStream(new TestBodyControlFeature(), pipeReader);
        pipeReader.StartAcceptingReads(null);
        pipeReader.Abort();
        await Assert.ThrowsAsync<TaskCanceledException>(() => stream.ReadAsync(new byte[1], 0, 1));
    }

    [Fact]
    public async Task AbortWithErrorCausesReadToCancel()
    {
        var pipeReader = new HttpRequestPipeReader();

        var stream = new HttpRequestStream(new TestBodyControlFeature(), pipeReader);
        pipeReader.StartAcceptingReads(null);
        var error = new Exception();
        pipeReader.Abort(error);
        var exception = await Assert.ThrowsAsync<Exception>(() => stream.ReadAsync(new byte[1], 0, 1));
        Assert.Same(error, exception);
    }

    [Fact]
    public async Task StopAcceptingReadsCausesReadToThrowObjectDisposedException()
    {
        var pipeReader = new HttpRequestPipeReader();
        var stream = new HttpRequestStream(new TestBodyControlFeature(), pipeReader);
        pipeReader.StartAcceptingReads(null);
        pipeReader.StopAcceptingReads();

        // Validation for ReadAsync occurs in an async method in ReadOnlyPipeStream.
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => { await stream.ReadAsync(new byte[1], 0, 1); });
    }

    [Fact]
    public async Task AbortCausesCopyToAsyncToCancel()
    {
        var pipeReader = new HttpRequestPipeReader();
        var stream = new HttpRequestStream(new TestBodyControlFeature(), pipeReader);
        pipeReader.StartAcceptingReads(null);
        pipeReader.Abort();
        await Assert.ThrowsAsync<TaskCanceledException>(() => stream.CopyToAsync(Stream.Null));
    }

    [Fact]
    public async Task AbortWithErrorCausesCopyToAsyncToCancel()
    {
        var pipeReader = new HttpRequestPipeReader();
        var stream = new HttpRequestStream(new TestBodyControlFeature(), pipeReader);
        pipeReader.StartAcceptingReads(null);
        var error = new Exception();
        pipeReader.Abort(error);
        var exception = await Assert.ThrowsAsync<Exception>(() => stream.CopyToAsync(Stream.Null));
        Assert.Same(error, exception);
    }

    [Fact]
    public async Task StopAcceptingReadsCausesCopyToAsyncToThrowObjectDisposedException()
    {
        var pipeReader = new HttpRequestPipeReader();
        var stream = new HttpRequestStream(new TestBodyControlFeature(), pipeReader);
        pipeReader.StartAcceptingReads(null);
        pipeReader.StopAcceptingReads();
        // Validation for CopyToAsync occurs in an async method in ReadOnlyPipeStream.
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => { await stream.CopyToAsync(Stream.Null); });
    }

    [Fact]
    public async Task NullDestinationCausesCopyToAsyncToThrowArgumentNullException()
    {
        var pipeReader = new HttpRequestPipeReader();
        var stream = new HttpRequestStream(new TestBodyControlFeature(), pipeReader);
        pipeReader.StartAcceptingReads(null);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => { await stream.CopyToAsync(null); });
    }

    [Fact]
    public async Task ZeroBufferSizeCausesCopyToAsyncToThrowArgumentException()
    {
        var pipeReader = new HttpRequestPipeReader();
        var stream = new HttpRequestStream(new TestBodyControlFeature(), new HttpRequestPipeReader());
        pipeReader.StartAcceptingReads(null);
        // This is technically a breaking change, to throw an ArgumentoutOfRangeException rather than an ArgumentException
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => { await stream.CopyToAsync(Stream.Null, 0); });
    }
}
