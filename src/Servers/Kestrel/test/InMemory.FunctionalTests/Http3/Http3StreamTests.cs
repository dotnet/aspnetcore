// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http3StreamTests : Http3TestBase
{
    [Fact]
    public async Task HelloWorldTest()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoApplication, headers);
        await requestStream.SendDataAsync(Encoding.ASCII.GetBytes("Hello world"), endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);

        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello world", Encoding.ASCII.GetString(responseData.ToArray()));
    }

    [Fact]
    public async Task UnauthorizedHttpStatusResponse()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal("401", responseHeaders[InternalHeaderNames.Status]);

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task EmptyMethod_Reset()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, ""),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoApplication, headers);
        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.FormatHttp3ErrorMethodInvalid(""));
    }

    [Fact]
    public async Task InvalidCustomMethod_Reset()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Hello,World"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoApplication, headers);
        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.FormatHttp3ErrorMethodInvalid("Hello,World"));
    }

    [Fact]
    public async Task CustomMethod_Accepted()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoMethod, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(4, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("Custom", responseHeaders["Method"]);
        Assert.Equal("0", responseHeaders["content-length"]);
    }

    [Fact]
    public async Task RequestHeadersMaxRequestHeaderFieldSize_EndsStream()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>("test", new string('a', 1024 * 32 + 1))
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoApplication, headers);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.InternalError,
            AssertExpectedErrorMessages,
            $"The HTTP headers length exceeded the set limit of {1024 * 32} bytes.");
    }

    [Fact]
    public async Task ConnectMethod_Accepted()
    {
        // :path and :scheme are not allowed, :authority is optional
        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT") };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoMethod, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("CONNECT", responseHeaders["Method"]);
    }

    [Fact]
    public async Task OptionsStar_LeftOutOfPath()
    {
        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "OPTIONS"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "*")};

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoPath, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(5, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("", responseHeaders["path"]);
        Assert.Equal("*", responseHeaders["rawtarget"]);
        Assert.Equal("0", responseHeaders["content-length"]);
    }

    [Fact]
    public async Task OptionsSlash_Accepted()
    {
        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "OPTIONS"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/")};

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoPath, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(5, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("/", responseHeaders["path"]);
        Assert.Equal("/", responseHeaders["rawtarget"]);
        Assert.Equal("0", responseHeaders["content-length"]);
    }

    [Fact]
    public async Task PathAndQuery_Separated()
    {
        // :path and :scheme are not allowed, :authority is optional
        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/a/path?a&que%35ry")};

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(context =>
        {
            context.Response.Headers["path"] = context.Request.Path.Value;
            context.Response.Headers["query"] = context.Request.QueryString.Value;
            context.Response.Headers["rawtarget"] = context.Features.Get<IHttpRequestFeature>().RawTarget;
            return Task.CompletedTask;
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(6, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("/a/path", responseHeaders["path"]);
        Assert.Equal("?a&que%35ry", responseHeaders["query"]);
        Assert.Equal("/a/path?a&que%35ry", responseHeaders["rawtarget"]);
        Assert.Equal("0", responseHeaders["content-length"]);
    }

    [Theory]
    [InlineData("/", "/")]
    [InlineData("/a%5E", "/a^")]
    [InlineData("/a%E2%82%AC", "/a€")]
    [InlineData("/a%2Fb", "/a%2Fb")] // Forward slash, not decoded
    [InlineData("/a%b", "/a%b")] // Incomplete encoding, not decoded
    [InlineData("/a/b/c/../d", "/a/b/d")] // Navigation processed
    [InlineData("/a/b/c/../../../../d", "/d")] // Navigation escape prevented
    [InlineData("/a/b/c/.%2E/d", "/a/b/d")] // Decode before navigation processing
    public async Task Path_DecodedAndNormalized(string input, string expected)
    {
        // :path and :scheme are not allowed, :authority is optional
        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, input)};

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(context =>
        {
            Assert.Equal(expected, context.Request.Path.Value);
            Assert.Equal(input, context.Features.Get<IHttpRequestFeature>().RawTarget);
            return Task.CompletedTask;
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders["content-length"]);
    }

    [Theory]
    [InlineData(":path", "/")]
    [InlineData(":scheme", "http")]
    public async Task ConnectMethod_WithSchemeOrPath_Reset(string headerName, string value)
    {
        // :path and :scheme are not allowed, :authority is optional
        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(headerName, value) };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.Http3ErrorConnectMustNotSendSchemeOrPath);
    }

    [Theory]
    [InlineData("https")]
    [InlineData("ftp")]
    public async Task SchemeMismatch_Reset(string scheme)
    {
        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, scheme) }; // Not the expected "http"

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.FormatHttp3StreamErrorSchemeMismatch(scheme, "http"));
    }

    [Theory]
    [InlineData("https")]
    [InlineData("ftp")]
    public async Task SchemeMismatchAllowed_Processed(string scheme)
    {
        _serviceContext.ServerOptions.AllowAlternateSchemes = true;

        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, scheme) }; // Not the expected "http"

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(context =>
        {
            Assert.Equal(scheme, context.Request.Scheme);
            return Task.CompletedTask;
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders["content-length"]);
    }

    [Theory]
    [InlineData("https,http")]
    [InlineData("http://fakehost/")]
    public async Task SchemeMismatchAllowed_InvalidScheme_Reset(string scheme)
    {
        _serviceContext.ServerOptions.AllowAlternateSchemes = true;

        var headers = new[] { new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, scheme) }; // Not the expected "http"

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.FormatHttp3StreamErrorSchemeMismatch(scheme, "http"));
    }

    [Fact]
    public async Task MissingAuthority_200Status()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders["content-length"]);
    }

    [Fact]
    public async Task EmptyAuthority_200Status()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, ""),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders["content-length"]);
    }

    [Fact]
    public async Task MissingAuthorityFallsBackToHost_200Status()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("Host", "abc"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoHost, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(4, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders[HeaderNames.ContentLength]);
        Assert.Equal("abc", responseHeaders[HeaderNames.Host]);
    }

    [Fact]
    public async Task EmptyAuthorityIgnoredOverHost_200Status()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, ""),
            new KeyValuePair<string, string>("Host", "abc"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoHost, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(4, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders[HeaderNames.ContentLength]);
        Assert.Equal("abc", responseHeaders[HeaderNames.Host]);
    }

    [Fact]
    public async Task AuthorityOverridesHost_200Status()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "def"),
            new KeyValuePair<string, string>("Host", "abc"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoHost, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(4, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders[HeaderNames.ContentLength]);
        Assert.Equal("def", responseHeaders[HeaderNames.Host]);
    }

    [Fact]
    public async Task AuthorityOverridesInvalidHost_200Status()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "def"),
            new KeyValuePair<string, string>("Host", "a=bc"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoHost, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(4, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders[HeaderNames.ContentLength]);
        Assert.Equal("def", responseHeaders[HeaderNames.Host]);
    }

    [Fact]
    public async Task InvalidAuthority_Reset()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "local=host:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("local=host:80"));
    }

    [Fact]
    public async Task InvalidAuthorityWithValidHost_Reset()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "d=ef"),
            new KeyValuePair<string, string>("Host", "abc"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("d=ef"));
    }

    [Fact]
    public async Task TwoHosts_StreamReset()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("Host", "host1"),
            new KeyValuePair<string, string>("Host", "host2"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.FormatBadRequest_InvalidHostHeader_Detail("host1,host2"));
    }

    [Fact]
    public async Task MaxRequestLineSize_Reset()
    {
        // Default 8kb limit
        // This test has to work around the HPack parser limit for incoming field sizes over 4kb. That's going to be a problem for people with long urls.
        // https://github.com/aspnet/KestrelHttpServer/issues/2872
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET" + new string('a', 1024 * 3)),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/Hello/How/Are/You/" + new string('a', 1024 * 3)),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost" + new string('a', 1024 * 3) + ":80"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.RequestRejected,
            AssertExpectedErrorMessages,
            CoreStrings.BadRequest_RequestLineTooLong);
    }

    [Fact]
    public async Task ContentLength_Received_SingleDataFrame_Verified()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var buffer = new byte[100];
            var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(12, read);
            read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(0, read);
        }, headers, endStream: false);
        await requestStream.SendDataAsync(new byte[12], endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ContentLength_Received_MultipleDataFrame_Verified()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var buffer = new byte[100];
            var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            var total = read;
            while (read > 0)
            {
                read = await context.Request.Body.ReadAsync(buffer, total, buffer.Length - total);
                total += read;
            }
            Assert.Equal(12, total);
        }, headers, endStream: false);

        await requestStream.SendDataAsync(new byte[1], endStream: false);
        await requestStream.SendDataAsync(new byte[3], endStream: false);
        await requestStream.SendDataAsync(new byte[8], endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task ContentLength_Received_MultipleDataFrame_ReadViaPipe_Verified()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var readResult = await context.Request.BodyReader.ReadAsync();
            while (!readResult.IsCompleted)
            {
                context.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
                readResult = await context.Request.BodyReader.ReadAsync();
            }

            Assert.Equal(12, readResult.Buffer.Length);
            context.Request.BodyReader.AdvanceTo(readResult.Buffer.End);
        }, headers, endStream: false);

        await requestStream.SendDataAsync(new byte[1], endStream: false);
        await requestStream.SendDataAsync(new byte[3], endStream: false);
        await requestStream.SendDataAsync(new byte[8], endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", responseHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task RemoveConnectionSpecificHeaders()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var response = context.Response;

            response.Headers.Add(HeaderNames.TransferEncoding, "chunked");
            response.Headers.Add(HeaderNames.Upgrade, "websocket");
            response.Headers.Add(HeaderNames.Connection, "Keep-Alive");
            response.Headers.Add(HeaderNames.KeepAlive, "timeout=5, max=1000");
            response.Headers.Add(HeaderNames.ProxyConnection, "keep-alive");

            await response.WriteAsync("Hello world");
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(2, responseHeaders.Count);

        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello world", Encoding.ASCII.GetString(responseData.ToArray()));

        Assert.Contains(LogMessages, m => m.Message.Equals("One or more of the following response headers have been removed because they are invalid for HTTP/2 and HTTP/3 responses: 'Connection', 'Transfer-Encoding', 'Keep-Alive', 'Upgrade' and 'Proxy-Connection'."));
    }

    [Fact]
    public async Task ContentLength_Received_NoDataFrames_Reset()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
        };

        var requestDelegateCalled = false;
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(c =>
        {
            // Bad content-length + end stream means the request delegate
            // is never called by the server.
            requestDelegateCalled = true;
            return Task.CompletedTask;
        }, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.ProtocolError,
            AssertExpectedErrorMessages,
            CoreStrings.Http3StreamErrorLessDataThanLength);

        Assert.False(requestDelegateCalled);
    }

    [Fact]
    public async Task EndRequestStream_ContinueReadingFromResponse()
    {
        var headersTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        var data = new byte[] { 1, 2, 3, 4, 5, 6 };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            await context.Response.BodyWriter.FlushAsync();

            await headersTcs.Task;

            for (var i = 0; i < data.Length; i++)
            {
                await Task.Delay(50);
                await context.Response.BodyWriter.WriteAsync(new byte[] { data[i] });
            }
        }, headers, endStream: true);
        await requestStream.ExpectHeadersAsync();

        headersTcs.SetResult();

        var receivedData = new List<byte>();
        while (receivedData.Count < data.Length)
        {
            var frameData = await requestStream.ExpectDataAsync();
            receivedData.AddRange(frameData.ToArray());
        }

        Assert.Equal(data, receivedData);

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task FlushPipeAsync_OnStoppedHttp3Stream_ReturnsFlushResultWithIsCompletedTrue()
    {
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestHeaders = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/hello"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>(HeaderNames.ContentType, "application/json")
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Abort();

                var payload = Encoding.ASCII.GetBytes("hello world");
                var result = await context.Response.BodyWriter.WriteAsync(payload);

                Assert.True(result.IsCompleted);
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, requestHeaders, endStream: true);

        await requestStream.ExpectReceiveEndOfStream();
        await appTcs.Task.DefaultTimeout();
    }

    [Fact]
    public async Task FlushPipeAsync_OnCanceledPendingFlush_ReturnsFlushResultWithIsCanceledTrue()
    {
        var requestHeaders = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/hello"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>(HeaderNames.ContentType, "application/json")
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            context.Response.BodyWriter.CancelPendingFlush();
            var payload = Encoding.ASCII.GetBytes("hello,");
            var cancelledResult = await context.Response.BodyWriter.WriteAsync(payload);
            Assert.True(cancelledResult.IsCanceled);

            var secondPayload = Encoding.ASCII.GetBytes(" world");
            var goodResult = await context.Response.BodyWriter.WriteAsync(secondPayload);
            Assert.False(goodResult.IsCanceled);
        }, requestHeaders, endStream: true);
        await requestStream.ExpectHeadersAsync();

        var response = await requestStream.ExpectDataAsync();
        Assert.Equal("hello,", Encoding.UTF8.GetString(response.Span));

        var secondResponse = await requestStream.ExpectDataAsync();
        Assert.Equal(" world", Encoding.UTF8.GetString(secondResponse.Span));

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task ResponseTrailers_WithoutData_Sent()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(context =>
        {
            var trailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();

            trailersFeature.Trailers.Add("Trailer1", "Value1");
            trailersFeature.Trailers.Add("Trailer2", "Value2");

            return Task.CompletedTask;
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        var responseTrailers = await requestStream.ExpectHeadersAsync();

        Assert.Equal(2, responseTrailers.Count);
        Assert.Equal("Value1", responseTrailers["Trailer1"]);
        Assert.Equal("Value2", responseTrailers["Trailer2"]);
    }

    [Fact]
    public async Task ResponseHeaders_WithNonAscii_Throws()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var trailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();

            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.ContentType = "Custom 你好 Type");
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom 你好 Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom \r Value"));
            await context.Response.WriteAsync("Hello World");
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.ASCII.GetString(responseData.ToArray()));

        Assert.Equal(2, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
    }

    [Fact]
    public async Task ResponseHeaders_WithNonAsciiAndCustomEncoder_Works()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        _serviceContext.ServerOptions.ResponseHeaderEncodingSelector = _ => Encoding.UTF8;
        _serviceContext.ServerOptions.RequestHeaderEncodingSelector = _ => Encoding.UTF8; // Used for decoding response.

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var trailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();

            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.Headers.Append("CustomName", "Custom \r Value"));
            context.Response.ContentType = "Custom 你好 Type";
            context.Response.Headers.Append("CustomName", "Custom 你好 Value");
            await context.Response.WriteAsync("Hello World");
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.ASCII.GetString(responseData.ToArray()));

        Assert.Equal(4, responseHeaders.Count);
        Assert.Contains("date", responseHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
        Assert.Equal("Custom 你好 Type", responseHeaders[HeaderNames.ContentType]);
        Assert.Equal("Custom 你好 Value", responseHeaders["CustomName"]);
    }

    [Fact]
    public async Task ResponseHeaders_WithInvalidValuesAndCustomEncoder_AbortsConnection()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var encoding = Encoding.GetEncoding(Encoding.Latin1.CodePage, EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
        _serviceContext.ServerOptions.ResponseHeaderEncodingSelector = _ => encoding;

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            context.Response.Headers.Append("CustomName", "Custom 你好 Value");
            await context.Response.WriteAsync("Hello World");
        }, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.InternalError,
            AssertExpectedErrorMessages,
            "");
    }

    [Fact]
    public async Task ResponseTrailers_WithData_Sent()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var trailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();

            trailersFeature.Trailers.Add("Trailer1", "Value1");
            trailersFeature.Trailers.Add("Trailer2", "Value2");

            await context.Response.WriteAsync("Hello world");
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello world", Encoding.ASCII.GetString(responseData.ToArray()));

        var responseTrailers = await requestStream.ExpectHeadersAsync();

        Assert.Equal(2, responseTrailers.Count);
        Assert.Equal("Value1", responseTrailers["Trailer1"]);
        Assert.Equal("Value2", responseTrailers["Trailer2"]);
    }

    [Fact]
    public async Task ResponseTrailers_WithExeption500_Cleared()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(context =>
        {
            var trailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();

            trailersFeature.Trailers.Add("Trailer1", "Value1");
            trailersFeature.Trailers.Add("Trailer2", "Value2");

            throw new NotImplementedException("Test Exception");
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task ResponseTrailers_WithNonAscii_Throws()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("CustomName", "Custom 你好 Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("CustomName", "Custom \r Value"));
            // ETag is one of the few special cased trailers. Accept is not.
            Assert.Throws<InvalidOperationException>(() => context.Features.Get<IHttpResponseTrailersFeature>().Trailers.ETag = "Custom 你好 Tag");
            Assert.Throws<InvalidOperationException>(() => context.Features.Get<IHttpResponseTrailersFeature>().Trailers.Accept = "Custom 你好 Tag");
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.ASCII.GetString(responseData.ToArray()));
        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task ResponseTrailers_WithNonAsciiAndCustomEncoder_Works()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        _serviceContext.ServerOptions.ResponseHeaderEncodingSelector = _ => Encoding.UTF8;
        _serviceContext.ServerOptions.RequestHeaderEncodingSelector = _ => Encoding.UTF8; // Used for decoding response.

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("Custom你好Name", "Custom Value"));
            Assert.Throws<InvalidOperationException>(() => context.Response.AppendTrailer("CustomName", "Custom \r Value"));
            context.Response.AppendTrailer("CustomName", "Custom 你好 Value");
            // ETag is one of the few special cased trailers. Accept is not.
            context.Features.Get<IHttpResponseTrailersFeature>().Trailers.ETag = "Custom 你好 Tag";
            context.Features.Get<IHttpResponseTrailersFeature>().Trailers.Accept = "Custom 你好 Accept";
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.ASCII.GetString(responseData.ToArray()));

        var responseTrailers = await requestStream.ExpectHeadersAsync();
        Assert.Equal(3, responseTrailers.Count);
        Assert.Equal("Custom 你好 Value", responseTrailers["CustomName"]);
        Assert.Equal("Custom 你好 Tag", responseTrailers[HeaderNames.ETag]);
        Assert.Equal("Custom 你好 Accept", responseTrailers[HeaderNames.Accept]);

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task ResponseTrailers_WithInvalidValuesAndCustomEncoder_AbortsConnection()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var encoding = Encoding.GetEncoding(Encoding.Latin1.CodePage, EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
        _serviceContext.ServerOptions.ResponseHeaderEncodingSelector = _ => encoding;

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            await context.Response.WriteAsync("Hello World");
            context.Response.AppendTrailer("CustomName", "Custom 你好 Value");
        }, headers, endStream: true);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.ASCII.GetString(responseData.ToArray()));

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.InternalError,
            AssertExpectedErrorMessages,
            "");
    }

    [Fact]
    public async Task ResetStream_ReturnStreamError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(context =>
        {
            var resetFeature = context.Features.Get<IHttpResetFeature>();

            resetFeature.Reset((int)Http3ErrorCode.RequestCancelled);

            return Task.CompletedTask;
        }, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.RequestCancelled,
            AssertExpectedErrorMessages,
            CoreStrings.FormatHttp3StreamResetByApplication(Http3Formatting.ToFormattedErrorCode(Http3ErrorCode.RequestCancelled)));
    }

    [Fact]
    public async Task CompleteAsync_BeforeBodyStarted_SendsHeadersWithEndStream()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });
                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);
                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();

        clientTcs.SetResult();
        await appTcs.Task;

        Assert.Equal(3, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", decodedHeaders["content-length"]);

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task CompleteAsync_BeforeBodyStarted_WithTrailers_SendsHeadersAndTrailersWithEndStream()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });
                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();
                await context.Response.CompleteAsync().DefaultTimeout(); // Can be called twice, no-ops

                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);
                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        var decodedTrailers = await requestStream.ExpectHeadersAsync();

        clientTcs.SetResult();
        await appTcs.Task;

        await requestStream.ExpectReceiveEndOfStream();

        Assert.Equal(3, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", decodedHeaders["content-length"]);

        Assert.Single(decodedTrailers);
        Assert.Equal("Custom Value", decodedTrailers["CustomName"]);
    }

    [Fact]
    public async Task CompleteAsync_BeforeBodyStarted_WithTrailers_TruncatedContentLength_ThrowsAnd500()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                context.Response.ContentLength = 25;
                context.Response.AppendTrailer("CustomName", "Custom Value");

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.CompleteAsync().DefaultTimeout());
                Assert.Equal(CoreStrings.FormatTooFewBytesWritten(0, 25), ex.Message);

                Assert.True(startingTcs.Task.IsCompletedSuccessfully);
                Assert.False(context.Response.Headers.IsReadOnly);
                Assert.False(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();

        await appTcs.Task;

        await requestStream.ExpectReceiveEndOfStream();

        Assert.Equal(3, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("500", decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", decodedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task CompleteAsync_AfterBodyStarted_SendsBodyWithEndStream()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                await context.Response.CompleteAsync().DefaultTimeout();
                await context.Response.CompleteAsync().DefaultTimeout(); // Can be called twice, no-ops

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(2, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();

        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        clientTcs.SetResult();
        await appTcs.Task;

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task CompleteAsync_WriteAfterComplete_Throws()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });
                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);
                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.WriteAsync("2 Hello World").DefaultTimeout());
                Assert.Equal("Writing is not allowed after writer was completed.", ex.Message);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", decodedHeaders[HeaderNames.ContentLength]);

        clientTcs.SetResult();
        await appTcs.Task;

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task CompleteAsync_WriteAgainAfterComplete_Throws()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World").DefaultTimeout();
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.WriteAsync("2 Hello World").DefaultTimeout());
                Assert.Equal("Writing is not allowed after writer was completed.", ex.Message);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(2, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        clientTcs.SetResult();
        await appTcs.Task;

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task CompleteAsync_AdvanceAfterComplete_AdvanceThrows()
    {
        var tcs = new TaskCompletionSource();
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var memory = context.Response.BodyWriter.GetMemory(12);
            await context.Response.CompleteAsync();
            try
            {
                context.Response.BodyWriter.Advance(memory.Length);
            }
            catch (InvalidOperationException)
            {
                tcs.SetResult();
                return;
            }

            Assert.True(false);
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();

        Assert.Equal(3, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", decodedHeaders["content-length"]);

        await tcs.Task.DefaultTimeout();

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task CompleteAsync_AfterPipeWrite_WithTrailers_SendsBodyAndTrailersWithEndStream()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                var buffer = context.Response.BodyWriter.GetMemory();
                var length = Encoding.UTF8.GetBytes("Hello World", buffer.Span);
                context.Response.BodyWriter.Advance(length);

                Assert.False(startingTcs.Task.IsCompletedSuccessfully); // OnStarting did not get called.
                Assert.False(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(2, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        var decodedTrailers = await requestStream.ExpectHeadersAsync();
        Assert.Equal("Custom Value", decodedTrailers["CustomName"]);

        clientTcs.SetResult();
        await appTcs.Task;

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task CompleteAsync_AfterBodyStarted_WithTrailers_SendsBodyAndTrailersWithEndStream()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(2, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        var decodedTrailers = await requestStream.ExpectHeadersAsync();
        Assert.Equal("Custom Value", decodedTrailers["CustomName"]);

        clientTcs.SetResult();
        await appTcs.Task;

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task CompleteAsync_AfterBodyStarted_WithTrailers_TruncatedContentLength_ThrowsAndReset()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                context.Response.ContentLength = 25;
                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Response.CompleteAsync().DefaultTimeout());
                Assert.Equal(CoreStrings.FormatTooFewBytesWritten(11, 25), ex.Message);

                Assert.False(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(3, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("25", decodedHeaders[HeaderNames.ContentLength]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        clientTcs.SetResult();

        await requestStream.WaitForStreamErrorAsync(Http3ErrorCode.InternalError,
            expectedErrorMessage: CoreStrings.FormatTooFewBytesWritten(11, 25));

        await appTcs.Task;
    }

    [Fact]
    public async Task PipeWriterComplete_AfterBodyStarted_WithTrailers_TruncatedContentLength_ThrowsAndReset()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                context.Response.ContentLength = 25;
                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                var ex = Assert.Throws<InvalidOperationException>(() => context.Response.BodyWriter.Complete());
                Assert.Equal(CoreStrings.FormatTooFewBytesWritten(11, 25), ex.Message);

                Assert.False(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(3, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("25", decodedHeaders[HeaderNames.ContentLength]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        clientTcs.SetResult();

        await requestStream.WaitForStreamErrorAsync(Http3ErrorCode.InternalError,
            expectedErrorMessage: CoreStrings.FormatTooFewBytesWritten(11, 25));

        await appTcs.Task;
    }

    [Fact]
    public async Task AbortAfterCompleteAsync_GETWithResponseBodyAndTrailers_ResetsAfterResponse()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // RequestAborted will no longer fire after CompleteAsync.
                Assert.False(context.RequestAborted.CanBeCanceled);
                context.Abort();

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(2, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        var decodedTrailers = await requestStream.ExpectHeadersAsync();
        Assert.Equal("Custom Value", decodedTrailers["CustomName"]);

        await requestStream.WaitForStreamErrorAsync(Http3ErrorCode.InternalError, expectedErrorMessage: null);

        clientTcs.SetResult();
        await appTcs.Task;
    }

    [Fact]
    public async Task AbortAfterCompleteAsync_POSTWithResponseBodyAndTrailers_RequestBodyThrows()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                var requestBodyTask = context.Request.BodyReader.ReadAsync();

                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // RequestAborted will no longer fire after CompleteAsync.
                Assert.False(context.RequestAborted.CanBeCanceled);
                context.Abort();

                await Assert.ThrowsAsync<TaskCanceledException>(async () => await requestBodyTask);
                await Assert.ThrowsAsync<ConnectionAbortedException>(async () => await context.Request.BodyReader.ReadAsync());

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: false);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(2, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        var decodedTrailers = await requestStream.ExpectHeadersAsync();
        Assert.Equal("Custom Value", decodedTrailers["CustomName"]);

        await requestStream.WaitForStreamErrorAsync(Http3ErrorCode.InternalError, expectedErrorMessage: null);

        clientTcs.SetResult();
        await appTcs.Task;
    }

    [Fact]
    public async Task ResetAfterCompleteAsync_GETWithResponseBodyAndTrailers_ResetsAfterResponse()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // RequestAborted will no longer fire after CompleteAsync.
                Assert.False(context.RequestAborted.CanBeCanceled);
                var resetFeature = context.Features.Get<IHttpResetFeature>();
                Assert.NotNull(resetFeature);
                resetFeature.Reset((int)Http3ErrorCode.NoError);

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: true);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(2, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        var decodedTrailers = await requestStream.ExpectHeadersAsync();
        Assert.Equal("Custom Value", decodedTrailers["CustomName"]);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.NoError,
            expectedErrorMessage: "The HTTP/3 stream was reset by the application with error code H3_NO_ERROR.");

        clientTcs.SetResult();
        await appTcs.Task;
    }

    [Fact]
    public async Task ResetAfterCompleteAsync_POSTWithResponseBodyAndTrailers_RequestBodyThrows()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            try
            {
                var requestBodyTask = context.Request.BodyReader.ReadAsync();

                context.Response.OnStarting(() => { startingTcs.SetResult(); return Task.CompletedTask; });

                await context.Response.WriteAsync("Hello World");
                Assert.True(startingTcs.Task.IsCompletedSuccessfully); // OnStarting got called.
                Assert.True(context.Response.Headers.IsReadOnly);

                context.Response.AppendTrailer("CustomName", "Custom Value");

                await context.Response.CompleteAsync().DefaultTimeout();

                Assert.True(context.Features.Get<IHttpResponseTrailersFeature>().Trailers.IsReadOnly);

                // RequestAborted will no longer fire after CompleteAsync.
                Assert.False(context.RequestAborted.CanBeCanceled);
                var resetFeature = context.Features.Get<IHttpResetFeature>();
                Assert.NotNull(resetFeature);
                resetFeature.Reset((int)Http3ErrorCode.NoError);

                await Assert.ThrowsAsync<TaskCanceledException>(async () => await requestBodyTask);
                await Assert.ThrowsAsync<ConnectionAbortedException>(async () => await context.Request.BodyReader.ReadAsync());

                // Make sure the client gets our results from CompleteAsync instead of from the request delegate exiting.
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, headers, endStream: false);

        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(2, decodedHeaders.Count);
        Assert.Contains("date", decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello World", Encoding.UTF8.GetString(data.Span));

        var decodedTrailers = await requestStream.ExpectHeadersAsync();
        Assert.Equal("Custom Value", decodedTrailers["CustomName"]);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.NoError,
            expectedErrorMessage: "The HTTP/3 stream was reset by the application with error code H3_NO_ERROR.");

        clientTcs.SetResult();
        await appTcs.Task;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DataBeforeHeaders_UnexpectedFrameError(bool pendingStreamsEnabled)
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = pendingStreamsEnabled;

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, null);

        await (pendingStreamsEnabled ? requestStream.OnUnidentifiedStreamCreatedTask : requestStream.OnStreamCreatedTask);

        await requestStream.SendDataAsync(Encoding.UTF8.GetBytes("This is invalid."));

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.UnexpectedFrame,
            expectedErrorMessage: CoreStrings.Http3StreamErrorDataReceivedBeforeHeaders);
    }

    [Fact]
    public async Task RequestTrailers_CanReadTrailersFromRequest()
    {
        string testValue = null;

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var trailers = new[]
        {
                new KeyValuePair<string, string>("TestName", "TestValue"),
            };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async c =>
        {
            await c.Request.Body.DrainAsync(default);

            testValue = c.Request.GetTrailer("TestName");
        }, headers, endStream: false);
        await requestStream.SendDataAsync(Encoding.UTF8.GetBytes("Hello world"));
        await requestStream.SendHeadersAsync(trailers, endStream: true);

        await requestStream.ExpectHeadersAsync();

        Assert.Equal("TestValue", testValue);

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task FrameAfterTrailers_UnexpectedFrameError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var trailers = new[]
        {
                new KeyValuePair<string, string>("TestName", "TestValue"),
            };
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async c =>
        {
            // Send headers
            await c.Response.Body.FlushAsync();

            await tcs.Task;
        }, headers, endStream: false);

        await requestStream.ExpectHeadersAsync();

        await requestStream.SendDataAsync(Encoding.UTF8.GetBytes("Hello world"));
        await requestStream.SendHeadersAsync(trailers, endStream: false);
        await requestStream.SendDataAsync(Encoding.UTF8.GetBytes("This is invalid."));

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.UnexpectedFrame,
            expectedErrorMessage: CoreStrings.FormatHttp3StreamErrorFrameReceivedAfterTrailers(Http3Formatting.ToFormattedType(Http3FrameType.Data)));

        tcs.SetResult();

        await Http3Api.WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: null,
            Http3ErrorCode.UnexpectedFrame,
            null);
        MetricsAssert.Equal(ConnectionEndReason.UnexpectedFrame, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task TrailersWithoutEndingStream_ErrorAccessingTrailers()
    {
        var readTrailersTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var syncPoint = new SyncPoint();
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var trailers = new[]
        {
                new KeyValuePair<string, string>("TestName", "TestValue"),
            };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async c =>
        {
            var data = new byte[1024];
            await c.Request.Body.ReadAsync(data);

            await syncPoint.WaitToContinue();

            try
            {
                c.Request.GetTrailer("TestName");
            }
            catch (Exception ex)
            {
                readTrailersTcs.TrySetException(ex);
                throw;
            }
        }, headers, endStream: false);
        await requestStream.SendDataAsync(Encoding.UTF8.GetBytes("Hello world"));
        await requestStream.SendHeadersAsync(trailers, endStream: false);

        await syncPoint.WaitForSyncPoint().DefaultTimeout();
        syncPoint.Continue();

        // Stream not ended after trailing headers.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => readTrailersTcs.Task).DefaultTimeout();
        Assert.Equal("The request trailers are not available yet. They may not be available until the full request body is read.", ex.Message);
    }

    [Theory]
    [InlineData(nameof(Http3FrameType.MaxPushId), true)]
    [InlineData(nameof(Http3FrameType.Settings), true)]
    [InlineData(nameof(Http3FrameType.CancelPush), true)]
    [InlineData(nameof(Http3FrameType.GoAway), true)]
    [InlineData(nameof(Http3FrameType.MaxPushId), false)]
    [InlineData(nameof(Http3FrameType.Settings), false)]
    [InlineData(nameof(Http3FrameType.CancelPush), false)]
    [InlineData(nameof(Http3FrameType.GoAway), false)]
    public async Task UnexpectedRequestFrame(string frameType, bool pendingStreamsEnabled)
    {
        Http3Api._serviceContext.ServerOptions.EnableWebTransportAndH3Datagrams = pendingStreamsEnabled;

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoApplication, null);

        await (pendingStreamsEnabled ? requestStream.OnUnidentifiedStreamCreatedTask : requestStream.OnStreamCreatedTask);

        var f = Enum.Parse<Http3FrameType>(frameType);
        await requestStream.SendFrameAsync(f, Memory<byte>.Empty);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.UnexpectedFrame,
            expectedErrorMessage: CoreStrings.FormatHttp3ErrorUnsupportedFrameOnRequestStream(Http3Formatting.ToFormattedType(f)));

        await Http3Api.WaitForConnectionErrorAsync<Http3ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 4,
            expectedErrorCode: Http3ErrorCode.UnexpectedFrame,
            matchExpectedErrorMessage: AssertExpectedErrorMessages,
            expectedErrorMessage: CoreStrings.FormatHttp3ErrorUnsupportedFrameOnRequestStream(Http3Formatting.ToFormattedType(f)));
        MetricsAssert.Equal(ConnectionEndReason.UnexpectedFrame, Http3Api.ConnectionTags);
    }

    [Theory]
    [InlineData(nameof(Http3FrameType.PushPromise))]
    public async Task UnexpectedServerFrame(string frameType)
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoApplication, headers);

        await requestStream.OnStreamCreatedTask;

        var f = Enum.Parse<Http3FrameType>(frameType);
        await requestStream.SendFrameAsync(f, Memory<byte>.Empty);

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.UnexpectedFrame,
            expectedErrorMessage: CoreStrings.FormatHttp3ErrorUnsupportedFrameOnServer(Http3Formatting.ToFormattedType(f)));

        await Http3Api.WaitForConnectionErrorAsync<ConnectionAbortedException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: null,
            Http3ErrorCode.UnexpectedFrame,
            null);
        MetricsAssert.Equal(ConnectionEndReason.UnexpectedFrame, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task RequestIncomplete()
    {
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_echoApplication, null);

        await requestStream.EndStreamAsync();

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.RequestIncomplete,
            expectedErrorMessage: CoreStrings.Http3StreamErrorRequestEndedNoHeaders);
    }

    [Fact]
    public Task HEADERS_Received_HeaderBlockContainsUnknownPseudoHeaderField_ConnectionError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(":unknown", "0"),
        };

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.HttpErrorUnknownPseudoHeaderField);
    }

    [Fact]
    public Task HEADERS_Received_HeaderBlockContainsResponsePseudoHeaderField_ConnectionError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Status, "200"),
        };

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.HttpErrorResponsePseudoHeaderField);
    }

    public static TheoryData<IEnumerable<KeyValuePair<string, string>>> DuplicatePseudoHeaderFieldData
    {
        get
        {
            var data = new TheoryData<IEnumerable<KeyValuePair<string, string>>>();
            var requestHeaders = new[]
            {
                new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
                new KeyValuePair<string, string>(InternalHeaderNames.Authority, "127.0.0.1"),
                new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            };

            foreach (var headerField in requestHeaders)
            {
                var headers = requestHeaders.Concat(new[] { new KeyValuePair<string, string>(headerField.Key, headerField.Value) });
                data.Add(headers);
            }

            return data;
        }
    }

    public static TheoryData<IEnumerable<KeyValuePair<string, string>>> ConnectMissingPseudoHeaderFieldData
    {
        get
        {
            var data = new TheoryData<IEnumerable<KeyValuePair<string, string>>>();
            var methodHeader = new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT");
            var headers = new[] { methodHeader };
            data.Add(headers);

            return data;
        }
    }

    public static TheoryData<IEnumerable<KeyValuePair<string, string>>> PseudoHeaderFieldAfterRegularHeadersData
    {
        get
        {
            var data = new TheoryData<IEnumerable<KeyValuePair<string, string>>>();
            var requestHeaders = new[]
            {
                new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
                new KeyValuePair<string, string>(InternalHeaderNames.Authority, "127.0.0.1"),
                new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
                new KeyValuePair<string, string>("content-length", "0")
            };

            foreach (var headerField in requestHeaders.Where(h => h.Key.StartsWith(':')))
            {
                var headers = requestHeaders.Except(new[] { headerField }).Concat(new[] { headerField });
                data.Add(headers);
            }

            return data;
        }
    }

    public static TheoryData<IEnumerable<KeyValuePair<string, string>>> MissingPseudoHeaderFieldData
    {
        get
        {
            var data = new TheoryData<IEnumerable<KeyValuePair<string, string>>>();
            var requestHeaders = new[]
            {
                new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
                new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
                new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            };

            foreach (var headerField in requestHeaders)
            {
                var headers = requestHeaders.Except(new[] { headerField });
                data.Add(headers);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(DuplicatePseudoHeaderFieldData))]
    public Task HEADERS_Received_HeaderBlockContainsDuplicatePseudoHeaderField_ConnectionError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.HttpErrorDuplicatePseudoHeaderField);
    }

    [Theory]
    [MemberData(nameof(ConnectMissingPseudoHeaderFieldData))]
    public async Task HEADERS_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_MethodIsCONNECT_NoError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Theory]
    [MemberData(nameof(PseudoHeaderFieldAfterRegularHeadersData))]
    public Task HEADERS_Received_HeaderBlockContainsPseudoHeaderFieldAfterRegularHeaders_ConnectionError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, expectedErrorMessage: CoreStrings.HttpErrorPseudoHeaderFieldAfterRegularHeaders);
    }

    private async Task HEADERS_Received_InvalidHeaderFields_StreamError(IEnumerable<KeyValuePair<string, string>> headers, string expectedErrorMessage, Http3ErrorCode? errorCode = null)
    {
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.WaitForStreamErrorAsync(
            errorCode ?? Http3ErrorCode.MessageError,
            AssertExpectedErrorMessages,
            expectedErrorMessage);
    }

    [Theory]
    [MemberData(nameof(MissingPseudoHeaderFieldData))]
    public async Task HEADERS_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_StreamError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);
        await requestStream.WaitForStreamErrorAsync(
             Http3ErrorCode.MessageError,
             expectedErrorMessage: CoreStrings.HttpErrorMissingMandatoryPseudoHeaderFields);
    }

    [Fact]
    public async Task HEADERS_Received_HeaderBlockOverLimit_431()
    {
        // > 32kb
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("a", _4kHeaderValue),
            new KeyValuePair<string, string>("b", _4kHeaderValue),
            new KeyValuePair<string, string>("c", _4kHeaderValue),
            new KeyValuePair<string, string>("d", _4kHeaderValue),
            new KeyValuePair<string, string>("e", _4kHeaderValue),
            new KeyValuePair<string, string>("f", _4kHeaderValue),
            new KeyValuePair<string, string>("g", _4kHeaderValue),
            new KeyValuePair<string, string>("h", _4kHeaderValue),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_notImplementedApp, headers, endStream: true);

        var receivedHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();

        Assert.Equal(3, receivedHeaders.Count);
        Assert.Contains("date", receivedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("431", receivedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", receivedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public Task HEADERS_Received_HeaderBlockOverLimitx2_ConnectionError()
    {
        // > 32kb * 2 to exceed graceful handling limit
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("a", _4kHeaderValue),
            new KeyValuePair<string, string>("b", _4kHeaderValue),
            new KeyValuePair<string, string>("c", _4kHeaderValue),
            new KeyValuePair<string, string>("d", _4kHeaderValue),
            new KeyValuePair<string, string>("e", _4kHeaderValue),
            new KeyValuePair<string, string>("f", _4kHeaderValue),
            new KeyValuePair<string, string>("g", _4kHeaderValue),
            new KeyValuePair<string, string>("h", _4kHeaderValue),
            new KeyValuePair<string, string>("i", _4kHeaderValue),
            new KeyValuePair<string, string>("j", _4kHeaderValue),
            new KeyValuePair<string, string>("k", _4kHeaderValue),
            new KeyValuePair<string, string>("l", _4kHeaderValue),
            new KeyValuePair<string, string>("m", _4kHeaderValue),
            new KeyValuePair<string, string>("n", _4kHeaderValue),
            new KeyValuePair<string, string>("o", _4kHeaderValue),
            new KeyValuePair<string, string>("p", _4kHeaderValue),
        };

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.BadRequest_HeadersExceedMaxTotalSize, Http3ErrorCode.RequestRejected);
    }

    [Fact]
    public async Task HEADERS_Received_TooManyHeaders_431()
    {
        // > MaxRequestHeaderCount (100)
        var headers = new List<KeyValuePair<string, string>>();
        headers.AddRange(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        });
        for (var i = 0; i < 100; i++)
        {
            headers.Add(new KeyValuePair<string, string>(i.ToString(CultureInfo.InvariantCulture), i.ToString(CultureInfo.InvariantCulture)));
        }

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_notImplementedApp, headers, endStream: true);

        var receivedHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();

        Assert.Equal(3, receivedHeaders.Count);
        Assert.Contains("date", receivedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("431", receivedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", receivedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public Task HEADERS_Received_TooManyHeadersx2_ConnectionError()
    {
        // > MaxRequestHeaderCount (100) * 2 to exceed graceful handling limit
        var headers = new List<KeyValuePair<string, string>>();
        headers.AddRange(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        });
        for (var i = 0; i < 200; i++)
        {
            headers.Add(new KeyValuePair<string, string>(i.ToString(CultureInfo.InvariantCulture), i.ToString(CultureInfo.InvariantCulture)));
        }

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.BadRequest_TooManyHeaders);
    }

    [Fact]
    public Task HEADERS_Received_InvalidCharactersInHeaderValue_ConnectionError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("Custom", "val\0ue"),
        };

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.BadRequest_MalformedRequestInvalidHeaders);
    }

    [Fact]
    public Task HEADERS_Received_InvalidCharactersInHeaderName_ConnectionError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("Cus\0tom", "value"),
        };

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.BadRequest_InvalidCharactersInHeaderName);
    }

    [Fact]
    public Task HEADERS_Received_HeaderBlockContainsConnectionHeader_ConnectionError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("connection", "keep-alive")
        };

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.HttpErrorConnectionSpecificHeaderField);
    }

    [Fact]
    public Task HEADERS_Received_HeaderBlockContainsTEHeader_ValueIsNotTrailers_ConnectionError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("te", "trailers, deflate")
        };

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.HttpErrorConnectionSpecificHeaderField);
    }

    [Fact]
    public async Task HEADERS_Received_HeaderBlockContainsTEHeader_ValueIsTrailers_NoError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("te", "trailers")
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(_noopApplication, headers, endStream: true);

        await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task MaxRequestBodySize_ContentLengthUnder_200()
    {
        _serviceContext.ServerOptions.Limits.MaxRequestBodySize = 15;
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var buffer = new byte[100];
            var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(12, read);
            read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(0, read);
        }, headers, endStream: false);
        await requestStream.SendDataAsync(new byte[12], endStream: true);

        var receivedHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();

        Assert.Equal(3, receivedHeaders.Count);
        Assert.Contains("date", receivedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", receivedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", receivedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task MaxRequestBodySize_ContentLengthOver_413()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException exception = null;
#pragma warning restore CS0618 // Type or member is obsolete
        _serviceContext.ServerOptions.Limits.MaxRequestBodySize = 10;
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var buffer = new byte[100];
                while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
            });
            ExceptionDispatchInfo.Capture(exception).Throw();
        }, headers, endStream: false);

        var receivedHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();

        await requestStream.OnStreamCompletedTask.DefaultTimeout();

        Assert.Contains(LogMessages, m => m.Message.Contains("the application completed without reading the entire request body."));
        Assert.Equal("The application completed without reading the entire request body.", requestStream.AbortReadException.Message);

        Assert.Equal(3, receivedHeaders.Count);
        Assert.Contains("date", receivedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("413", receivedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", receivedHeaders[HeaderNames.ContentLength]);

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task MaxRequestBodySize_NoContentLength_Under_200()
    {
        _serviceContext.ServerOptions.Limits.MaxRequestBodySize = 15;
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var buffer = new byte[100];
            var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(12, read);
            read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(0, read);
        }, headers, endStream: false);
        await requestStream.SendDataAsync(new byte[12], endStream: true);

        var receivedHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();

        Assert.Equal(3, receivedHeaders.Count);
        Assert.Contains("date", receivedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", receivedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", receivedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public async Task MaxRequestBodySize_NoContentLength_Over_413()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException exception = null;
#pragma warning restore CS0618 // Type or member is obsolete
        _serviceContext.ServerOptions.Limits.MaxRequestBodySize = 10;
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var buffer = new byte[100];
                while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
            });
            ExceptionDispatchInfo.Capture(exception).Throw();
        }, headers, endStream: false);
        await requestStream.SendDataAsync(new byte[6], endStream: false);
        await requestStream.SendDataAsync(new byte[6], endStream: false);

        var receivedHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();

        await requestStream.OnStreamCompletedTask.DefaultTimeout();
        Assert.Contains(LogMessages, m => m.Message.Contains("the application completed without reading the entire request body."));

        Assert.Equal(3, receivedHeaders.Count);
        Assert.Contains("date", receivedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("413", receivedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", receivedHeaders[HeaderNames.ContentLength]);

        Assert.NotNull(exception);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MaxRequestBodySize_AppCanLowerLimit(bool includeContentLength)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException exception = null;
#pragma warning restore CS0618 // Type or member is obsolete
        _serviceContext.ServerOptions.Limits.MaxRequestBodySize = 20;
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        if (includeContentLength)
        {
            headers.Concat(new[]
                {
                        new KeyValuePair<string, string>(HeaderNames.ContentLength, "18"),
                    });
        }
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            Assert.False(context.Features.Get<IHttpMaxRequestBodySizeFeature>().IsReadOnly);
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 17;
#pragma warning disable CS0618 // Type or member is obsolete
            exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var buffer = new byte[100];
                while (await context.Request.Body.ReadAsync(buffer, 0, buffer.Length) > 0) { }
            });
            Assert.True(context.Features.Get<IHttpMaxRequestBodySizeFeature>().IsReadOnly);
            ExceptionDispatchInfo.Capture(exception).Throw();
        }, headers, endStream: false);
        await requestStream.SendDataAsync(new byte[6], endStream: false);
        await requestStream.SendDataAsync(new byte[6], endStream: false);
        await requestStream.SendDataAsync(new byte[6], endStream: false);

        var receivedHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();

        await requestStream.OnStreamCompletedTask.DefaultTimeout();
        Assert.Contains(LogMessages, m => m.Message.Contains("the application completed without reading the entire request body."));

        Assert.Equal(3, receivedHeaders.Count);
        Assert.Contains("date", receivedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("413", receivedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", receivedHeaders[HeaderNames.ContentLength]);

        Assert.NotNull(exception);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MaxRequestBodySize_AppCanRaiseLimit(bool includeContentLength)
    {
        _serviceContext.ServerOptions.Limits.MaxRequestBodySize = 10;
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        if (includeContentLength)
        {
            headers.Concat(new[]
                {
                    new KeyValuePair<string, string>(HeaderNames.ContentLength, "12"),
                });
        }
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            Assert.False(context.Features.Get<IHttpMaxRequestBodySizeFeature>().IsReadOnly);
            context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 12;
            var buffer = new byte[100];
            var read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(12, read);
            Assert.True(context.Features.Get<IHttpMaxRequestBodySizeFeature>().IsReadOnly);
            read = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(0, read);
        }, headers, endStream: false);
        await requestStream.SendDataAsync(new byte[12], endStream: true);

        var receivedHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();

        Assert.Equal(3, receivedHeaders.Count);
        Assert.Contains("date", receivedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", receivedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", receivedHeaders[HeaderNames.ContentLength]);
    }

    [Fact]
    public Task HEADERS_Received_RequestLineLength_Error()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, new string('A', 8192 / 2)),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/" + new string('A', 8192 / 2)),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http")
        };

        return HEADERS_Received_InvalidHeaderFields_StreamError(headers, CoreStrings.BadRequest_RequestLineTooLong, Http3ErrorCode.RequestRejected);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(int.MaxValue)]
    public async Task UnsupportedControlStreamType(int typeId)
    {
        await Http3Api.InitializeConnectionAsync(_noopApplication);

        var outboundControlStream = await Http3Api.CreateControlStream().DefaultTimeout();
        await outboundControlStream.SendSettingsAsync(new List<Http3PeerSetting>());

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        // Create unsupported control stream
        var invalidStream = await Http3Api.CreateControlStream(typeId).DefaultTimeout();
        await invalidStream.WaitForStreamErrorAsync(
            Http3ErrorCode.StreamCreationError,
            AssertExpectedErrorMessages,
            CoreStrings.FormatHttp3ControlStreamErrorUnsupportedType(typeId)).DefaultTimeout();

        // Connection is still alive and available for requests
        var requestStream = await Http3Api.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        }, endStream: true).DefaultTimeout();

        await requestStream.ExpectHeadersAsync().DefaultTimeout();
        await requestStream.ExpectReceiveEndOfStream().DefaultTimeout();
    }

    [Fact]
    public async Task HEADERS_ExceedsClientMaxFieldSectionSize_ErrorOnServer()
    {
        await Http3Api.InitializeConnectionAsync(context =>
        {
            context.Response.Headers["BigHeader"] = new string('!', 100);
            return Task.CompletedTask;
        });

        var outboundcontrolStream = await Http3Api.CreateControlStream();
        await outboundcontrolStream.SendSettingsAsync(new List<Http3PeerSetting>
            {
                new Http3PeerSetting(Core.Internal.Http3.Http3SettingType.MaxFieldSectionSize, 100)
            });

        var maxFieldSetting = await Http3Api.ServerReceivedSettingsReader.ReadAsync().DefaultTimeout();

        Assert.Equal(Core.Internal.Http3.Http3SettingType.MaxFieldSectionSize, maxFieldSetting.Key);
        Assert.Equal(100, maxFieldSetting.Value);

        var requestStream = await Http3Api.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        }, endStream: true).DefaultTimeout();

        await requestStream.WaitForStreamErrorAsync(
            Http3ErrorCode.InternalError,
            AssertExpectedErrorMessages,
            "The encoded HTTP headers length exceeds the limit specified by the peer of 100 bytes.");
    }

    [Fact]
    public async Task PostRequest_ServerReadsPartialAndFinishes_SendsBodyWithEndStream()
    {
        var startingTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            var buffer = new byte[1024];
            try
            {
                // Read 100 bytes
                var readCount = 0;
                while (readCount < 100)
                {
                    readCount += await context.Request.Body.ReadAsync(buffer.AsMemory(readCount, 100 - readCount));
                }

                await context.Response.Body.WriteAsync(buffer.AsMemory(0, 100));
                await clientTcs.Task.DefaultTimeout();
                appTcs.SetResult();
            }
            catch (Exception ex)
            {
                appTcs.SetException(ex);
            }
        }, new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        });

        var sourceData = new byte[1024];
        for (var i = 0; i < sourceData.Length; i++)
        {
            sourceData[i] = (byte)(i % byte.MaxValue);
        }

        await requestStream.SendDataAsync(sourceData);
        var decodedHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal(2, decodedHeaders.Count);
        Assert.Equal("200", decodedHeaders[InternalHeaderNames.Status]);

        var data = await requestStream.ExpectDataAsync();

        Assert.Equal(sourceData.AsMemory(0, 100).ToArray(), data.ToArray());

        clientTcs.SetResult();
        await appTcs.Task;

        await requestStream.ExpectReceiveEndOfStream();

        await requestStream.OnStreamCompletedTask.DefaultTimeout();

        Assert.Contains(LogMessages, m => m.Message.Contains("the application completed without reading the entire request body."));
        Assert.Equal("The application completed without reading the entire request body.", requestStream.AbortReadException.Message);
    }

    [Fact]
    public async Task HEADERS_WriteLargeResponseHeaderSection_Success()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var headerText = string.Create(6 * 1024, new object(), (chars, state) =>
        {
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)('0' + i % 10);
            }
        });

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(c =>
        {
            for (var i = 0; i < 10; i++)
            {
                c.Response.Headers["Header" + i] = i + "-" + headerText;
            }

            return Task.CompletedTask;
        }, headers);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i + "-" + headerText, responseHeaders["Header" + i]);
        }

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task HEADERS_WriteLargeResponseHeaderSectionTrailers_Success()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var headerText = string.Create(6 * 1024, new object(), (chars, state) =>
        {
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)('0' + i % 10);
            }
        });

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(c =>
        {
            for (var i = 0; i < 10; i++)
            {
                c.Response.AppendTrailer("Header" + i, i + "-" + headerText);
            }

            return Task.CompletedTask;
        }, headers);

        var responseHeaders = await requestStream.ExpectHeadersAsync();
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);

        var responseTrailers = await requestStream.ExpectHeadersAsync();
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i + "-" + headerText, responseTrailers["Header" + i]);
        }

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task HEADERS_NoResponseBody_RequestEndsOnHeaders()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(c =>
        {
            return Task.CompletedTask;
        }, headers);

        var responseHeaders = await requestStream.ExpectHeadersAsync(expectEnd: true);
        Assert.Equal("200", responseHeaders[InternalHeaderNames.Status]);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(4096)]
    [InlineData(8000)] // Greater than the default max pool size (4096)
    public async Task GetMemory_AfterAbort_GetsFakeMemory(int sizeHint)
    {
        var tcs = new TaskCompletionSource();
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        var requestStream = await Http3Api.InitializeConnectionAndStreamsAsync(async context =>
        {
            context.Abort();

            var memory = context.Response.BodyWriter.GetMemory(sizeHint);

            Assert.True(memory.Length >= sizeHint);
            await context.Response.CompleteAsync();
            context.Response.BodyWriter.Advance(memory.Length);
        }, headers);
    }
}
