// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class RequestHeaderTests
{
    [ConditionalFact]
    public async Task RequestHeaders_ClientSendsDefaultHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                var requestHeaders = httpContext.Request.Headers;
                // NOTE: The System.Net client only sends the Connection: keep-alive header on the first connection per service-point.
                // Assert.Equal(2, requestHeaders.Count);
                // Assert.Equal("Keep-Alive", requestHeaders.Get("Connection"));
                Assert.False(StringValues.IsNullOrEmpty(requestHeaders["Host"]));
                Assert.True(StringValues.IsNullOrEmpty(requestHeaders["Accept"]));
                return Task.FromResult(0);
            }))
        {
            string response = await SendRequestAsync(address);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendsCustomHeaders_Success()
    {
        using var server = Utilities.CreateHttpServer(out var address, httpContext =>
        {
            var requestHeaders = httpContext.Request.Headers;
            Assert.Equal(4, requestHeaders.Count);
            Assert.False(StringValues.IsNullOrEmpty(requestHeaders["Host"]));
            Assert.Equal("close", requestHeaders["Connection"]);
            // Apparently Http.Sys squashes request headers together.
            Assert.Single(requestHeaders["Custom-Header"]);
            Assert.Equal("custom1, and custom2, custom3", requestHeaders["Custom-Header"]);
            Assert.Single(requestHeaders["Spacer-Header"]);
            Assert.Equal("spacervalue, spacervalue", requestHeaders["Spacer-Header"]);
            return Task.FromResult(0);
        });

        var customValues = new string[] { "custom1, and custom2", "custom3" };

        var uri = new Uri(address);
        var builder = new StringBuilder();
        builder.AppendLine("GET / HTTP/1.1");
        builder.AppendLine("Connection: close");
        builder.Append("HOST: ");
        builder.AppendLine(uri.Authority);
        foreach (string value in customValues)
        {
            builder.Append("Custom-Header: ");
            builder.AppendLine(value);
            builder.AppendLine("Spacer-Header: spacervalue");
        }
        builder.AppendLine();

        var request = Encoding.ASCII.GetBytes(builder.ToString());

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(uri.Host, uri.Port);
        socket.Send(request);
        var response = new byte[1024 * 5];
        var read = await Task.Run(() => socket.Receive(response));
        var result = Encoding.ASCII.GetString(response, 0, read);
        var responseStatusCode = result.Substring(9, 3); // Skip "HTTP/1.1 "
        Assert.Equal("200", responseStatusCode);
    }

    [ConditionalFact]
    public async Task RequestHeaders_ServerAddsCustomHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var requestHeaders = httpContext.Request.Headers;
            var header = KeyValuePair.Create("Custom-Header", new StringValues("custom"));
            requestHeaders.Add(header);

            Assert.True(requestHeaders.Contains(header));
            return Task.FromResult(0);
        }))
        {
            string response = await SendRequestAsync(address);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendTransferEncodingHeaders()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var requestHeaders = httpContext.Request.Headers;
            var request = httpContext.Features.Get<RequestContext>().Request;
            Assert.Single(requestHeaders["Transfer-Encoding"]);
            Assert.Equal("chunked", requestHeaders.TransferEncoding);
            Assert.True(request.HasEntityBody);
            return Task.FromResult(0);
        }))
        {
            var headerDictionary = new HeaderDictionary(new Dictionary<string, StringValues> {
                { "Transfer-Encoding", "chunked" }
            });
            var response = await SendRequestAsync(address, headerDictionary);
            var responseStatusCode = response.Substring(9, 3); // Skip "HTTP/1.1 "
            Assert.Equal("200", responseStatusCode);
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendTransferEncodingHeadersWithMultipleValues()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var requestHeaders = httpContext.Request.Headers;
            var request = httpContext.Features.Get<RequestContext>().Request;
            Assert.Single(requestHeaders["Transfer-Encoding"]);
            Assert.Equal("gzip, chunked", requestHeaders.TransferEncoding);
            Assert.True(request.HasEntityBody);
            return Task.FromResult(0);
        }))
        {
            var headerDictionary = new HeaderDictionary(new Dictionary<string, StringValues> {
                { "Transfer-Encoding", new string[] { "gzip", "chunked" } }
            });
            var response = await SendRequestAsync(address, headerDictionary);
            var responseStatusCode = response.Substring(9, 3); // Skip "HTTP/1.1 "
            Assert.Equal("200", responseStatusCode);
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendTransferEncodingAndContentLength_ContentLengthShouldBeRemoved()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var requestHeaders = httpContext.Request.Headers;
            var request = httpContext.Features.Get<RequestContext>().Request;
            Assert.Single(requestHeaders["Transfer-Encoding"]);
            Assert.Equal("gzip, chunked", requestHeaders.TransferEncoding);

            Assert.Null(request.ContentLength);
            Assert.True(request.HasEntityBody);

            Assert.False(requestHeaders.ContainsKey("Content-Length"));
            Assert.Null(requestHeaders.ContentLength);

            Assert.Single(requestHeaders["X-Content-Length"]);
            Assert.Equal("1", requestHeaders["X-Content-Length"]);
            return Task.FromResult(0);
        }))
        {
            var headerDictionary = new HeaderDictionary(new Dictionary<string, StringValues> {
                { "Transfer-Encoding", new string[] { "gzip", "chunked" } },
                { "Content-Length", "1" },
            });
            var response = await SendRequestAsync(address, headerDictionary);
            var responseStatusCode = response.Substring(9, 3); // Skip "HTTP/1.1 "
            Assert.Equal("200", responseStatusCode);
        }
    }

    private async Task<string> SendRequestAsync(string uri)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetStringAsync(uri);
        }
    }

    private async Task<string> SendRequestAsync(string address, IHeaderDictionary headers)
    {
        var uri = new Uri(address);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("POST / HTTP/1.1");
        builder.AppendLine("Connection: close");
        builder.Append("HOST: ");
        builder.AppendLine(uri.Authority);
        foreach (var header in headers)
        {
            foreach (var value in header.Value)
            {
                builder.Append(header.Key);
                builder.Append(": ");
                builder.AppendLine(value);
            }
        }
        builder.AppendLine();

        byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

        using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Connect(uri.Host, uri.Port);
            socket.Send(request);
            byte[] response = new byte[1024 * 5];
            var read = await Task.Run(() => socket.Receive(response));
            return Encoding.ASCII.GetString(response, 0, read);
        }
    }
}
