// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Moq;

namespace Microsoft.AspNetCore.Http.Features;

public class RequestBodyPipeFeatureTests
{
    [Fact]
    public void RequestBodyReturnsStreamPipeReader()
    {
        var context = new DefaultHttpContext();
        var expectedStream = new MemoryStream();
        context.Request.Body = expectedStream;

        var feature = new RequestBodyPipeFeature(context);

        var pipeBody = feature.Reader;

        Assert.NotNull(pipeBody);
    }

    [Fact]
    public async Task RequestBodyGetsDataFromSecondStream()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes("hahaha"));
        var feature = new RequestBodyPipeFeature(context);
        var _ = feature.Reader;

        var expectedString = "abcdef";
        context.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes(expectedString));
        var data = await feature.Reader.ReadAsync();
        Assert.Equal(expectedString, GetStringFromReadResult(data));
    }

    [Fact]
    public async Task RequestBodyDoesZeroByteRead()
    {
        var context = new DefaultHttpContext();
        var mockStream = new Mock<Stream>();

        var bufferLengths = new List<int>();

        mockStream.Setup(s => s.CanRead).Returns(true);
        mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>())).Returns<Memory<byte>, CancellationToken>((buffer, token) =>
        {
            bufferLengths.Add(buffer.Length);
            return ValueTask.FromResult(0);
        });

        context.Request.Body = mockStream.Object;
        var feature = new RequestBodyPipeFeature(context);
        var data = await feature.Reader.ReadAsync();

        Assert.Equal(2, bufferLengths.Count);
        Assert.Equal(0, bufferLengths[0]);
        Assert.Equal(4096, bufferLengths[1]);
    }

    private static string GetStringFromReadResult(ReadResult data)
    {
        return Encoding.ASCII.GetString(data.Buffer.ToArray());
    }
}
