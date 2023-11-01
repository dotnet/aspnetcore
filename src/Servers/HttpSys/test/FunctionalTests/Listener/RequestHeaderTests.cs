// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener;

public class RequestHeaderTests
{
    [ConditionalFact]
    public async Task RequestHeaders_RemoveHeaders_Success()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            string[] customValues = new string[] { "custom1, and custom测试2", "custom3" };
            Task responseTask = SendRequestAsync(address, "Custom-Header", customValues, Encoding.UTF8);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout);
            var requestHeaders = context.Request.Headers;

            var headers = context.Request.Headers;
            bool removed = headers.Remove("Connection");
            Assert.False(headers.TryGetValue("Connection", out _));
            Assert.True(StringValues.IsNullOrEmpty(headers["Connection"]));
            Assert.True(StringValues.IsNullOrEmpty(headers.Connection));

            removed = headers.Remove("Custom-Header");
            Assert.True(removed);
            Assert.False(headers.TryGetValue("Custom-Header", out _));
            Assert.True(StringValues.IsNullOrEmpty(headers["Custom-Header"]));

            headers["Connection"] = "foo";
            Assert.True(headers.TryGetValue("Connection", out var connectionValue));
            Assert.Equal("foo", connectionValue);
            Assert.Equal("foo", headers["Connection"]);

            bool removedAfterAssignValue = headers.Remove("Connection");
            bool removedAgain = headers.Remove("Connection");

            Assert.True(removed);
            Assert.True(removedAfterAssignValue);
            Assert.False(removedAgain);

            context.Dispose();

            await responseTask;
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendsUtf8Headers_Success()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            string[] customValues = new string[] { "custom1, and custom测试2", "custom3" };
            Task responseTask = SendRequestAsync(address, "Custom-Header", customValues, Encoding.UTF8);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout);
            var requestHeaders = context.Request.Headers;
            Assert.Equal(4, requestHeaders.Count);
            Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
            Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
            Assert.Equal("close", requestHeaders["Connection"]);
            Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
            // Apparently Http.Sys squashes request headers together.
            Assert.Equal("custom1, and custom测试2, custom3", requestHeaders["Custom-Header"]);
            Assert.Equal(new[] { "custom1", "and custom测试2", "custom3" }, requestHeaders.GetValues("Custom-Header"));
            Assert.Equal("spacervalue, spacervalue", requestHeaders["Spacer-Header"]);
            Assert.Equal(new[] { "spacervalue", "spacervalue" }, requestHeaders.GetValues("Spacer-Header"));
            context.Dispose();

            await responseTask;
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_Latin1Replaced()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            string[] customValues = new string[] { "£" };
            Task responseTask = SendRequestAsync(address, "Custom-Header", customValues, Encoding.Latin1);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout);
            var requestHeaders = context.Request.Headers;
            Assert.Equal(4, requestHeaders.Count);
            Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
            Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
            Assert.Equal("close", requestHeaders["Connection"]);
            Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
            // Apparently Http.Sys squashes request headers together.
            Assert.Equal("�", requestHeaders["Custom-Header"]);
            Assert.Equal(new[] { "�" }, requestHeaders.GetValues("Custom-Header"));
            Assert.Equal("spacervalue", requestHeaders["Spacer-Header"]);
            Assert.Equal(new[] { "spacervalue" }, requestHeaders.GetValues("Spacer-Header"));
            context.Dispose();

            await responseTask;
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendsLatin1Headers_Success()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            server.Options.UseLatin1RequestHeaders = true;
            string[] customValues = new string[] { "£" };
            Task responseTask = SendRequestAsync(address, "Custom-Header", customValues, Encoding.Latin1);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout);
            var requestHeaders = context.Request.Headers;
            Assert.Equal(4, requestHeaders.Count);
            Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
            Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
            Assert.Equal("close", requestHeaders["Connection"]);
            Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
            // Apparently Http.Sys squashes request headers together.
            Assert.Equal("£", requestHeaders["Custom-Header"]);
            Assert.Equal(new[] { "£" }, requestHeaders.GetValues("Custom-Header"));
            Assert.Equal("spacervalue", requestHeaders["Spacer-Header"]);
            Assert.Equal(new[] { "spacervalue" }, requestHeaders.GetValues("Spacer-Header"));
            context.Dispose();

            await responseTask;
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendsBadLatin1Headers_Rejected()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            server.Options.UseLatin1RequestHeaders = true;
            string[] customValues = new string[] { "£\0a" };
            var responseTask = SendRequestAsync(address, "Custom-Header", customValues, Encoding.Latin1);
            var response = await responseTask;
            Assert.StartsWith("400", response.Substring(9));
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendsKnownHeaderWithNoValue_Success()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            string[] customValues = new string[] { "" };
            Task responseTask = SendRequestAsync(address, "If-None-Match", customValues, Encoding.UTF8);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout);
            var requestHeaders = context.Request.Headers;
            Assert.Equal(3, requestHeaders.Count);
            Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
            Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
            Assert.Equal("close", requestHeaders["Connection"]);
            Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
            Assert.Equal(StringValues.Empty, requestHeaders["If-None-Match"]);
            Assert.Empty(requestHeaders.GetValues("If-None-Match"));
            Assert.Equal("spacervalue", requestHeaders["Spacer-Header"]);
            context.Dispose();

            await responseTask;
        }
    }

    [ConditionalFact]
    public async Task RequestHeaders_ClientSendsUnknownHeaderWithNoValue_Success()
    {
        string address;
        using (var server = Utilities.CreateHttpServer(out address))
        {
            string[] customValues = new string[] { "" };
            Task responseTask = SendRequestAsync(address, "Custom-Header", customValues, Encoding.UTF8);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout);
            var requestHeaders = context.Request.Headers;
            Assert.Equal(4, requestHeaders.Count);
            Assert.Equal(new Uri(address).Authority, requestHeaders["Host"]);
            Assert.Equal(new[] { new Uri(address).Authority }, requestHeaders.GetValues("Host"));
            Assert.Equal("close", requestHeaders["Connection"]);
            Assert.Equal(new[] { "close" }, requestHeaders.GetValues("Connection"));
            Assert.Equal("", requestHeaders["Custom-Header"]);
            Assert.Empty(requestHeaders.GetValues("Custom-Header"));
            Assert.Equal("spacervalue", requestHeaders["Spacer-Header"]);
            context.Dispose();

            await responseTask;
        }
    }

    private async Task<string> SendRequestAsync(string address, string customHeader, string[] customValues, Encoding encoding)
    {
        var uri = new Uri(address);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("GET / HTTP/1.1");
        builder.AppendLine("Connection: close");
        builder.Append("HOST: ");
        builder.AppendLine(uri.Authority);
        foreach (string value in customValues)
        {
            builder.Append(customHeader);
            builder.Append(": ");
            builder.AppendLine(value);
            builder.AppendLine("Spacer-Header: spacervalue");
        }
        builder.AppendLine();

        byte[] request = encoding.GetBytes(builder.ToString());

        Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(uri.Host, uri.Port);

        socket.Send(request);

        byte[] response = new byte[1024 * 5];
        await Task.Run(() => socket.Receive(response));
        socket.Dispose();
        return encoding.GetString(response);
    }
}
