// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class SendFileResponseExtensionsTests
{
    [Fact]
    public Task SendFileWhenFileNotFoundThrows()
    {
        var response = new DefaultHttpContext().Response;
        return Assert.ThrowsAsync<FileNotFoundException>(() => response.SendFileAsync("foo"));
    }

    [Fact]
    public async Task SendFileWorks()
    {
        var context = new DefaultHttpContext();
        var response = context.Response;
        var fakeFeature = new FakeResponseBodyFeature();
        context.Features.Set<IHttpResponseBodyFeature>(fakeFeature);

        await response.SendFileAsync("bob", 1, 3, CancellationToken.None);

        Assert.Equal("bob", fakeFeature.Name);
        Assert.Equal(1, fakeFeature.Offset);
        Assert.Equal(3, fakeFeature.Length);
        Assert.Equal(CancellationToken.None, fakeFeature.Token);
    }

    [Fact]
    public async Task SendFile_FallsBackToBodyStream()
    {
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        var response = context.Response;
        response.Body = body;

        await response.SendFileAsync("testfile1kb.txt", 1, 3, CancellationToken.None);

        Assert.Equal(3, body.Length);
    }

    [Fact]
    public async Task SendFile_Stream_ThrowsWhenCanceled()
    {
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        var response = context.Response;
        response.Body = body;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => response.SendFileAsync("testfile1kb.txt", 1, 3, new CancellationToken(canceled: true)));

        Assert.Equal(0, body.Length);
    }

    [Fact]
    public async Task SendFile_Feature_ThrowsWhenCanceled()
    {
        var context = new DefaultHttpContext();
        var fakeFeature = new FakeResponseBodyFeature();
        context.Features.Set<IHttpResponseBodyFeature>(fakeFeature);
        var response = context.Response;

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => response.SendFileAsync("testfile1kb.txt", 1, 3, new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task SendFile_Stream_AbortsSilentlyWhenRequestCanceled()
    {
        var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.RequestAborted = new CancellationToken(canceled: true);
        var response = context.Response;
        response.Body = body;

        await response.SendFileAsync("testfile1kb.txt", 1, 3, CancellationToken.None);

        Assert.Equal(0, body.Length);
    }

    [Fact]
    public async Task SendFile_Feature_AbortsSilentlyWhenRequestCanceled()
    {
        var context = new DefaultHttpContext();
        var fakeFeature = new FakeResponseBodyFeature();
        context.Features.Set<IHttpResponseBodyFeature>(fakeFeature);
        var token = new CancellationToken(canceled: true);
        context.RequestAborted = token;
        var response = context.Response;

        await response.SendFileAsync("testfile1kb.txt", 1, 3, CancellationToken.None);

        Assert.Equal(token, fakeFeature.Token);
    }

    private class FakeResponseBodyFeature : IHttpResponseBodyFeature
    {
        public string Name { get; set; } = null;
        public long Offset { get; set; } = 0;
        public long? Length { get; set; } = null;
        public CancellationToken Token { get; set; }

        public Stream Stream => throw new System.NotImplementedException();

        public PipeWriter Writer => throw new System.NotImplementedException();

        public Task CompleteAsync()
        {
            throw new System.NotImplementedException();
        }

        public void DisableBuffering()
        {
            throw new System.NotImplementedException();
        }

        public Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            Name = path;
            Offset = offset;
            Length = length;
            Token = cancellation;

            cancellation.ThrowIfCancellationRequested();
            return Task.FromResult(0);
        }

        public Task StartAsync(CancellationToken token = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
