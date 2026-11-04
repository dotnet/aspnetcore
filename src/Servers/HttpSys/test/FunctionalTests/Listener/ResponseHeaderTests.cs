// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener;

public class ResponseHeaderTests : IDisposable
{
    private HttpClient _client = new HttpClient();

    void IDisposable.Dispose()
    {
        _client.Dispose();
    }

    [ConditionalFact]
    public async Task ResponseHeaders_11Request_ServerSendsDefaultHeaders()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
            Assert.Equal(2, response.Headers.Count());
            Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.Date.HasValue);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
            Assert.Single(response.Content.Headers);
            Assert.Equal(0, response.Content.Headers.ContentLength);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_10Request_ServerSendsDefaultHeaders()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendRequestAsync(address, usehttp11: false);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
            Assert.Equal(3, response.Headers.Count());
            Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.ConnectionClose.Value);
            Assert.True(response.Headers.Date.HasValue);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
            Assert.Single(response.Content.Headers);
            Assert.Equal(0, response.Content.Headers.ContentLength);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_11HeadRequest_ServerSendsDefaultHeaders()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
            Assert.Equal(2, response.Headers.Count());
            Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.Date.HasValue);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
            Assert.False(response.Content.Headers.Contains("Content-Length"));
            Assert.Empty(response.Content.Headers);

            // Send a second request to check that the connection wasn't corrupted.
            responseTask = SendHeadRequestAsync(address);
            context = await server.AcceptAsync(Utilities.DefaultTimeout);
            context.Dispose();
            response = await responseTask;
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_10HeadRequest_ServerSendsDefaultHeaders()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address, usehttp11: false);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
            Assert.Equal(3, response.Headers.Count());
            Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.ConnectionClose.Value);
            Assert.True(response.Headers.Date.HasValue);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
            Assert.False(response.Content.Headers.Contains("Content-Length"));
            Assert.Empty(response.Content.Headers);

            // Send a second request to check that the connection wasn't corrupted.
            responseTask = SendHeadRequestAsync(address);
            context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Dispose();
            response = await responseTask;
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_11HeadRequestWithContentLength_Success()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Response.ContentLength = 20;
            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
            Assert.Equal(2, response.Headers.Count());
            Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.Date.HasValue);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
            Assert.Single(response.Content.Headers);
            Assert.Equal(20, response.Content.Headers.ContentLength);

            // Send a second request to check that the connection wasn't corrupted.
            responseTask = SendHeadRequestAsync(address);
            context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Dispose();
            response = await responseTask;
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_11RequestStatusCodeWithoutBody_NoContentLengthOrChunkedOrClose()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Response.StatusCode = 204; // No Content
            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
            Assert.Equal(2, response.Headers.Count());
            Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.Date.HasValue);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
            Assert.False(response.Content.Headers.Contains("Content-Length"));
            Assert.Empty(response.Content.Headers);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_11HeadRequestStatusCodeWithoutBody_NoContentLengthOrChunkedOrClose()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendHeadRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Response.StatusCode = 204; // No Content
            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
            Assert.Equal(2, response.Headers.Count());
            Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.Date.HasValue);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
            Assert.False(response.Content.Headers.Contains("Content-Length"));
            Assert.Empty(response.Content.Headers);

            // Send a second request to check that the connection wasn't corrupted.
            responseTask = SendHeadRequestAsync(address);
            context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Dispose();
            response = await responseTask;
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_HTTP10KeepAliveRequest_KeepAliveHeader_Gets11NoClose()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            // Track the number of times ConnectCallback is invoked to ensure the underlying socket wasn't closed.
            int connectCallbackInvocations = 0;
            var handler = new SocketsHttpHandler();
            handler.ConnectCallback = (context, cancellationToken) =>
            {
                Interlocked.Increment(ref connectCallbackInvocations);
                return ConnectCallback(context, cancellationToken);
            };

            using (var client = new HttpClient(handler))
            {
                // Send the first request
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, usehttp11: false, sendKeepAlive: true, httpClient: client);
                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Null(response.Headers.ConnectionClose);

                // Send the second request
                responseTask = SendRequestAsync(address, usehttp11: false, sendKeepAlive: true, httpClient: client);
                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Dispose();

                response = await responseTask;
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Null(response.Headers.ConnectionClose);
            }

            // Verify that ConnectCallback was only called once
            Assert.Equal(1, connectCallbackInvocations);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_HTTP10KeepAliveRequest_ChunkedTransferEncoding_Gets11Close()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendRequestAsync(address, usehttp11: false, sendKeepAlive: true);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            context.Response.Headers["Transfer-Encoding"] = new string[] { "chunked" };
            var responseBytes = Encoding.ASCII.GetBytes("10\r\nManually Chunked\r\n0\r\n\r\n");
            await context.Response.Body.WriteAsync(responseBytes, 0, responseBytes.Length);
            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
            Assert.Equal(new Version(1, 1), response.Version);
            Assert.True(response.Headers.TransferEncodingChunked.HasValue, "Chunked");
            Assert.True(response.Headers.ConnectionClose.Value);
        }
    }

    [ConditionalTheory]
    [InlineData("Server", "\r\nData")]
    [InlineData("Server", "\0Data")]
    [InlineData("Server", "Data\r")]
    [InlineData("Server", "Da\0ta")]
    [InlineData("Server", "Da\u001Fta")]
    [InlineData("Unknown-Header", "\r\nData")]
    [InlineData("Unknown-Header", "\0Data")]
    [InlineData("Unknown-Header", "Data\0")]
    [InlineData("Unknown-Header", "Da\nta")]
    [InlineData("\r\nServer", "Data")]
    [InlineData("Server\r", "Data")]
    [InlineData("Ser\0ver", "Data")]
    [InlineData("Server\r\n", "Data")]
    [InlineData("\u001FServer", "Data")]
    [InlineData("Unknown-Header\r\n", "Data")]
    [InlineData("\0Unknown-Header", "Data")]
    [InlineData("Unknown\r-Header", "Data")]
    [InlineData("Unk\nown-Header", "Data")]
    public async Task AddingControlCharactersToHeadersThrows(string key, string value)
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);

            var responseHeaders = context.Response.Headers;

            Assert.Throws<InvalidOperationException>(() =>
            {
                responseHeaders[key] = value;
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                responseHeaders[key] = new StringValues(new[] { "valid", value });
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                ((IDictionary<string, StringValues>)responseHeaders)[key] = value;
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                var kvp = new KeyValuePair<string, StringValues>(key, value);
                ((ICollection<KeyValuePair<string, StringValues>>)responseHeaders).Add(kvp);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                var kvp = new KeyValuePair<string, StringValues>(key, value);
                ((IDictionary<string, StringValues>)responseHeaders).Add(key, value);
            });

            context.Dispose();

            HttpResponseMessage response = await responseTask;
            response.EnsureSuccessStatusCode();
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool usehttp11 = true, bool sendKeepAlive = false, HttpClient httpClient = null)
    {
        httpClient ??= _client;
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!usehttp11)
        {
            request.Version = new Version(1, 0);
        }
        if (sendKeepAlive)
        {
            request.Headers.Add("Connection", "Keep-Alive");
        }
        return await httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendHeadRequestAsync(string uri, bool usehttp11 = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Head, uri);
        if (!usehttp11)
        {
            request.Version = new Version(1, 0);
        }
        return await _client.SendAsync(request);
    }

    private static async ValueTask<Stream> ConnectCallback(SocketsHttpConnectionContext connectContext, CancellationToken ct)
    {
        var s = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await s.ConnectAsync(connectContext.DnsEndPoint, ct);
            return new NetworkStream(s, ownsSocket: true);
        }
        catch
        {
            s.Dispose();
            throw;
        }
    }
}
