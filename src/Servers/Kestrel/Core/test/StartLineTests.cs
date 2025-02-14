// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class StartLineTests : IDisposable
{
    private IDuplexPipe Transport { get; }
    private MemoryPool<byte> MemoryPool { get; }
    private Http1Connection Http1Connection { get; }
    private Http1ParsingHandler ParsingHandler { get; }
    private IHttpParser<Http1ParsingHandler> Parser { get; }

    [Fact]
    public void InOriginForm()
    {
        var rawTarget = "/path%20with%20spaces?q=123&w=xyzw1";
        var path = "/path with spaces";
        var query = "?q=123&w=xyzw1";
        Http1Connection.Reset();
        // RawTarget, Path, QueryString are null after reset
        Assert.Null(Http1Connection.RawTarget);
        Assert.Null(Http1Connection.Path);
        Assert.Null(Http1Connection.QueryString);

        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"POST {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // But not the same as the inputs.
        Assert.NotSame(rawTarget, Http1Connection.RawTarget);
        Assert.NotSame(path, Http1Connection.Path);
        Assert.NotSame(query, Http1Connection.QueryString);
    }

    [Fact]
    public void InAuthorityForm()
    {
        var rawTarget = "example.com:1234";
        var path = string.Empty;
        var query = string.Empty;
        Http1Connection.Reset();
        // RawTarget, Path, QueryString are null after reset
        Assert.Null(Http1Connection.RawTarget);
        Assert.Null(Http1Connection.Path);
        Assert.Null(Http1Connection.QueryString);

        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"CONNECT {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // But not the same as the inputs.
        Assert.NotSame(rawTarget, Http1Connection.RawTarget);
        // Empty strings, so interned and the same.
        Assert.Same(path, Http1Connection.Path);
        Assert.Same(query, Http1Connection.QueryString);
    }

    [Fact]
    public void InAbsoluteForm()
    {
        var rawTarget = "http://localhost/path1?q=123&w=xyzw";
        var path = "/path1";
        var query = "?q=123&w=xyzw";
        Http1Connection.Reset();
        // RawTarget, Path, QueryString are null after reset
        Assert.Null(Http1Connection.RawTarget);
        Assert.Null(Http1Connection.Path);
        Assert.Null(Http1Connection.QueryString);

        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"CONNECT {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // But not the same as the inputs.
        Assert.NotSame(rawTarget, Http1Connection.RawTarget);
        Assert.NotSame(path, Http1Connection.Path);
        Assert.NotSame(query, Http1Connection.QueryString);
    }

    [Fact]
    public void InAsteriskForm()
    {
        var rawTarget = "*";
        var path = string.Empty;
        var query = string.Empty;
        Http1Connection.Reset();
        // RawTarget, Path, QueryString are null after reset
        Assert.Null(Http1Connection.RawTarget);
        Assert.Null(Http1Connection.Path);
        Assert.Null(Http1Connection.QueryString);

        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"OPTIONS {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // Asterisk is interned string, so the same.
        Assert.Same(rawTarget, Http1Connection.RawTarget);
        // Empty strings, so interned and the same.
        Assert.Same(path, Http1Connection.Path);
        Assert.Same(query, Http1Connection.QueryString);
    }

    [Fact]
    public void DifferentFormsWorkTogether()
    {
        // InOriginForm
        var rawTarget = "/a%20path%20with%20spaces?q=123&w=xyzw12";
        var path = "/a path with spaces";
        var query = "?q=123&w=xyzw12";
        Http1Connection.Reset();
        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"POST {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // But not the same as the inputs.
        Assert.NotSame(rawTarget, Http1Connection.RawTarget);
        Assert.NotSame(path, Http1Connection.Path);
        Assert.NotSame(query, Http1Connection.QueryString);

        InAuthorityForm();

        InOriginForm();
        InAbsoluteForm();

        InOriginForm();
        InAsteriskForm();

        InAuthorityForm();
        InAsteriskForm();

        InAbsoluteForm();
        InAuthorityForm();

        InAbsoluteForm();
        InAsteriskForm();

        InAbsoluteForm();
        InAuthorityForm();
    }

    [Theory]
    [InlineData("/abs/path", "/abs/path", "")]
    [InlineData("/", "/", "")]
    [InlineData("/path", "/path", "")]
    [InlineData("/?q=123&w=xyz", "/", "?q=123&w=xyz")]
    [InlineData("/path?q=123&w=xyz", "/path", "?q=123&w=xyz")]
    [InlineData("/path%20with%20space?q=abc%20123", "/path with space", "?q=abc%20123")]
    public void OriginForms(string rawTarget, string path, string query)
    {
        Http1Connection.Reset();
        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"GET {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        var prevRequestUrl = Http1Connection.RawTarget;
        var prevPath = Http1Connection.Path;
        var prevQuery = Http1Connection.QueryString;

        // Identical requests keep same materialized string values
        for (var i = 0; i < 5; i++)
        {
            Http1Connection.Reset();
            // RawTarget, Path, QueryString are null after reset
            Assert.Null(Http1Connection.RawTarget);
            Assert.Null(Http1Connection.Path);
            Assert.Null(Http1Connection.QueryString);

            // Parser decodes % encoding in place, so we need to recreate the ROS
            ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"GET {rawTarget} HTTP/1.1\r\n"));
            reader = new SequenceReader<byte>(ros);
            Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

            // Equal the inputs.
            Assert.Equal(rawTarget, Http1Connection.RawTarget);
            Assert.Equal(path, Http1Connection.Path);
            Assert.Equal(query, Http1Connection.QueryString);

            // But not the same as the inputs.

            Assert.NotSame(rawTarget, Http1Connection.RawTarget);
            Assert.NotSame(path, Http1Connection.Path);
            // string.Empty is used for empty strings, so should be the same.
            Assert.True(query.Length == 0 || !ReferenceEquals(query, Http1Connection.QueryString));

            // However, materalized strings are reused if generated for previous requests.

            Assert.Same(prevRequestUrl, Http1Connection.RawTarget);
            Assert.Same(prevPath, Http1Connection.Path);
            Assert.Same(prevQuery, Http1Connection.QueryString);

            prevRequestUrl = Http1Connection.RawTarget;
            prevPath = Http1Connection.Path;
            prevQuery = Http1Connection.QueryString;
        }

        // Different OriginForm request changes values

        rawTarget = "/path1?q=123&w=xyzw";
        path = "/path1";
        query = "?q=123&w=xyzw";

        Http1Connection.Reset();
        ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"GET {rawTarget} HTTP/1.1\r\n"));
        reader = new SequenceReader<byte>(ros);
        Parser.ParseRequestLine(ParsingHandler, ref reader);

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // But not the same as the inputs.
        Assert.NotSame(rawTarget, Http1Connection.RawTarget);
        Assert.NotSame(path, Http1Connection.Path);
        Assert.NotSame(query, Http1Connection.QueryString);

        // Not equal previous request.
        Assert.NotEqual(prevRequestUrl, Http1Connection.RawTarget);
        Assert.NotEqual(prevPath, Http1Connection.Path);
        Assert.NotEqual(prevQuery, Http1Connection.QueryString);

        DifferentFormsWorkTogether();
    }

    [Theory]
    [InlineData("http://localhost/abs/path", "/abs/path", "")]
    [InlineData("https://localhost/abs/path", "/abs/path", "")] // handles mismatch scheme
    [InlineData("https://localhost:22/abs/path", "/abs/path", "")] // handles mismatched ports
    [InlineData("https://differenthost/abs/path", "/abs/path", "")] // handles mismatched hostname
    [InlineData("http://localhost/", "/", "")]
    [InlineData("http://root@contoso.com/path", "/path", "")]
    [InlineData("http://root:password@contoso.com/path", "/path", "")]
    [InlineData("https://localhost/", "/", "")]
    [InlineData("http://localhost", "/", "")]
    [InlineData("http://127.0.0.1/", "/", "")]
    [InlineData("http://[::1]/", "/", "")]
    [InlineData("http://[::1]:8080/", "/", "")]
    [InlineData("http://localhost?q=123&w=xyz", "/", "?q=123&w=xyz")]
    [InlineData("http://localhost/?q=123&w=xyz", "/", "?q=123&w=xyz")]
    [InlineData("http://localhost/path?q=123&w=xyz", "/path", "?q=123&w=xyz")]
    [InlineData("http://localhost/path%20with%20space?q=abc%20123", "/path with space", "?q=abc%20123")]
    public void AbsoluteForms(string rawTarget, string path, string query)
    {
        Http1Connection.Reset();
        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"GET {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        var prevRequestUrl = Http1Connection.RawTarget;
        var prevPath = Http1Connection.Path;
        var prevQuery = Http1Connection.QueryString;

        // Identical requests keep same materialized string values
        for (var i = 0; i < 5; i++)
        {
            Http1Connection.Reset();
            // RawTarget, Path, QueryString are null after reset
            Assert.Null(Http1Connection.RawTarget);
            Assert.Null(Http1Connection.Path);
            Assert.Null(Http1Connection.QueryString);

            ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"GET {rawTarget} HTTP/1.1\r\n"));
            reader = new SequenceReader<byte>(ros);
            Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

            // Equal the inputs.
            Assert.Equal(rawTarget, Http1Connection.RawTarget);
            Assert.Equal(path, Http1Connection.Path);
            Assert.Equal(query, Http1Connection.QueryString);

            // But not the same as the inputs.

            Assert.NotSame(rawTarget, Http1Connection.RawTarget);
            Assert.NotSame(path, Http1Connection.Path);
            // string.Empty is used for empty strings, so should be the same.
            Assert.True(query.Length == 0 || !ReferenceEquals(query, Http1Connection.QueryString));

            // However, materalized strings are reused if generated for previous requests.

            Assert.Same(prevRequestUrl, Http1Connection.RawTarget);
            Assert.Same(prevPath, Http1Connection.Path);
            Assert.Same(prevQuery, Http1Connection.QueryString);

            prevRequestUrl = Http1Connection.RawTarget;
            prevPath = Http1Connection.Path;
            prevQuery = Http1Connection.QueryString;
        }

        // Different Absolute Form request changes values

        rawTarget = "http://localhost/path1?q=123&w=xyzw";
        path = "/path1";
        query = "?q=123&w=xyzw";

        Http1Connection.Reset();
        ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"GET {rawTarget} HTTP/1.1\r\n"));
        reader = new SequenceReader<byte>(ros);
        Parser.ParseRequestLine(ParsingHandler, ref reader);

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // But not the same as the inputs.
        Assert.NotSame(rawTarget, Http1Connection.RawTarget);
        Assert.NotSame(path, Http1Connection.Path);
        Assert.NotSame(query, Http1Connection.QueryString);

        // Not equal previous request.
        Assert.NotEqual(prevRequestUrl, Http1Connection.RawTarget);
        Assert.NotEqual(prevPath, Http1Connection.Path);
        Assert.NotEqual(prevQuery, Http1Connection.QueryString);

        DifferentFormsWorkTogether();
    }

    [Fact]
    public void AsteriskForms()
    {
        var rawTarget = "*";
        var path = string.Empty;
        var query = string.Empty;

        Http1Connection.Reset();
        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"OPTIONS {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        var prevRequestUrl = Http1Connection.RawTarget;
        var prevPath = Http1Connection.Path;
        var prevQuery = Http1Connection.QueryString;

        // Identical requests keep same materialized string values
        for (var i = 0; i < 5; i++)
        {
            Http1Connection.Reset();
            // RawTarget, Path, QueryString are null after reset
            Assert.Null(Http1Connection.RawTarget);
            Assert.Null(Http1Connection.Path);
            Assert.Null(Http1Connection.QueryString);

            ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"OPTIONS {rawTarget} HTTP/1.1\r\n"));
            reader = new SequenceReader<byte>(ros);
            Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

            // Equal the inputs.
            Assert.Equal(rawTarget, Http1Connection.RawTarget);
            Assert.Equal(path, Http1Connection.Path);
            Assert.Equal(query, Http1Connection.QueryString);

            // Also same as the inputs (interned strings).

            Assert.Same(rawTarget, Http1Connection.RawTarget);
            Assert.Same(path, Http1Connection.Path);
            Assert.Same(query, Http1Connection.QueryString);

            // Materalized strings are reused if generated for previous requests.

            Assert.Same(prevRequestUrl, Http1Connection.RawTarget);
            Assert.Same(prevPath, Http1Connection.Path);
            Assert.Same(prevQuery, Http1Connection.QueryString);

            prevRequestUrl = Http1Connection.RawTarget;
            prevPath = Http1Connection.Path;
            prevQuery = Http1Connection.QueryString;
        }

        // Different request changes values (can't be Astrisk Form as all the same)
        rawTarget = "http://localhost/path1?q=123&w=xyzw";
        path = "/path1";
        query = "?q=123&w=xyzw";

        Http1Connection.Reset();
        ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"GET {rawTarget} HTTP/1.1\r\n"));
        reader = new SequenceReader<byte>(ros);
        Parser.ParseRequestLine(ParsingHandler, ref reader);

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // But not the same as the inputs.
        Assert.NotSame(rawTarget, Http1Connection.RawTarget);
        Assert.NotSame(path, Http1Connection.Path);
        Assert.NotSame(query, Http1Connection.QueryString);

        // Not equal previous request.
        Assert.NotEqual(prevRequestUrl, Http1Connection.RawTarget);
        Assert.NotEqual(prevPath, Http1Connection.Path);
        Assert.NotEqual(prevQuery, Http1Connection.QueryString);

        DifferentFormsWorkTogether();
    }

    [Theory]
    [InlineData("localhost", "", "")]
    [InlineData("localhost:22", "", "")] // handles mismatched ports
    [InlineData("differenthost", "", "")] // handles mismatched hostname
    [InlineData("different-host", "", "")]
    [InlineData("127.0.0.1", "", "")]
    [InlineData("[::1]", "", "")]
    [InlineData("[::1]:8080", "", "")]
    public void AuthorityForms(string rawTarget, string path, string query)
    {
        Http1Connection.Reset();
        var ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"CONNECT {rawTarget} HTTP/1.1\r\n"));
        var reader = new SequenceReader<byte>(ros);
        Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

        var prevRequestUrl = Http1Connection.RawTarget;
        var prevPath = Http1Connection.Path;
        var prevQuery = Http1Connection.QueryString;

        // Identical requests keep same materialized string values
        for (var i = 0; i < 5; i++)
        {
            Http1Connection.Reset();
            // RawTarget, Path, QueryString are null after reset
            Assert.Null(Http1Connection.RawTarget);
            Assert.Null(Http1Connection.Path);
            Assert.Null(Http1Connection.QueryString);

            ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"CONNECT {rawTarget} HTTP/1.1\r\n"));
            reader = new SequenceReader<byte>(ros);
            Assert.True(Parser.ParseRequestLine(ParsingHandler, ref reader));

            // Equal the inputs.
            Assert.Equal(rawTarget, Http1Connection.RawTarget);
            Assert.Equal(path, Http1Connection.Path);
            Assert.Equal(query, Http1Connection.QueryString);

            // RawTarget not the same as the input.
            Assert.NotSame(rawTarget, Http1Connection.RawTarget);
            // Others same as the inputs, empty strings.
            Assert.Same(path, Http1Connection.Path);
            Assert.Same(query, Http1Connection.QueryString);

            // However, materalized strings are reused if generated for previous requests.

            Assert.Same(prevRequestUrl, Http1Connection.RawTarget);
            Assert.Same(prevPath, Http1Connection.Path);
            Assert.Same(prevQuery, Http1Connection.QueryString);

            prevRequestUrl = Http1Connection.RawTarget;
            prevPath = Http1Connection.Path;
            prevQuery = Http1Connection.QueryString;
        }

        // Different Authority Form request changes values
        rawTarget = "example.org:2345";
        path = "";
        query = "";

        Http1Connection.Reset();
        ros = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes($"CONNECT {rawTarget} HTTP/1.1\r\n"));
        reader = new SequenceReader<byte>(ros);
        Parser.ParseRequestLine(ParsingHandler, ref reader);

        // Equal the inputs.
        Assert.Equal(rawTarget, Http1Connection.RawTarget);
        Assert.Equal(path, Http1Connection.Path);
        Assert.Equal(query, Http1Connection.QueryString);

        // But not the same as the inputs.
        Assert.NotSame(rawTarget, Http1Connection.RawTarget);
        // Empty interned strings
        Assert.Same(path, Http1Connection.Path);
        Assert.Same(query, Http1Connection.QueryString);

        // Not equal previous request.
        Assert.NotEqual(prevRequestUrl, Http1Connection.RawTarget);

        DifferentFormsWorkTogether();
    }

    public StartLineTests()
    {
        MemoryPool = PinnedBlockMemoryPoolFactory.Create();
        var options = new PipeOptions(MemoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);
        Transport = pair.Transport;

        var timeProvider = new FakeTimeProvider();
        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            timeProvider: timeProvider,
            httpParser: new HttpParser<Http1ParsingHandler>());

        var connectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: serviceContext,
            connectionContext: Mock.Of<ConnectionContext>(),
            transport: Transport,
            timeoutControl: new TimeoutControl(timeoutHandler: null, timeProvider),
            memoryPool: MemoryPool,
            connectionFeatures: new FeatureCollection());

        Http1Connection = new Http1Connection(connectionContext);

        Parser = new HttpParser<Http1ParsingHandler>(showErrorDetails: true);
        ParsingHandler = new Http1ParsingHandler(Http1Connection);
    }

    public void Dispose()
    {
        Transport.Input.Complete();
        Transport.Output.Complete();

        MemoryPool.Dispose();
    }
}
