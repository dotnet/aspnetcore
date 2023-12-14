// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class RequestTests : LoggedTest
{
    [ConditionalFact]
    public async Task Request_SimpleGet_ExpectedFieldsSet()
    {
        string root;
        using (Utilities.CreateHttpServerReturnRoot("/basepath", out root, httpContext =>
        {
            try
            {
                var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();

                // Request Keys
                Assert.Equal("GET", requestInfo.Method);
                Assert.Equal(Stream.Null, requestInfo.Body);
                Assert.NotNull(requestInfo.Headers);
                Assert.Equal("http", requestInfo.Scheme);
                Assert.Equal("/basepath", requestInfo.PathBase);
                Assert.Equal("/SomePath", requestInfo.Path);
                Assert.Equal("?SomeQuery", requestInfo.QueryString);
                Assert.Equal("/basepath/SomePath?SomeQuery", requestInfo.RawTarget);
                Assert.Equal("HTTP/1.1", requestInfo.Protocol);

                Assert.False(httpContext.Request.CanHaveBody());
                var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                Assert.Equal("::1", connectionInfo.RemoteIpAddress.ToString());
                Assert.NotEqual(0, connectionInfo.RemotePort);
                Assert.Equal("::1", connectionInfo.LocalIpAddress.ToString());
                Assert.NotEqual(0, connectionInfo.LocalPort);
                Assert.NotNull(connectionInfo.ConnectionId);

                // Trace identifier
                var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
                Assert.NotNull(requestIdentifierFeature);
                Assert.NotNull(requestIdentifierFeature.TraceIdentifier);

                // Note: Response keys are validated in the ResponseTests
            }
            catch (Exception ex)
            {
                byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                httpContext.Response.Body.Write(body, 0, body.Length);
            }
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(root + "/basepath/SomePath?SomeQuery");
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task Request_FieldsCanBeSet_Set()
    {
        string root;
        using (Utilities.CreateHttpServerReturnRoot("/basepath", out root, httpContext =>
        {
            try
            {
                var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();

                // Request Keys
                requestInfo.Method = "TEST";
                Assert.Equal("TEST", requestInfo.Method);
                requestInfo.Body = new MemoryStream();
                Assert.IsType<MemoryStream>(requestInfo.Body);
                var customHeaders = new HeaderCollection();
                requestInfo.Headers = customHeaders;
                Assert.Same(customHeaders, requestInfo.Headers);
                requestInfo.Scheme = "abcd";
                Assert.Equal("abcd", requestInfo.Scheme);
                requestInfo.PathBase = "/customized/Base";
                Assert.Equal("/customized/Base", requestInfo.PathBase);
                requestInfo.Path = "/customized/Path";
                Assert.Equal("/customized/Path", requestInfo.Path);
                requestInfo.QueryString = "?customizedQuery";
                Assert.Equal("?customizedQuery", requestInfo.QueryString);
                requestInfo.RawTarget = "/customized/raw?Target";
                Assert.Equal("/customized/raw?Target", requestInfo.RawTarget);
                requestInfo.Protocol = "Custom/2.0";
                Assert.Equal("Custom/2.0", requestInfo.Protocol);

                var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                connectionInfo.RemoteIpAddress = IPAddress.Broadcast;
                Assert.Equal(IPAddress.Broadcast, connectionInfo.RemoteIpAddress);
                connectionInfo.RemotePort = 12345;
                Assert.Equal(12345, connectionInfo.RemotePort);
                connectionInfo.LocalIpAddress = IPAddress.Any;
                Assert.Equal(IPAddress.Any, connectionInfo.LocalIpAddress);
                connectionInfo.LocalPort = 54321;
                Assert.Equal(54321, connectionInfo.LocalPort);
                connectionInfo.ConnectionId = "CustomId";
                Assert.Equal("CustomId", connectionInfo.ConnectionId);

                // Trace identifier
                var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
                Assert.NotNull(requestIdentifierFeature);
                requestIdentifierFeature.TraceIdentifier = "customTrace";
                Assert.Equal("customTrace", requestIdentifierFeature.TraceIdentifier);

                // Note: Response keys are validated in the ResponseTests
            }
            catch (Exception ex)
            {
                byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                httpContext.Response.Body.Write(body, 0, body.Length);
            }
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(root + "/basepath/SomePath?SomeQuery");
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task Request_FieldsCanBeSetToNull_Set()
    {
        string root;
        using (Utilities.CreateHttpServerReturnRoot("/basepath", out root, httpContext =>
        {
            try
            {
                var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();

                // Request Keys
                requestInfo.Method = null;
                Assert.Null(requestInfo.Method);
                requestInfo.Body = null;
                Assert.Null(requestInfo.Body);
                requestInfo.Headers = null;
                Assert.Null(requestInfo.Headers);
                requestInfo.Scheme = null;
                Assert.Null(requestInfo.Scheme);
                requestInfo.PathBase = null;
                Assert.Null(requestInfo.PathBase);
                requestInfo.Path = null;
                Assert.Null(requestInfo.Path);
                requestInfo.QueryString = null;
                Assert.Null(requestInfo.QueryString);
                requestInfo.RawTarget = null;
                Assert.Null(requestInfo.RawTarget);
                requestInfo.Protocol = null;
                Assert.Null(requestInfo.Protocol);

                var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                connectionInfo.RemoteIpAddress = null;
                Assert.Null(connectionInfo.RemoteIpAddress);
                connectionInfo.RemotePort = -1;
                Assert.Equal(-1, connectionInfo.RemotePort);
                connectionInfo.LocalIpAddress = null;
                Assert.Null(connectionInfo.LocalIpAddress);
                connectionInfo.LocalPort = -1;
                Assert.Equal(-1, connectionInfo.LocalPort);
                connectionInfo.ConnectionId = null;
                Assert.Null(connectionInfo.ConnectionId);

                // Trace identifier
                var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
                Assert.NotNull(requestIdentifierFeature);
                requestIdentifierFeature.TraceIdentifier = null;
                Assert.Null(requestIdentifierFeature.TraceIdentifier);

                // Note: Response keys are validated in the ResponseTests
            }
            catch (Exception ex)
            {
                byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                httpContext.Response.Body.Write(body, 0, body.Length);
            }
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(root + "/basepath/SomePath?SomeQuery");
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalTheory]
    [InlineData("/", "/", "", "/")]
    [InlineData("/basepath/", "/basepath", "/basepath", "")]
    [InlineData("/basepath/", "/basepath/", "/basepath", "/")]
    [InlineData("/basepath/", "/basepath/subpath", "/basepath", "/subpath")]
    [InlineData("/base path/", "/base%20path/sub%20path", "/base path", "/sub path")]
    [InlineData("/base葉path/", "/base%E8%91%89path/sub%E8%91%89path", "/base葉path", "/sub葉path")]
    [InlineData("/basepath/", "/basepath/sub%2Fpath", "/basepath", "/sub%2Fpath")]
    [InlineData("/base", "///base/path1/path2", "///base", "/path1/path2")]
    public async Task Request_PathSplitting(string pathBase, string requestPath, string expectedPathBase, string expectedPath)
    {
        string root;
        using (Utilities.CreateHttpServerReturnRoot(pathBase, out root, httpContext =>
        {
            try
            {
                var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
                var connectionInfo = httpContext.Features.Get<IHttpConnectionFeature>();
                var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();

                // Request Keys
                Assert.Equal("http", requestInfo.Scheme);
                Assert.Equal(expectedPath, requestInfo.Path);
                Assert.Equal(expectedPathBase, requestInfo.PathBase);
                Assert.Equal(string.Empty, requestInfo.QueryString);
                Assert.Equal(requestPath, requestInfo.RawTarget);

                // Trace identifier
                Assert.NotNull(requestIdentifierFeature);
                Assert.NotNull(requestIdentifierFeature.TraceIdentifier);
            }
            catch (Exception ex)
            {
                byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                httpContext.Response.Body.Write(body, 0, body.Length);
            }
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(root + requestPath);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task Request_DoubleEscapingAllowed()
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
        {
            var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
            Assert.Equal("/%2F", requestInfo.Path);
            Assert.Equal("/%252F", requestInfo.RawTarget);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendSocketRequestAsync(root, "/%252F");
            var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
            Assert.Equal("200", responseStatusCode);
        }
    }

    [ConditionalFact]
    public async Task Request_FullUriInRequestLine_ParsesPath()
    {
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out var root, httpContext =>
        {
            var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
            Assert.Equal("/", requestInfo.Path);
            Assert.Equal("", requestInfo.PathBase);
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            // Send a HTTP request with the request line:
            // GET http://localhost:5001 HTTP/1.1
            var response = await SendSocketRequestAsync(root, root);
            var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
            Assert.Equal(StatusCodes.Status200OK.ToString(CultureInfo.InvariantCulture), responseStatusCode);
        }
    }

    [ConditionalFact]
    public async Task Request_FullUriInRequestLineWithSlashesInQuery_BlockedByHttpSys()
    {
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out var root, httpContext =>
        {
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            // Send a HTTP request with the request line:
            // GET http://localhost:5001?query=value/1/2 HTTP/1.1
            // Should return a 400 as it is a client error
            var response = await SendSocketRequestAsync(root, root + "?query=value/1/2");
            var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
            Assert.Equal(StatusCodes.Status400BadRequest.ToString(CultureInfo.InvariantCulture), responseStatusCode);
        }
    }

    [ConditionalTheory]
    // The test server defines these prefixes: "/", "/11", "/2/3", "/2", "/11/2"
    [InlineData("/", "", "/")]
    [InlineData("/random", "", "/random")]
    [InlineData("/11", "/11", "")]
    [InlineData("/11/", "/11", "/")]
    [InlineData("/11/random", "/11", "/random")]
    [InlineData("/2", "/2", "")]
    [InlineData("/2/", "/2", "/")]
    [InlineData("/2/random", "/2", "/random")]
    [InlineData("/2/3", "/2/3", "")]
    [InlineData("/2/3/", "/2/3", "/")]
    [InlineData("/2/3/random", "/2/3", "/random")]
    public async Task Request_MultiplePrefixes(string requestPath, string expectedPathBase, string expectedPath)
    {
        string root;
        using (CreateServer(out root, httpContext =>
        {
            var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
            var requestIdentifierFeature = httpContext.Features.Get<IHttpRequestIdentifierFeature>();
            try
            {
                Assert.Equal(expectedPath, requestInfo.Path);
                Assert.Equal(expectedPathBase, requestInfo.PathBase);
                Assert.Equal(requestPath, requestInfo.RawTarget);

                // Trace identifier
                Assert.NotNull(requestIdentifierFeature);
                Assert.NotNull(requestIdentifierFeature.TraceIdentifier);
            }
            catch (Exception ex)
            {
                byte[] body = Encoding.ASCII.GetBytes(ex.ToString());
                httpContext.Response.Body.Write(body, 0, body.Length);
            }
            return Task.FromResult(0);
        }))
        {
            string response = await SendRequestAsync(root + requestPath);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task Request_UrlUnescaping()
    {
        // Must start with '/'
        var stringBuilder = new StringBuilder("/");
        for (var i = 32; i < 127; i++)
        {
            stringBuilder.Append("%");
            stringBuilder.Append(i.ToString("X2", CultureInfo.InvariantCulture));
        }
        var rawPath = stringBuilder.ToString();
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
        {
            var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
            Assert.Equal(rawPath, requestInfo.RawTarget);
            // '/' %2F is an exception, un-escaping it would change the structure of the path
            Assert.Equal("/ !\"#$%&'()*+,-.%2F0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~", requestInfo.Path);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendSocketRequestAsync(root, rawPath);
            var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
            Assert.Equal("200", responseStatusCode);
        }
    }

    [ConditionalFact]
    public async Task Request_WithDoubleSlashes_LeftAlone()
    {
        var rawPath = "//a/b//c";
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
        {
            var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
            Assert.Equal(rawPath, requestInfo.RawTarget);
            Assert.Equal(rawPath, requestInfo.Path);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendSocketRequestAsync(root, rawPath);
            var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
            Assert.Equal("200", responseStatusCode);
        }
    }

    [ConditionalTheory]
    [InlineData("/", "/a/b/../c", "", "/a/c")]
    [InlineData("/", "/a/b/./c", "", "/a/b/c")]
    [InlineData("/a", "/a/./c", "/a", "/c")]
    [InlineData("/a", "/a/d/../b/c", "/a", "/b/c")]
    [InlineData("/a/b", "/a/d/../b/c", "/a/b", "/c")] // Http.Sys uses the cooked URL when routing.
    [InlineData("/a/b", "/a/./b/c", "/a/b", "/c")] // Http.Sys uses the cooked URL when routing.
    public async Task Request_WithNavigation_Removed(string basePath, string input, string expectedPathBase, string expectedPath)
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot(basePath, out root, httpContext =>
        {
            var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
            Assert.Equal(input, requestInfo.RawTarget);
            Assert.Equal(expectedPathBase, requestInfo.PathBase);
            Assert.Equal(expectedPath, requestInfo.Path);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendSocketRequestAsync(root, input);
            var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
            Assert.Equal("200", responseStatusCode);
        }
    }

    [ConditionalTheory]
    [InlineData("/a/b/%2E%2E/c", "/a/c")]
    [InlineData("/a/b/%2E/c", "/a/b/c")]
    public async Task Request_WithEscapedNavigation_Removed(string input, string expected)
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
        {
            var requestInfo = httpContext.Features.Get<IHttpRequestFeature>();
            Assert.Equal(input, requestInfo.RawTarget);
            Assert.Equal(expected, requestInfo.Path);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendSocketRequestAsync(root, input);
            var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
            Assert.Equal("200", responseStatusCode);
        }
    }

    [ConditionalFact]
    public async Task Request_ControlCharacters_400()
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
        {
            throw new NotImplementedException();
        }, LoggerFactory))
        {
            for (var i = 0; i < 32; i++)
            {
                if (i == 9 || i == 10)
                {
                    continue; // \t and \r are allowed by Http.Sys.
                }
                var response = await SendSocketRequestAsync(root, "/" + (char)i);
                var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
                Assert.True(string.Equals("400", responseStatusCode), i.ToString("X2", CultureInfo.InvariantCulture));
            }
        }
    }

    [ConditionalFact]
    public async Task Request_EscapedControlCharacters_400()
    {
        string root;
        using (var server = Utilities.CreateHttpServerReturnRoot("/", out root, httpContext =>
        {
            throw new NotImplementedException();
        }, LoggerFactory))
        {
            for (var i = 0; i < 32; i++)
            {
                var response = await SendSocketRequestAsync(root, "/%" + i.ToString("X2", CultureInfo.InvariantCulture));
                var responseStatusCode = response.Substring(9); // Skip "HTTP/1.1 "
                Assert.True(string.Equals("400", responseStatusCode), i.ToString("X2", CultureInfo.InvariantCulture));
            }
        }
    }

    [ConditionalFact]
    public async Task RequestAborted_AfterAccessingProperty_Notified()
    {
        var registered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = Utilities.CreateHttpServerReturnRoot("/", out var address, async httpContext =>
        {
            var ct = httpContext.RequestAborted;

            if (!ct.CanBeCanceled || ct.IsCancellationRequested)
            {
                result.SetException(new Exception("The CT isn't valid."));
                return;
            }

            ct.Register(() => result.SetResult());

            registered.SetResult();

            // Don't exit until it fires or else it could be disposed.
            await result.Task.DefaultTimeout();
        }, LoggerFactory);

        // Send a request and then abort.

        var uri = new Uri(address);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("POST / HTTP/1.1");
        builder.AppendLine("Connection: close");
        builder.AppendLine("Content-Length: 10");
        builder.Append("HOST: ");
        builder.AppendLine(uri.Authority);
        builder.AppendLine();

        byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        await socket.ConnectAsync(uri.Host, uri.Port);
        socket.Send(request);

        // Wait for the token to be setup before aborting.
        await registered.Task.DefaultTimeout();

        socket.Close();

        await result.Task.DefaultTimeout();
    }

    [ConditionalFact]
    public async Task RequestAbortedDurringRead_BeforeAccessingProperty_TokenAlreadyCanceled()
    {
        var requestAborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = Utilities.CreateHttpServerReturnRoot("/", out var address, async httpContext =>
        {
            await requestAborted.Task.DefaultTimeout();
            try
            {
                await httpContext.Request.Body.ReadAsync(new byte[10]).DefaultTimeout();
                result.SetException(new Exception("This should have aborted"));
                return;
            }
            catch (IOException)
            {
            }

            result.SetResult(httpContext.RequestAborted.IsCancellationRequested);
        }, LoggerFactory);

        // Send a request and then abort.

        var uri = new Uri(address);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("POST / HTTP/1.1");
        builder.AppendLine("Connection: close");
        builder.AppendLine("Content-Length: 10");
        builder.Append("HOST: ");
        builder.AppendLine(uri.Authority);
        builder.AppendLine();

        byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

        await socket.ConnectAsync(uri.Host, uri.Port);
        socket.Send(request);
        socket.Close();

        requestAborted.SetResult();

        var wasCancelled = await result.Task;
        Assert.True(wasCancelled);
    }

    private IServer CreateServer(out string root, RequestDelegate app)
    {
        // TODO: We're just doing this to get a dynamic port. This can be removed later when we add support for hot-adding prefixes.
        var dynamicServer = Utilities.CreateHttpServerReturnRoot("/", out root, app, LoggerFactory);
        dynamicServer.Dispose();
        var rootUri = new Uri(root);
        var server = Utilities.CreatePump(LoggerFactory);

        foreach (string path in new[] { "/", "/11", "/2/3", "/2", "/11/2" })
        {
            server.Listener.Options.UrlPrefixes.Add(UrlPrefix.Create(rootUri.Scheme, rootUri.Host, rootUri.Port, path));
        }

        server.StartAsync(new DummyApplication(app), CancellationToken.None).Wait();
        return server;
    }

    private async Task<string> SendRequestAsync(string uri)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetStringAsync(uri);
        }
    }

    private async Task<string> SendSocketRequestAsync(string address, string path)
    {
        var uri = new Uri(address);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("GET " + path + " HTTP/1.1");
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
