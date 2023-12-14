// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test;

public class AzureAppendBlobTests
{
    public string _containerUrl = "https://host/container?query=1";
    public string _blobName = "blob/path";

    [Fact]
    public async Task SendsDataAsStream()
    {
        var testMessageHandler = new TestMessageHandler(async message =>
        {
            Assert.Equal(HttpMethod.Put, message.Method);
            Assert.Equal("https://host/container/blob/path?query=1&comp=appendblock", message.RequestUri.ToString());
            Assert.Equal(new byte[] { 0, 2, 3 }, await message.Content.ReadAsByteArrayAsync());
            AssertDefaultHeaders(message);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var blob = new BlobAppendReferenceWrapper(_containerUrl, _blobName, new HttpClient(testMessageHandler));
        await blob.AppendAsync(new ArraySegment<byte>(new byte[] { 0, 2, 3 }), CancellationToken.None);
    }

    private static void AssertDefaultHeaders(HttpRequestMessage message)
    {
        Assert.Equal(new[] { "AppendBlob" }, message.Headers.GetValues("x-ms-blob-type"));
        Assert.Equal(new[] { "2016-05-31" }, message.Headers.GetValues("x-ms-version"));
        Assert.NotNull(message.Headers.Date);
    }

    [Theory]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.PreconditionFailed)]
    public async Task CreatesBlobIfNotExist(HttpStatusCode createStatusCode)
    {
        var stage = 0;
        var testMessageHandler = new TestMessageHandler(async message =>
        {
            // First PUT request
            if (stage == 0)
            {
                Assert.Equal(HttpMethod.Put, message.Method);
                Assert.Equal("https://host/container/blob/path?query=1&comp=appendblock", message.RequestUri.ToString());
                Assert.Equal(new byte[] { 0, 2, 3 }, await message.Content.ReadAsByteArrayAsync());
                Assert.Equal(3, message.Content.Headers.ContentLength);

                AssertDefaultHeaders(message);

                stage++;
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            // Create request
            if (stage == 1)
            {
                Assert.Equal(HttpMethod.Put, message.Method);
                Assert.Equal("https://host/container/blob/path?query=1", message.RequestUri.ToString());
                Assert.Equal(0, message.Content.Headers.ContentLength);
                Assert.Equal(new[] { "*" }, message.Headers.GetValues("If-None-Match"));

                AssertDefaultHeaders(message);

                stage++;
                return new HttpResponseMessage(createStatusCode);
            }
            // First PUT request
            if (stage == 2)
            {
                Assert.Equal(HttpMethod.Put, message.Method);
                Assert.Equal("https://host/container/blob/path?query=1&comp=appendblock", message.RequestUri.ToString());
                Assert.Equal(new byte[] { 0, 2, 3 }, await message.Content.ReadAsByteArrayAsync());
                Assert.Equal(3, message.Content.Headers.ContentLength);

                AssertDefaultHeaders(message);

                stage++;
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            throw new NotImplementedException();
        });

        var blob = new BlobAppendReferenceWrapper(_containerUrl, _blobName, new HttpClient(testMessageHandler));
        await blob.AppendAsync(new ArraySegment<byte>(new byte[] { 0, 2, 3 }), CancellationToken.None);

        Assert.Equal(3, stage);
    }

    [Fact]
    public async Task ThrowsForUnknownStatus()
    {
        var stage = 0;
        var testMessageHandler = new TestMessageHandler(async message =>
        {
            // First PUT request
            if (stage == 0)
            {
                Assert.Equal(HttpMethod.Put, message.Method);
                Assert.Equal("https://host/container/blob/path?query=1&comp=appendblock", message.RequestUri.ToString());
                Assert.Equal(new byte[] { 0, 2, 3 }, await message.Content.ReadAsByteArrayAsync());
                Assert.Equal(3, message.Content.Headers.ContentLength);

                AssertDefaultHeaders(message);

                stage++;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            throw new NotImplementedException();
        });

        var blob = new BlobAppendReferenceWrapper(_containerUrl, _blobName, new HttpClient(testMessageHandler));
        await Assert.ThrowsAsync<HttpRequestException>(() => blob.AppendAsync(new ArraySegment<byte>(new byte[] { 0, 2, 3 }), CancellationToken.None));

        Assert.Equal(1, stage);
    }

    [Fact]
    public async Task ThrowsForUnknownStatusDuringCreation()
    {
        var stage = 0;
        var testMessageHandler = new TestMessageHandler(async message =>
        {
            // First PUT request
            if (stage == 0)
            {
                Assert.Equal(HttpMethod.Put, message.Method);
                Assert.Equal("https://host/container/blob/path?query=1&comp=appendblock", message.RequestUri.ToString());
                Assert.Equal(new byte[] { 0, 2, 3 }, await message.Content.ReadAsByteArrayAsync());
                Assert.Equal(3, message.Content.Headers.ContentLength);

                AssertDefaultHeaders(message);

                stage++;
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            // Create request
            if (stage == 1)
            {
                Assert.Equal(HttpMethod.Put, message.Method);
                Assert.Equal("https://host/container/blob/path?query=1", message.RequestUri.ToString());
                Assert.Equal(0, message.Content.Headers.ContentLength);
                Assert.Equal(new[] { "*" }, message.Headers.GetValues("If-None-Match"));

                AssertDefaultHeaders(message);

                stage++;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            throw new NotImplementedException();
        });

        var blob = new BlobAppendReferenceWrapper(_containerUrl, _blobName, new HttpClient(testMessageHandler));
        await Assert.ThrowsAsync<HttpRequestException>(() => blob.AppendAsync(new ArraySegment<byte>(new byte[] { 0, 2, 3 }), CancellationToken.None));

        Assert.Equal(2, stage);
    }

    private class TestMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _callback;

        public TestMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback)
        {
            _callback = callback;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await _callback(request);
        }
    }
}
