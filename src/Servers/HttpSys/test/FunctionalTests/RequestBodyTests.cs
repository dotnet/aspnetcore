// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class RequestBodyTests : LoggedTest
{
    [ConditionalFact]
    public async Task RequestBody_ReadSync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            Assert.True(httpContext.Request.CanHaveBody());
            byte[] input = new byte[100];
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            int read = httpContext.Request.Body.Read(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            httpContext.Response.Body.Write(input, 0, read);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_Read0ByteSync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            Assert.True(httpContext.Request.CanHaveBody());
            byte[] input = new byte[100];
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            int read = httpContext.Request.Body.Read(input, 0, 0);
            Assert.Equal(0, read);
            read = httpContext.Request.Body.Read(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            httpContext.Response.Body.Write(input, 0, read);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadAsync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            Assert.True(httpContext.Request.CanHaveBody());
            byte[] input = new byte[100];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            await httpContext.Response.Body.WriteAsync(input, 0, read);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_Read0ByteAsync_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            Assert.True(httpContext.Request.CanHaveBody());
            byte[] input = new byte[100];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, 0);
            Assert.Equal(0, read);
            read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            await httpContext.Response.Body.WriteAsync(input, 0, read);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadBeginEnd_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            byte[] input = new byte[100];
            int read = httpContext.Request.Body.EndRead(httpContext.Request.Body.BeginRead(input, 0, input.Length, null, null));
            httpContext.Response.ContentLength = read;
            httpContext.Response.Body.EndWrite(httpContext.Response.Body.BeginWrite(input, 0, read, null, null));
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_InvalidBuffer_ArgumentException()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            byte[] input = new byte[100];
            Assert.Throws<ArgumentNullException>("buffer", () => httpContext.Request.Body.Read(null, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => httpContext.Request.Body.Read(input, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => httpContext.Request.Body.Read(input, input.Length + 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => httpContext.Request.Body.Read(input, 10, -1));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => httpContext.Request.Body.Read(input, 1, input.Length));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => httpContext.Request.Body.Read(input, 0, input.Length + 1));
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadSyncPartialBody_Success()
    {
        StaggardContent content = new StaggardContent();
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            byte[] input = new byte[10];
            httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
            int read = httpContext.Request.Body.Read(input, 0, input.Length);
            Assert.Equal(5, read);
            content.Block.Release();
            read = httpContext.Request.Body.Read(input, 0, input.Length);
            Assert.Equal(5, read);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, content);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ReadAsyncPartialBody_Success()
    {
        StaggardContent content = new StaggardContent();
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            byte[] input = new byte[10];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            Assert.Equal(5, read);
            content.Block.Release();
            read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            Assert.Equal(5, read);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, content);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_PostWithImidateBody_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            byte[] input = new byte[11];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            Assert.Equal(10, read);
            read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            Assert.Equal(0, read);
            httpContext.Response.ContentLength = 10;
            await httpContext.Response.Body.WriteAsync(input, 0, 10);
        }, LoggerFactory))
        {
            string response = await SendSocketRequestAsync(address);
            string[] lines = response.Split('\r', '\n');
            Assert.Equal(13, lines.Length);
            Assert.Equal("HTTP/1.1 200 OK", lines[0]);
            Assert.Equal("0123456789", lines[12]);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_ChangeContentLength_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
        {
            var newContentLength = httpContext.Request.ContentLength + 1000;
            httpContext.Request.ContentLength = newContentLength;
            Assert.Equal(newContentLength, httpContext.Request.ContentLength);

            var contentLengthHeadersCount = 0;
            foreach (var header in httpContext.Request.Headers)
            {
                if (string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    contentLengthHeadersCount++;
                }
            }
            Assert.Equal(1, contentLengthHeadersCount);

            byte[] input = new byte[100];
            int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
            httpContext.Response.ContentLength = read;
            await httpContext.Response.Body.WriteAsync(input, 0, read);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task RequestBody_RemoveHeaderOnEmptyValueSet_Success()
    {
        var requestWasProcessed = false;

        static void CheckHeadersCount(string headerName, int expectedCount, HttpRequest request)
        {
            var headersCount = 0;
            foreach (var header in request.Headers)
            {
                if (string.Equals(header.Key, headerName, StringComparison.OrdinalIgnoreCase))
                {
                    headersCount++;
                }
            }

            Assert.Equal(expectedCount, headersCount);
        }

        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            // play with standard header
            httpContext.Request.Headers[HeaderNames.ContentLength] = "123";
            CheckHeadersCount(HeaderNames.ContentLength, 1, httpContext.Request);
            Assert.Equal(123, httpContext.Request.ContentLength);
            httpContext.Request.Headers[HeaderNames.ContentLength] = "456";
            CheckHeadersCount(HeaderNames.ContentLength, 1, httpContext.Request);
            Assert.Equal(456, httpContext.Request.ContentLength);
            httpContext.Request.Headers[HeaderNames.ContentLength] = StringValues.Empty;
            CheckHeadersCount(HeaderNames.ContentLength, 0, httpContext.Request);
            Assert.Null(httpContext.Request.ContentLength);
            Assert.Equal("", httpContext.Request.Headers[HeaderNames.ContentLength].ToString());
            httpContext.Request.ContentLength = 789;
            CheckHeadersCount(HeaderNames.ContentLength, 1, httpContext.Request);
            Assert.Equal(789, httpContext.Request.ContentLength);

            // play with custom header
            httpContext.Request.Headers["Custom-Header"] = "foo";
            CheckHeadersCount("Custom-Header", 1, httpContext.Request);
            httpContext.Request.Headers["Custom-Header"] = "bar";
            CheckHeadersCount("Custom-Header", 1, httpContext.Request);
            httpContext.Request.Headers["Custom-Header"] = StringValues.Empty;
            CheckHeadersCount("Custom-Header", 0, httpContext.Request);
            Assert.Equal("", httpContext.Request.Headers["Custom-Header"].ToString());

            httpContext.Response.StatusCode = 200;
            requestWasProcessed = true;
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            await SendRequestAsync(address, "Hello World");
            Assert.True(requestWasProcessed);
        }
    }

    private Task<string> SendRequestAsync(string uri, string upload)
    {
        return SendRequestAsync(uri, new StringContent(upload));
    }

    private async Task<string> SendRequestAsync(string uri, HttpContent content)
    {
        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    private async Task<string> SendSocketRequestAsync(string address)
    {
        // Connect with a socket
        Uri uri = new Uri(address);
        TcpClient client = new TcpClient();

        try
        {
            await client.ConnectAsync(uri.Host, uri.Port);
            NetworkStream stream = client.GetStream();

            // Send an HTTP GET request
            byte[] requestBytes = BuildPostRequest(uri);
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
            StreamReader reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception)
        {
            ((IDisposable)client).Dispose();
            throw;
        }
    }

    private byte[] BuildPostRequest(Uri uri)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("POST");
        builder.Append(" ");
        builder.Append(uri.PathAndQuery);
        builder.Append(" HTTP/1.1");
        builder.AppendLine();

        builder.Append("Host: ");
        builder.Append(uri.Host);
        builder.Append(':');
        builder.Append(uri.Port);
        builder.AppendLine();

        builder.AppendLine("Connection: close");
        builder.AppendLine("Content-Length: 10");
        builder.AppendLine();
        builder.Append("0123456789");
        return Encoding.ASCII.GetBytes(builder.ToString());
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
            await stream.WriteAsync(new byte[5], 0, 5);
            await stream.FlushAsync();
            Assert.True(await Block.WaitAsync(TimeSpan.FromSeconds(10)));
            await stream.WriteAsync(new byte[5], 0, 5);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 10;
            return true;
        }
    }
}
