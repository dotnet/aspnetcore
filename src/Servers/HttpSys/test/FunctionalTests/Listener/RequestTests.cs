// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener;

public class RequestTests
{

    [ConditionalTheory]
    [InlineData("/path%")]
    [InlineData("/path%XY")]
    [InlineData("/path%F")]
    [InlineData("/path with spaces")]
    public async Task Request_MalformedPathReturns400StatusCode(string requestPath)
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root))
        {
            var responseTask = SendSocketRequestAsync(root, requestPath);
            var contextTask = server.AcceptAsync(Utilities.DefaultTimeout);
            var response = await responseTask;
            var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
            Assert.Equal("400", responseStatusCode);
        }
    }

    [ConditionalFact]
    public async Task Request_OptionsStar_EmptyPath()
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root))
        {
            var responseTask = SendSocketRequestAsync(root, "*", "OPTIONS");
            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            Assert.Equal("", context.Request.PathBase);
            Assert.Equal("", context.Request.Path);
            Assert.Equal("*", context.Request.RawUrl);
            context.Dispose();
        }
    }

    [ConditionalTheory]
    [InlineData("%D0%A4", "Ф")]
    [InlineData("%d0%a4", "Ф")]
    [InlineData("%E0%A4%AD", "भ")]
    [InlineData("%e0%A4%Ad", "भ")]
    [InlineData("%F0%A4%AD%A2", "𤭢")]
    [InlineData("%F0%a4%Ad%a2", "𤭢")]
    [InlineData("%48%65%6C%6C%6F%20%57%6F%72%6C%64", "Hello World")]
    [InlineData("%48%65%6C%6C%6F%2D%C2%B5%40%C3%9F%C3%B6%C3%A4%C3%BC%C3%A0%C3%A1", "Hello-µ@ßöäüàá")]
    // Test the borderline cases of overlong UTF8.
    [InlineData("%C2%80", "\u0080")]
    [InlineData("%E0%A0%80", "\u0800")]
    [InlineData("%F0%90%80%80", "\U00010000")]
    [InlineData("%63", "c")]
    [InlineData("%32", "2")]
    [InlineData("%20", " ")]
    // Internationalized
    [InlineData("%C3%84ra%20Benetton", "Ära Benetton")]
    [InlineData("%E6%88%91%E8%87%AA%E6%A8%AA%E5%88%80%E5%90%91%E5%A4%A9%E7%AC%91%E5%8E%BB%E7%95%99%E8%82%9D%E8%83%86%E4%B8%A4%E6%98%86%E4%BB%91", "我自横刀向天笑去留肝胆两昆仑")]
    // Skip forward slash
    [InlineData("%2F", "%2F")]
    [InlineData("foo%2Fbar", "foo%2Fbar")]
    [InlineData("foo%2F%20bar", "foo%2F bar")]
    public async Task Request_PathDecodingValidUTF8(string requestPath, string expect)
    {
        string root;
        string actualPath;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root))
        {
            var responseTask = SendSocketRequestAsync(root, "/" + requestPath);
            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            actualPath = context.Request.Path;
            context.Dispose();

            var response = await responseTask;
            Assert.Equal("200", response.Substring(9));
        }

        Assert.Equal(expect, actualPath.TrimStart('/'));
    }

    [ConditionalTheory]
    [InlineData("/%%32")]
    [InlineData("/%%20")]
    [InlineData("/%F0%8F%8F%BF")]
    [InlineData("/%")]
    [InlineData("/%%")]
    [InlineData("/%A")]
    [InlineData("/%Y")]
    public async Task Request_PathDecodingInvalidUTF8(string requestPath)
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root))
        {
            var responseTask = SendSocketRequestAsync(root, requestPath);
            var contextTask = server.AcceptAsync(Utilities.DefaultTimeout);

            var response = await responseTask;
            Assert.Equal("400", response.Substring(9));
        }
    }

    [ConditionalTheory]
    // Overlong ASCII
    [InlineData("/%C0%A4", "/%C0%A4")]
    [InlineData("/%C1%BF", "/%C1%BF")]
    [InlineData("/%E0%80%AF", "/%E0%80%AF")]
    [InlineData("/%E0%9F%BF", "/%E0%9F%BF")]
    [InlineData("/%F0%80%80%AF", "/%F0%80%80%AF")]
    [InlineData("/%F0%80%BF%BF", "/%F0%80%BF%BF")]
    // Mixed
    [InlineData("/%C0%A4%32", "/%C0%A42")]
    [InlineData("/%32%C0%A4%32", "/2%C0%A42")]
    [InlineData("/%C0%32%A4", "/%C02%A4")]
    public async Task Request_OverlongUTF8Path(string requestPath, string expectedPath)
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root))
        {
            var responseTask = SendSocketRequestAsync(root, requestPath);
            var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
            Assert.Equal(expectedPath, context.Request.Path);
            context.Dispose();

            var response = await responseTask;
            Assert.Equal("200", response.Substring(9));
        }
    }

    [ConditionalTheory]
    [InlineData("/", "/", "", "/")]
    [InlineData("/base", "/base", "/base", "")]
    [InlineData("/base", "/baSe", "/baSe", "")]
    [InlineData("/base", "/base/path", "/base", "/path")]
    [InlineData("/base", "///base/path1/path2", "///base", "/path1/path2")]
    [InlineData("/base/ball", @"/baSe\ball//path1//path2", @"/baSe\ball", "//path1//path2")]
    [InlineData("/base/ball", @"/base%2fball//path1//path2", @"/base%2fball", "//path1//path2")]
    [InlineData("/base/ball", @"/base%2Fball//path1//path2", @"/base%2Fball", "//path1//path2")]
    [InlineData("/base/ball", @"/base%5cball//path1//path2", @"/base\ball", "//path1//path2")]
    [InlineData("/base/ball", @"/base%5Cball//path1//path2", @"/base\ball", "//path1//path2")]
    [InlineData("/base/ball", "///baSe//ball//path1//path2", "///baSe//ball", "//path1//path2")]
    [InlineData("/base/ball", @"/base/\ball//path1//path2", @"/base/\ball", "//path1//path2")]
    [InlineData("/base/ball", @"/base/%2fball//path1//path2", @"/base/%2fball", "//path1//path2")]
    [InlineData("/base/ball", @"/base/%2Fball//path1//path2", @"/base/%2Fball", "//path1//path2")]
    [InlineData("/base/ball", @"/base/%5cball//path1//path2", @"/base/\ball", "//path1//path2")]
    [InlineData("/base/ball", @"/base/%5Cball//path1//path2", @"/base/\ball", "//path1//path2")]
    [InlineData("/base/ball", @"/base/call/../ball//path1//path2", @"/base/ball", "//path1//path2")]
    // The results should be "/base/ball", "//path1//path2", but Http.Sys collapses the "//" before the "../"
    // and we don't have a good way of emulating that.
    [InlineData("/base/ball", @"/base/call//../ball//path1//path2", @"", "/base/call/ball//path1//path2")]
    [InlineData("/base/ball", @"/base/call/.%2e/ball//path1//path2", @"/base/ball", "//path1//path2")]
    [InlineData("/base/ball", @"/base/call/.%2E/ball//path1//path2", @"/base/ball", "//path1//path2")]
    public async Task Request_WithPathBase(string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        using var server = Utilities.CreateHttpServerReturnRoot(pathBase, out var root);
        var responseTask = SendSocketRequestAsync(root, requestPath);
        var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
        Assert.Equal(expectedPathBase, context.Request.PathBase);
        Assert.Equal(expectedPath, context.Request.Path);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal("200", response.Substring(9));
    }

    private async Task<string> SendSocketRequestAsync(string address, string path, string method = "GET")
    {
        var uri = new Uri(address);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(FormattableString.Invariant($"{method} {path} HTTP/1.1"));
        builder.AppendLine("Connection: close");
        builder.Append("HOST: ");
        builder.AppendLine(uri.Authority);
        builder.AppendLine();

        byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

        using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Connect(uri.Host, uri.Port);
            socket.Send(request);
            var response = new byte[12];
            await Task.Run(() => socket.Receive(response));
            return Encoding.ASCII.GetString(response);
        }
    }
}
