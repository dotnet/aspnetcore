// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class RequestBodyLimitTests : LoggedTest
{
    [ConditionalFact]
    public async Task ContentLengthEqualsLimit_ReadSync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Equal(11, httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = httpContext.Request.Body.Read(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            httpContext.Response.Body.Write(input, 0, read);
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 11, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task ContentLengthEqualsLimit_ReadAsync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.True(httpContext.Request.CanHaveBody());
            Assert.Equal(11, httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            await httpContext.Response.Body.WriteAsync(input, 0, read);
        }, options => options.MaxRequestBodySize = 11, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task ContentLengthEqualsLimit_ReadBeginEnd_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Equal(11, httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = httpContext.Request.Body.EndRead(httpContext.Request.Body.BeginRead(input, 0, input.Length, null, null));
            httpContext.Response.ContentLength = read;
            httpContext.Response.Body.EndWrite(httpContext.Response.Body.BeginWrite(input, 0, read, null, null));
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 11, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task ChunkedEqualsLimit_ReadSync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = httpContext.Request.Body.Read(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            httpContext.Response.Body.Write(input, 0, read);
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 11, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World", chunked: true);
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task ChunkedEqualsLimit_ReadAsync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.True(httpContext.Request.CanHaveBody());
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            await httpContext.Response.Body.WriteAsync(input, 0, read);
        }, options => options.MaxRequestBodySize = 11, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World", chunked: true);
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task ChunkedEqualsLimit_ReadBeginEnd_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = httpContext.Request.Body.EndRead(httpContext.Request.Body.BeginRead(input, 0, input.Length, null, null));
            httpContext.Response.ContentLength = read;
            httpContext.Response.Body.EndWrite(httpContext.Response.Body.BeginWrite(input, 0, read, null, null));
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 11, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World", chunked: true);
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task ContentLengthExceedsLimit_ReadSync_ThrowsImmediately()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Equal(11, httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            var ex = Assert.Throws<BadHttpRequestException>(() => httpContext.Request.Body.Read(input, 0, input.Length));
            Assert.Equal("The request's Content-Length 11 is larger than the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            ex = Assert.Throws<BadHttpRequestException>(() => httpContext.Request.Body.Read(input, 0, input.Length));
            Assert.Equal("The request's Content-Length 11 is larger than the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 10, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World");
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task ContentLengthExceedsLimit_ReadAsync_ThrowsImmediately()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Equal(11, httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            var ex = Assert.Throws<BadHttpRequestException>(() => { var t = httpContext.Request.Body.ReadAsync(input, 0, input.Length); });
            Assert.Equal("The request's Content-Length 11 is larger than the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            ex = Assert.Throws<BadHttpRequestException>(() => { var t = httpContext.Request.Body.ReadAsync(input, 0, input.Length); });
            Assert.Equal("The request's Content-Length 11 is larger than the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 10, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World");
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task ContentLengthExceedsLimit_ReadBeginEnd_ThrowsImmediately()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Equal(11, httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            var ex = Assert.Throws<BadHttpRequestException>(() => httpContext.Request.Body.BeginRead(input, 0, input.Length, null, null));
            Assert.Equal("The request's Content-Length 11 is larger than the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            ex = Assert.Throws<BadHttpRequestException>(() => httpContext.Request.Body.BeginRead(input, 0, input.Length, null, null));
            Assert.Equal("The request's Content-Length 11 is larger than the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 10, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World");
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task ChunkedExceedsLimit_ReadSync_ThrowsAtLimit()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            var ex = Assert.Throws<BadHttpRequestException>(() => httpContext.Request.Body.Read(input, 0, input.Length));
            Assert.Equal("The total number of bytes read 11 has exceeded the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            ex = Assert.Throws<BadHttpRequestException>(() => httpContext.Request.Body.Read(input, 0, input.Length));
            Assert.Equal("The total number of bytes read 11 has exceeded the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 10, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World", chunked: true);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task ChunkedExceedsLimit_ReadAsync_ThrowsAtLimit()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => httpContext.Request.Body.ReadAsync(input, 0, input.Length));
            Assert.Equal("The total number of bytes read 11 has exceeded the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => httpContext.Request.Body.ReadAsync(input, 0, input.Length));
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            Assert.Equal("The total number of bytes read 11 has exceeded the request body size limit 10.", ex.Message);
        }, options => options.MaxRequestBodySize = 10, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World", chunked: true);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task ChunkedExceedsLimit_ReadBeginEnd_ThrowsAtLimit()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            var body = httpContext.Request.Body;
            var ex = Assert.Throws<BadHttpRequestException>(() => body.EndRead(body.BeginRead(input, 0, input.Length, null, null)));
            Assert.Equal("The total number of bytes read 11 has exceeded the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            ex = Assert.Throws<BadHttpRequestException>(() => body.EndRead(body.BeginRead(input, 0, input.Length, null, null)));
            Assert.Equal("The total number of bytes read 11 has exceeded the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 10, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World", chunked: true);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task Chunked_ReadSyncPartialBodyUnderLimit_ThrowsAfterLimit()
    {
        var content = new StaggardContent();
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = httpContext.Request.Body.Read(input, 0, input.Length);
            Assert.Equal(10, read);
            content.Block.Release();
            var ex = Assert.Throws<BadHttpRequestException>(() => httpContext.Request.Body.Read(input, 0, input.Length));
            Assert.Equal("The total number of bytes read 20 has exceeded the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
            return Task.FromResult(0);
        }, options => options.MaxRequestBodySize = 10, LoggerFactory))
        {
            string response = await SendRequestAsync(address, content, chunked: true);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task Chunked_ReadAsyncPartialBodyUnderLimit_ThrowsAfterLimit()
    {
        var content = new StaggardContent();
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            Assert.Equal(10, read);
            content.Block.Release();
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => httpContext.Request.Body.ReadAsync(input, 0, input.Length));
            Assert.Equal("The total number of bytes read 20 has exceeded the request body size limit 10.", ex.Message);
            Assert.Equal(StatusCodes.Status413PayloadTooLarge, ex.StatusCode);
        }, options => options.MaxRequestBodySize = 10, LoggerFactory))
        {
            string response = await SendRequestAsync(address, content, chunked: true);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task AdjustLimitPerRequest_ContentLength_ReadAsync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Equal(11, feature.MaxRequestBodySize);
            feature.MaxRequestBodySize = 12;
            Assert.Equal(12, httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            Assert.True(feature.IsReadOnly);
            httpContext.Response.ContentLength = read;
            await httpContext.Response.Body.WriteAsync(input, 0, read);
        }, options => options.MaxRequestBodySize = 11, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World!");
            Assert.Equal("Hello World!", response);
        }
    }

    [ConditionalFact]
    public async Task AdjustLimitPerRequest_Chunked_ReadAsync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var feature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.NotNull(feature);
            Assert.False(feature.IsReadOnly);
            Assert.Equal(11, feature.MaxRequestBodySize);
            feature.MaxRequestBodySize = 12;
            Assert.Null(httpContext.Request.ContentLength);
            byte[] input = new byte[100];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            Assert.True(feature.IsReadOnly);
            httpContext.Response.ContentLength = read;
            await httpContext.Response.Body.WriteAsync(input, 0, read);
        }, options => options.MaxRequestBodySize = 11, LoggerFactory))
        {
            var response = await SendRequestAsync(address, "Hello World!", chunked: true);
            Assert.Equal("Hello World!", response);
        }
    }

    private Task<string> SendRequestAsync(string uri, string upload, bool chunked = false)
    {
        return SendRequestAsync(uri, new StringContent(upload), chunked);
    }

    private async Task<string> SendRequestAsync(string uri, HttpContent content, bool chunked = false)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
            HttpResponseMessage response = await client.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    private class StaggardContent : HttpContent
    {
        public StaggardContent()
        {
            Block = new SemaphoreSlim(0, 1);
        }

        public SemaphoreSlim Block { get; private set; }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await stream.WriteAsync(new byte[10], 0, 10);
            await stream.FlushAsync();
            Assert.True(await Block.WaitAsync(TimeSpan.FromSeconds(10)));
            await stream.WriteAsync(new byte[10], 0, 10);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 10;
            return true;
        }
    }
}
