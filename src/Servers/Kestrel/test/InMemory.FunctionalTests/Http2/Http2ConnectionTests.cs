// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.HPack;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http2ConnectionTests : Http2TestBase
{
    [Fact]
    public async Task MaxConcurrentStreamsLogging_ReachLimit_MessageLogged()
    {
        await InitializeConnectionAsync(_echoApplication);

        _connection.ServerSettings.MaxConcurrentStreams = 2;

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        Assert.Equal(0, LogMessages.Count(m => m.EventId.Name == "Http2MaxConcurrentStreamsReached"));

        // Log message because we've reached the stream limit
        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);
        Assert.Equal(1, LogMessages.Count(m => m.EventId.Name == "Http2MaxConcurrentStreamsReached"));

        // This stream will error because it exceeds max concurrent streams
        await StartStreamAsync(5, _browserRequestHeaders, endStream: true);
        await WaitForStreamErrorAsync(5, Http2ErrorCode.REFUSED_STREAM, CoreStrings.Http2ErrorMaxStreams);
        Assert.Equal(1, LogMessages.Count(m => m.EventId.Name == "Http2MaxConcurrentStreamsReached"));

        await StopConnectionAsync(expectedLastStreamId: 5, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task FlowControl_NoAvailability_ResponseHeadersStillFlushed()
    {
        _clientSettings.InitialWindowSize = 0;

        await InitializeConnectionAsync(c =>
        {
            return c.Response.Body.WriteAsync(new byte[1]).AsTask();
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        await SendWindowUpdateAsync(streamId: 1, 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task FlowControl_OneStream_CorrectlyAwaited()
    {
        await InitializeConnectionAsync(async c =>
        {
            // Send headers
            await c.Response.Body.FlushAsync();

            // Send large data (1 larger than window size)
            await c.Response.Body.WriteAsync(new byte[65540]);
        });

        // Ensure the connection window size is large enough
        await SendWindowUpdateAsync(streamId: 0, 65537);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16384,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16384,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16384,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16383,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // 2 bytes remaining

        await SendWindowUpdateAsync(streamId: 1, 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendWindowUpdateAsync(streamId: 1, 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendWindowUpdateAsync(streamId: 1, 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendWindowUpdateAsync(streamId: 1, 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendWindowUpdateAsync(streamId: 1, 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task RequestHeaderStringReuse_MultipleStreams_KnownHeaderReused()
    {
        var requestHeaders = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/hello"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>(HeaderNames.ContentType, "application/json")
        };

        await InitializeConnectionAsync(_readHeadersApplication);

        await StartStreamAsync(1, requestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        var contentType1 = _receivedHeaders["Content-Type"];
        var authority1 = _receivedRequestFields.Authority;
        var path1 = _receivedRequestFields.Path;

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);

        await StartStreamAsync(3, requestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        var contentType2 = _receivedHeaders["Content-Type"];
        var authority2 = _receivedRequestFields.Authority;
        var path2 = _receivedRequestFields.Path;

        Assert.Same(contentType1, contentType2);
        Assert.Same(authority1, authority2);
        Assert.Same(path1, path2);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task RequestHeaderStringReuse_MultipleStreams_KnownHeaderClearedIfNotReused()
    {
        const BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        var requestHeaders1 = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/hello"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>(HeaderNames.ContentType, "application/json")
        };

        // Note: No content-type
        var requestHeaders2 = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/hello"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80")
        };

        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(1, requestHeaders1, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);
        Assert.True(_connection.StreamPool.TryPeek(out var stream1));

        // Hacky but required because header references is private.
        var headerReferences1 = typeof(HttpRequestHeaders).GetField("_headers", privateFlags).GetValue(stream1.RequestHeaders);
        var contentTypeValue1 = (StringValues)headerReferences1.GetType().GetField("_ContentType").GetValue(headerReferences1);

        await StartStreamAsync(3, requestHeaders2, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);
        Assert.True(_connection.StreamPool.TryPeek(out var stream2));

        // Hacky but required because header references is private.
        var headerReferences2 = typeof(HttpRequestHeaders).GetField("_headers", privateFlags).GetValue(stream2.RequestHeaders);
        var contentTypeValue2 = (StringValues)headerReferences2.GetType().GetField("_ContentType").GetValue(headerReferences2);

        Assert.Equal("application/json", contentTypeValue1);
        Assert.Equal(StringValues.Empty, contentTypeValue2);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    private class ResponseTrailersWrapper : IHeaderDictionary
    {
        readonly IHeaderDictionary _innerHeaders;

        public ResponseTrailersWrapper(IHeaderDictionary headers)
        {
            _innerHeaders = headers;
        }

        public StringValues this[string key] { get => _innerHeaders[key]; set => _innerHeaders[key] = value; }
        public long? ContentLength { get => _innerHeaders.ContentLength; set => _innerHeaders.ContentLength = value; }
        public ICollection<string> Keys => _innerHeaders.Keys;
        public ICollection<StringValues> Values => _innerHeaders.Values;
        public int Count => _innerHeaders.Count;
        public bool IsReadOnly => _innerHeaders.IsReadOnly;
        public void Add(string key, StringValues value) => _innerHeaders.Add(key, value);
        public void Add(KeyValuePair<string, StringValues> item) => _innerHeaders.Add(item);
        public void Clear() => _innerHeaders.Clear();
        public bool Contains(KeyValuePair<string, StringValues> item) => _innerHeaders.Contains(item);
        public bool ContainsKey(string key) => _innerHeaders.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex) => _innerHeaders.CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => _innerHeaders.GetEnumerator();
        public bool Remove(string key) => _innerHeaders.Remove(key);
        public bool Remove(KeyValuePair<string, StringValues> item) => _innerHeaders.Remove(item);
        public bool TryGetValue(string key, out StringValues value) => _innerHeaders.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => _innerHeaders.GetEnumerator();
    }

    [Fact]
    public async Task ResponseTrailers_MultipleStreams_Reset()
    {
        var requestHeaders = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/hello"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>(HeaderNames.ContentType, "application/json")
        };

        var requestCount = 0;
        IHeaderDictionary trailersFirst = null;
        IHeaderDictionary trailersLast = null;
        await InitializeConnectionAsync(context =>
        {
            requestCount++;

            var trailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();
            if (requestCount == 1)
            {
                trailersFirst = new ResponseTrailersWrapper(trailersFeature.Trailers);
                trailersFeature.Trailers = trailersFirst;
            }
            else
            {
                trailersLast = trailersFeature.Trailers;
            }
            trailersFeature.Trailers["trailer-" + requestCount] = "true";
            return Task.CompletedTask;
        });

        await StartStreamAsync(1, requestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);

        var trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 16,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

        Assert.Single(_decodedHeaders);
        Assert.Equal("true", _decodedHeaders["trailer-1"]);

        _decodedHeaders.Clear();

        for (int i = 1; i < 3; i++)
        {
            int streamId = i * 2 + 1;
            // TriggerTick will trigger the stream to be returned to the pool so we can assert it
            TriggerTick();

            // Stream has been returned to the pool
            Assert.Equal(1, _connection.StreamPool.Count);

            await StartStreamAsync(streamId, requestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 6,
                withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
                withStreamId: streamId);

            trailersFrame = await ExpectAsync(Http2FrameType.HEADERS,
                withLength: 16,
                withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
                withStreamId: streamId);

            _hpackDecoder.Decode(trailersFrame.PayloadSequence, endHeaders: true, handler: this);

            Assert.Single(_decodedHeaders);
            Assert.Equal("true", _decodedHeaders[$"trailer-{i + 1}"]);

            _decodedHeaders.Clear();

        }

        Assert.NotNull(trailersFirst);
        Assert.NotNull(trailersLast);
        Assert.NotSame(trailersFirst, trailersLast);

        await StopConnectionAsync(expectedLastStreamId: 5, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task StreamPool_SingleStream_ReturnedToPool()
    {
        // Add stream to Http2Connection._completedStreams inline with SetResult().
        var serverTcs = new TaskCompletionSource();

        await InitializeConnectionAsync(async context =>
        {
            await serverTcs.Task;
            await _echoApplication(context);
        });

        Assert.Equal(0, _connection.StreamPool.Count);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var stream = _connection._streams[1];
        serverTcs.SetResult();

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/39477")]
    public async Task StreamPool_MultipleStreamsConcurrent_StreamsReturnedToPool()
    {
        await InitializeConnectionAsync(_echoApplication);

        Assert.Equal(0, _connection.StreamPool.Count);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);

        await SendDataAsync(1, _helloBytes, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await SendDataAsync(3, _helloBytes, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Streams have been returned to the pool
        Assert.Equal(2, _connection.StreamPool.Count);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task StreamPool_MultipleStreamsInSequence_PooledStreamReused()
    {
        TaskCompletionSource appDelegateTcs = null;
        object persistedState = null;
        var requestCount = 0;

        await InitializeConnectionAsync(async context =>
        {
            requestCount++;
            var persistentStateCollection = context.Features.Get<IPersistentStateFeature>().State;
            if (persistentStateCollection.TryGetValue("Counter", out var value))
            {
                persistedState = value;
            }
            persistentStateCollection["Counter"] = requestCount;
            await appDelegateTcs.Task;
        });

        Assert.Equal(0, _connection.StreamPool.Count);

        // Add stream to Http2Connection._completedStreams inline with SetResult().
        appDelegateTcs = new TaskCompletionSource();
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        // Get the in progress stream
        var stream = _connection._streams[1];

        appDelegateTcs.TrySetResult();

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);
        Assert.True(_connection.StreamPool.TryPeek(out var pooledStream));
        Assert.Equal(stream, pooledStream);

        // First request has no persisted state
        Assert.Null(persistedState);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        // Add stream to Http2Connection._completedStreams inline with SetResult().
        appDelegateTcs = new TaskCompletionSource();
        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        // New stream has been taken from the pool
        Assert.Equal(0, _connection.StreamPool.Count);

        appDelegateTcs.TrySetResult();

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream was reused and returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);
        Assert.True(_connection.StreamPool.TryPeek(out pooledStream));
        Assert.Equal(stream, pooledStream);

        // State persisted on first request was available on the second request
        Assert.Equal(1, (int)persistedState);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/40626")]
    public async Task StreamPool_StreamIsInvalidState_DontReturnedToPool()
    {
        // Add (or don't add) stream to Http2Connection._completedStreams inline with SetResult().
        var serverTcs = new TaskCompletionSource();

        await InitializeConnectionAsync(async context =>
        {
            await serverTcs.Task.DefaultTimeout();

            await context.Response.WriteAsync("Content");
            throw new InvalidOperationException("Put the stream into an invalid state by throwing after writing to response.");
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var stream = _connection._streams[1];
        serverTcs.SetResult();

        // Wait for the stream to be completed
        await WaitForStreamAsync(stream.StreamId).DefaultTimeout();

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        var output = (Http2OutputProducer)stream.Output;
        Assert.True(output._disposed);

        // Stream is not returned to the pool
        Assert.Equal(0, _connection.StreamPool.Count);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 7,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, null);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task StreamPool_EndedStreamErrorsOnStart_NotReturnedToPool()
    {
        await InitializeConnectionAsync(_echoApplication);

        _connection.ServerSettings.MaxConcurrentStreams = 1;

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        // This stream will error because it exceeds max concurrent streams
        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);
        await WaitForStreamErrorAsync(3, Http2ErrorCode.REFUSED_STREAM, CoreStrings.Http2ErrorMaxStreams);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream not returned to the pool
        Assert.Equal(0, _connection.StreamPool.Count);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task StreamPool_UnendedStreamErrorsOnStart_NotReturnedToPool()
    {
        _serviceContext.ServerOptions.Limits.MinRequestBodyDataRate = null;

        await InitializeConnectionAsync(_echoApplication);

        _connection.ServerSettings.MaxConcurrentStreams = 1;

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        // This stream will error because it exceeds max concurrent streams
        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);
        await WaitForStreamErrorAsync(3, Http2ErrorCode.REFUSED_STREAM, CoreStrings.Http2ErrorMaxStreams);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        AdvanceTime(TimeSpan.FromTicks(Constants.RequestBodyDrainTimeout.Ticks + 1));

        // TriggerTick will trigger the stream to attempt to be returned to the pool
        TriggerTick();

        // Drain timeout has past but the stream was not returned because it is unfinished
        Assert.Equal(0, _connection.StreamPool.Count);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task StreamPool_UnusedExpiredStream_RemovedFromPool()
    {
        await InitializeConnectionAsync(async context =>
        {
            await _echoApplication(context);
        });

        Assert.Equal(0, _connection.StreamPool.Count);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        // TriggerTick will trigger the stream to be returned to the pool so we can assert it
        TriggerTick();

        // Stream has been returned to the pool
        Assert.Equal(1, _connection.StreamPool.Count);

        _connection.StreamPool.TryPeek(out var pooledStream);

        AdvanceTime(TimeSpan.FromSeconds(1));

        // Stream has not expired and is still in pool
        Assert.Equal(1, _connection.StreamPool.Count);

        AdvanceTime(TimeSpan.FromSeconds(6));

        // Stream has expired and has been removed from pool
        Assert.Equal(0, _connection.StreamPool.Count);

        // Removed stream should have been disposed
        Assert.True(((Http2OutputProducer)pooledStream.Output)._disposed);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task Frame_Received_OverMaxSize_FrameError()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        uint length = Http2PeerSettings.MinAllowedMaxFrameSize + 1;
        await SendDataAsync(1, new byte[length], endStream: true);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorFrameOverLimit(length, Http2PeerSettings.MinAllowedMaxFrameSize));
        AssertConnectionEndReason(ConnectionEndReason.MaxFrameLengthExceeded);
    }

    [Fact]
    public async Task ServerSettings_ChangesRequestMaxFrameSize()
    {
        var length = Http2PeerSettings.MinAllowedMaxFrameSize + 10;
        _serviceContext.ServerOptions.Limits.Http2.MaxFrameSize = length;

        await InitializeConnectionAsync(_echoApplication, expectedSettingsCount: 5);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, new byte[length], endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        // The client's settings is still defaulted to Http2PeerSettings.MinAllowedMaxFrameSize so the echo response will come back in two separate frames
        await ExpectAsync(Http2FrameType.DATA,
            withLength: Http2PeerSettings.MinAllowedMaxFrameSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: length - Http2PeerSettings.MinAllowedMaxFrameSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task DATA_Received_ReadByStream()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _helloWorldBytes, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        Assert.True(_helloWorldBytes.AsSpan().SequenceEqual(dataFrame.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task DATA_Received_MaxSize_ReadByStream()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _maxData, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: _maxData.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        Assert.True(_maxData.AsSpan().SequenceEqual(dataFrame.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task DATA_Received_GreaterThanInitialWindowSize_ReadByStream()
    {
        var initialStreamWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialStreamWindowSize;
        var framesInStreamWindow = initialStreamWindowSize / Http2PeerSettings.DefaultMaxFrameSize;
        var initialConnectionWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
        var framesInConnectionWindow = initialConnectionWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

        // Grow the client stream windows so no stream WINDOW_UPDATEs need to be sent.
        _clientSettings.InitialWindowSize = int.MaxValue;

        await InitializeConnectionAsync(_echoApplication);

        // Grow the client connection windows so no connection WINDOW_UPDATEs need to be sent.
        await SendWindowUpdateAsync(0, int.MaxValue - (int)Http2PeerSettings.DefaultInitialWindowSize);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        // Rounds down so we don't go over the half window size and trigger an update
        for (var i = 0; i < framesInStreamWindow / 2; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        var dataFrames = new List<Http2FrameWithPayload>();

        for (var i = 0; i < framesInStreamWindow / 2; i++)
        {
            var dataFrame1 = await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
            dataFrames.Add(dataFrame1);
        }

        // Writing over half the initial window size induces both a connection-level and stream-level window update.
        await SendDataAsync(1, _maxData, endStream: false);

        var streamWindowUpdateFrame1 = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        var dataFrame2 = await ExpectAsync(Http2FrameType.DATA,
            withLength: _maxData.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        dataFrames.Add(dataFrame2);

        // Write a few more frames to get close to the connection window threshold
        var additionalFrames = (framesInConnectionWindow / 2) - (framesInStreamWindow / 2) - 1;
        for (var i = 0; i < additionalFrames; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);

            var dataFrame1 = await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
            dataFrames.Add(dataFrame1);
        }

        // Write one more to cross the connection window update threshold
        await SendDataAsync(1, _maxData, endStream: false);

        var connectionWindowUpdateFrame1 = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 0);

        var dataFrame3 = await ExpectAsync(Http2FrameType.DATA,
            withLength: _maxData.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        dataFrames.Add(dataFrame3);

        // End
        await SendDataAsync(1, new Memory<byte>(), endStream: true);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        foreach (var frame in dataFrames)
        {
            Assert.True(_maxData.AsSpan().SequenceEqual(frame.PayloadSequence.ToArray()));
        }
        var updateSize = ((framesInStreamWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, streamWindowUpdateFrame1.WindowUpdateSizeIncrement);
        updateSize = ((framesInConnectionWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, connectionWindowUpdateFrame1.WindowUpdateSizeIncrement);
    }

    [Fact]
    public async Task DATA_Received_RightAtWindowLimit_DoesNotPausePipe()
    {
        var initialStreamWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialStreamWindowSize;
        var framesInStreamWindow = initialStreamWindowSize / Http2PeerSettings.DefaultMaxFrameSize;
        var initialConnectionWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
        var framesInConnectionWindow = initialConnectionWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        // Rounds down so we don't go over the limit
        for (var i = 0; i < framesInStreamWindow; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }

        var remainder = initialStreamWindowSize % (int)Http2PeerSettings.DefaultMaxFrameSize;

        // Write just to the limit.
        // This should not produce a async task from the request body pipe. See the Debug.Assert in Http2Stream.OnDataAsync
        await SendDataAsync(1, new Memory<byte>(_maxData, 0, remainder), endStream: false);

        // End
        await SendDataAsync(1, new Memory<byte>(), endStream: true);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task DATA_Received_Multiple_ReadByStream()
    {
        await InitializeConnectionAsync(_bufferingApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        for (var i = 0; i < _helloWorldBytes.Length; i++)
        {
            await SendDataAsync(1, new ArraySegment<byte>(_helloWorldBytes, i, 1), endStream: false);
        }

        await SendDataAsync(1, _noData, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        Assert.True(_helloWorldBytes.AsSpan().SequenceEqual(dataFrame.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task DATA_Received_Multiplexed_ReadByStreams()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);

        await SendDataAsync(1, _helloBytes, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var stream1DataFrame1 = await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendDataAsync(3, _helloBytes, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        var stream3DataFrame1 = await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);

        await SendDataAsync(3, _worldBytes, endStream: false);

        var stream3DataFrame2 = await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);

        await SendDataAsync(1, _worldBytes, endStream: false);

        var stream1DataFrame2 = await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendDataAsync(1, _noData, endStream: true);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await SendDataAsync(3, _noData, endStream: true);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        Assert.True(_helloBytes.AsSpan().SequenceEqual(stream1DataFrame1.PayloadSequence.ToArray()));
        Assert.True(_worldBytes.AsSpan().SequenceEqual(stream1DataFrame2.PayloadSequence.ToArray()));
        Assert.True(_helloBytes.AsSpan().SequenceEqual(stream3DataFrame1.PayloadSequence.ToArray()));
        Assert.True(_worldBytes.AsSpan().SequenceEqual(stream3DataFrame2.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task DATA_Received_Multiplexed_GreaterThanInitialWindowSize_ReadByStream()
    {
        var initialStreamWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialStreamWindowSize;
        var initialConnectionWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
        var framesInStreamWindow = initialStreamWindowSize / Http2PeerSettings.DefaultMaxFrameSize;
        var framesInConnectionWindow = initialConnectionWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

        // Grow the client stream windows so no stream WINDOW_UPDATEs need to be sent.
        _clientSettings.InitialWindowSize = int.MaxValue;

        await InitializeConnectionAsync(_echoApplication);

        // Grow the client connection windows so no connection WINDOW_UPDATEs need to be sent.
        await SendWindowUpdateAsync(0, int.MaxValue - (int)Http2PeerSettings.DefaultInitialWindowSize);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        // Rounds down so we don't go over the half window size and trigger an update
        for (var i = 0; i < framesInStreamWindow / 2; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        var dataFrames = new List<Http2FrameWithPayload>();

        for (var i = 0; i < framesInStreamWindow / 2; i++)
        {
            var dataFrame1 = await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
            dataFrames.Add(dataFrame1);
        }

        // Writing over half the initial window size induces a stream-level window update.
        await SendDataAsync(1, _maxData, endStream: false);

        var streamWindowUpdateFrame = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        var dataFrame2 = await ExpectAsync(Http2FrameType.DATA,
            withLength: _maxData.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        dataFrames.Add(dataFrame2);

        // No update expected for these
        var additionalFrames = (framesInConnectionWindow / 2) - (framesInStreamWindow / 2) - 1;
        for (var i = 0; i < additionalFrames; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);

            var dataFrame3 = await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
            dataFrames.Add(dataFrame3);
        }

        // Uploading data to a new stream induces a second connection-level but not stream-level window update.
        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);
        await SendDataAsync(3, _maxData, endStream: true);

        var connectionWindowUpdateFrame = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 0);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);

        var dataFrame4 = await ExpectAsync(Http2FrameType.DATA,
            withLength: _maxData.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);
        dataFrames.Add(dataFrame4);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        // Would trigger a stream window update, except it's the last frame.
        await SendDataAsync(1, _maxData, endStream: true);

        var dataFrame5 = await ExpectAsync(Http2FrameType.DATA,
            withLength: _maxData.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        dataFrames.Add(dataFrame5);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        foreach (var frame in dataFrames)
        {
            Assert.True(_maxData.AsSpan().SequenceEqual(frame.PayloadSequence.ToArray()));
        }
        var updateSize = ((framesInStreamWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, streamWindowUpdateFrame.WindowUpdateSizeIncrement);
        updateSize = ((framesInConnectionWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, connectionWindowUpdateFrame.WindowUpdateSizeIncrement);
    }

    [Fact]
    public async Task DATA_Received_Multiplexed_AppMustNotBlockOtherFrames()
    {
        var stream1Read = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream1ReadFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream3Read = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stream3ReadFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await InitializeConnectionAsync(async context =>
        {
            var data = new byte[10];
            var read = await context.Request.Body.ReadAsync(new byte[10], 0, 10);
            if (context.Features.Get<IHttp2StreamIdFeature>().StreamId == 1)
            {
                stream1Read.TrySetResult();

                await stream1ReadFinished.Task.DefaultTimeout();
            }
            else
            {
                stream3Read.TrySetResult();

                await stream3ReadFinished.Task.DefaultTimeout();
            }
            await context.Response.Body.WriteAsync(data, 0, read);
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);

        await SendDataAsync(1, _helloBytes, endStream: true);
        await stream1Read.Task.DefaultTimeout();

        await SendDataAsync(3, _helloBytes, endStream: true);
        await stream3Read.Task.DefaultTimeout();

        stream3ReadFinished.TrySetResult();

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        stream1ReadFinished.TrySetResult();

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public async Task DATA_Received_WithPadding_ReadByStream(byte padLength)
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataWithPaddingAsync(1, _helloWorldBytes, padLength, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        var dataFrame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 12,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        Assert.True(_helloWorldBytes.AsSpan().SequenceEqual(dataFrame.PayloadSequence.ToArray()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public async Task DATA_Received_WithPadding_CountsTowardsInputFlowControl(byte padLength)
    {
        var initialWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialStreamWindowSize;
        var framesInWindow = initialWindowSize / Http2PeerSettings.DefaultMaxFrameSize;
        var maxDataMinusPadding = _maxData.AsMemory(0, _maxData.Length - padLength - 1);

        // Grow the client stream windows so no stream WINDOW_UPDATEs need to be sent.
        _clientSettings.InitialWindowSize = int.MaxValue;

        await InitializeConnectionAsync(_echoApplication);

        // Grow the client connection windows so no connection WINDOW_UPDATEs need to be sent.
        await SendWindowUpdateAsync(0, int.MaxValue - (int)Http2PeerSettings.DefaultInitialWindowSize);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        var dataSent = 0;
        // Rounds down so we don't go over the half window size and trigger an update
        for (var i = 0; i < framesInWindow / 2; i++)
        {
            await SendDataWithPaddingAsync(1, maxDataMinusPadding, padLength, endStream: false);
            dataSent += maxDataMinusPadding.Length;
        }

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        // The frames come back in various sizes depending on the pipe buffers, and without the padding we sent.
        while (dataSent > 0)
        {
            var frame = await ReceiveFrameAsync();
            Assert.Equal(Http2FrameType.DATA, frame.Type);
            Assert.True(dataSent >= frame.PayloadLength);
            Assert.Equal(Http2DataFrameFlags.NONE, frame.DataFlags);
            Assert.Equal(1, frame.StreamId);

            dataSent -= frame.PayloadLength;
        }

        // Writing over half the initial window size induces a stream-level window update.
        await SendDataAsync(1, _maxData, endStream: false);

        var connectionWindowUpdateFrame = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        var dataFrame3 = await ExpectAsync(Http2FrameType.DATA,
            withLength: _maxData.Length,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendDataAsync(1, new Memory<byte>(), endStream: true);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        Assert.True(_maxData.AsSpan().SequenceEqual(dataFrame3.PayloadSequence.ToArray()));

        var updateSize = ((framesInWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, connectionWindowUpdateFrame.WindowUpdateSizeIncrement);
    }

    [Fact]
    public async Task DATA_Received_ButNotConsumedByApp_CountsTowardsInputFlowControl()
    {
        var initialConnectionWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
        var framesConnectionInWindow = initialConnectionWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        for (var i = 0; i < framesConnectionInWindow / 2; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await WaitForStreamErrorAsync(expectedStreamId: 1, Http2ErrorCode.NO_ERROR, null);
        // Logged without an exception.
        Assert.Contains(LogMessages, m => m.Message.Contains("the application completed without reading the entire request body."));

        // Writing over half the initial window size induces a connection-level window update.
        // But no stream window update since this is the last frame.
        await SendDataAsync(1, _maxData, endStream: true);

        var connectionWindowUpdateFrame = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 0);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        var updateSize = ((framesConnectionInWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, connectionWindowUpdateFrame.WindowUpdateSizeIncrement);
    }

    [Fact]
    public async Task DATA_BufferRequestBodyLargerThanStreamSizeSmallerThanConnectionPipe_Works()
    {
        // This test was not written to allow for arbitrary initial stream sizes. It was tuned to the old defaults.
        _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize = 128 * 1024;
        _serviceContext.ServerOptions.Limits.Http2.InitialStreamWindowSize = 96 * 1024;

        var initialStreamWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialStreamWindowSize;
        var framesInStreamWindow = initialStreamWindowSize / Http2PeerSettings.DefaultMaxFrameSize;
        var initialConnectionWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
        var framesInConnectionWindow = initialConnectionWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

        // Grow the client stream windows so no stream WINDOW_UPDATEs need to be sent.
        _clientSettings.InitialWindowSize = int.MaxValue;

        await InitializeConnectionAsync(async context =>
        {
            await context.Response.BodyWriter.FlushAsync();
            var readResult = await context.Request.BodyReader.ReadAsync();
            while (readResult.Buffer.Length != _maxData.Length * 4)
            {
                context.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
                readResult = await context.Request.BodyReader.ReadAsync();
            }

            context.Request.BodyReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            readResult = await context.Request.BodyReader.ReadAsync();
            Assert.Equal(readResult.Buffer.Length, _maxData.Length * 5);

            await context.Response.BodyWriter.WriteAsync(readResult.Buffer.ToArray());

            context.Request.BodyReader.AdvanceTo(readResult.Buffer.End);
        });

        // Grow the client connection windows so no connection WINDOW_UPDATEs need to be sent.
        await SendWindowUpdateAsync(0, int.MaxValue - (int)Http2PeerSettings.DefaultInitialWindowSize);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        // Rounds down so we don't go over the half window size and trigger an update
        for (var i = 0; i < framesInStreamWindow / 2; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }

        // trip over the update size.
        await SendDataAsync(1, _maxData, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        var dataFrames = new List<Http2FrameWithPayload>();

        var streamWindowUpdateFrame1 = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Writing over half the initial window size induces both a connection-level and stream-level window update.

        await SendDataAsync(1, _maxData, endStream: true);

        for (var i = 0; i < framesInStreamWindow / 2 + 2; i++)
        {
            var dataFrame3 = await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
            dataFrames.Add(dataFrame3);
        }

        var connectionWindowUpdateFrame = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 0);
        // End

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();

        foreach (var frame in dataFrames)
        {
            Assert.True(_maxData.AsSpan().SequenceEqual(frame.PayloadSequence.ToArray()));
        }

        var updateSize = ((framesInStreamWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, streamWindowUpdateFrame1.WindowUpdateSizeIncrement);
        updateSize = ((framesInConnectionWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, connectionWindowUpdateFrame.WindowUpdateSizeIncrement);
    }

    [Fact]
    public async Task DATA_Received_StreamIdZero_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendDataAsync(0, _noData, endStream: false);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.DATA));
        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task DATA_Received_StreamIdEven_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendDataAsync(2, _noData, endStream: false);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.DATA, streamId: 2));
        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task DATA_Received_PaddingEqualToFramePayloadLength_ConnectionError()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendInvalidDataFrameAsync(1, frameLength: 5, padLength: 5);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.DATA));
        AssertConnectionEndReason(ConnectionEndReason.InvalidDataPadding);
    }

    [Fact]
    public async Task DATA_Received_PaddingGreaterThanFramePayloadLength_ConnectionError()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendInvalidDataFrameAsync(1, frameLength: 5, padLength: 6);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.DATA));
        AssertConnectionEndReason(ConnectionEndReason.InvalidDataPadding);
    }

    [Fact]
    public async Task DATA_Received_FrameLengthZeroPaddingZero_ConnectionError()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendInvalidDataFrameAsync(1, frameLength: 0, padLength: 0);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.DATA, expectedLength: 1));
        AssertConnectionEndReason(ConnectionEndReason.InvalidFrameLength);
    }

    [Fact]
    public async Task DATA_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendDataAsync(1, _helloWorldBytes, endStream: true);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.DATA, streamId: 1, headersStreamId: 1));
        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Fact]
    public async Task DATA_Received_StreamIdle_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendDataAsync(1, _helloWorldBytes, endStream: false);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdle(Http2FrameType.DATA, streamId: 1));
        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task DATA_Received_StreamHalfClosedRemote_ConnectionError()
    {
        // Use _waitForAbortApplication so we know the stream will still be active when we send the illegal DATA frame
        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _postRequestHeaders, endStream: true);

        await SendDataAsync(1, _helloWorldBytes, endStream: false);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.DATA, streamId: 1));
        AssertConnectionEndReason(ConnectionEndReason.FrameAfterStreamClose);
    }

    [Fact]
    public async Task DATA_Received_StreamClosed_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(1, _postRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await SendDataAsync(1, _helloWorldBytes, endStream: false);

        // There's a race where either of these messages could be logged, depending on if the stream cleanup has finished yet.
        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: new[] {
                    CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.DATA, streamId: 1),
                    CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.DATA, streamId: 1)
            });
        AssertConnectionEndReason(ConnectionEndReason.UnknownStream);
    }

    [Fact]
    public async Task Frame_MultipleStreams_CanBeCreatedIfClientCountIsLessThanActualMaxStreamCount()
    {
        _serviceContext.ServerOptions.Limits.Http2.MaxStreamsPerConnection = 1;
        var firstRequestBlock = new TaskCompletionSource();
        var firstRequestReceived = new TaskCompletionSource();
        var makeFirstRequestWait = false;
        await InitializeConnectionAsync(async context =>
        {
            if (!makeFirstRequestWait)
            {
                makeFirstRequestWait = true;
                firstRequestReceived.SetResult();
                await firstRequestBlock.Task.DefaultTimeout();
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
        await SendRstStreamAsync(1);

        await firstRequestReceived.Task.DefaultTimeout();

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        firstRequestBlock.SetResult();

        await StopConnectionAsync(3, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task MaxTrackedStreams_SmallMaxConcurrentStreams_LowerLimitOf100Async()
    {
        _serviceContext.ServerOptions.Limits.Http2.MaxStreamsPerConnection = 1;

        await InitializeConnectionAsync(_noopApplication);

        Assert.Equal((uint)100, _connection.MaxTrackedStreams);

        await StopConnectionAsync(0, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task MaxTrackedStreams_DefaultMaxConcurrentStreams_DoubleLimit()
    {
        _serviceContext.ServerOptions.Limits.Http2.MaxStreamsPerConnection = 100;

        await InitializeConnectionAsync(_noopApplication);

        Assert.Equal((uint)200, _connection.MaxTrackedStreams);

        await StopConnectionAsync(0, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task MaxTrackedStreams_LargeMaxConcurrentStreams_DoubleLimit()
    {
        _serviceContext.ServerOptions.Limits.Http2.MaxStreamsPerConnection = int.MaxValue;

        await InitializeConnectionAsync(_noopApplication);

        Assert.Equal((uint)int.MaxValue * 2, _connection.MaxTrackedStreams);

        await StopConnectionAsync(0, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public void MinimumMaxTrackedStreams()
    {
        _serviceContext.ServerOptions.Limits.Http2.MaxStreamsPerConnection = 1;
        CreateConnection();
        // Kestrel always tracks at least 100 streams
        Assert.Equal(100u, _connection.MaxTrackedStreams);
        _connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);
        AssertConnectionEndReason(ConnectionEndReason.AbortedByApp);
    }

    [Fact]
    public Task Frame_MultipleStreams_RequestsNotFinished_LowMaxStreamsPerConnection_EnhanceYourCalmAfter100()
    {
        // Kestrel always tracks at least 100 streams
        return RequestUntilEnhanceYourCalm(maxStreamsPerConnection: 1, sentStreams: 101);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/30309")]
    public Task Frame_MultipleStreams_RequestsNotFinished_DefaultMaxStreamsPerConnection_EnhanceYourCalmAfterDoubleMaxStreams()
    {
        // Kestrel tracks max streams per connection * 2
        return RequestUntilEnhanceYourCalm(maxStreamsPerConnection: 100, sentStreams: 201);
    }

    [Fact]
    public async Task AbortConnectionAfterTooManyEnhanceYourCalms()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await InitializeConnectionAsync(_ => Task.FromException(new InvalidOperationException("No requests should be processed")));

        // Reject *every* stream
        _connection.SendEnhanceYourCalmOnStartStream = true;

        var enhanceYourCalmMaximumCount = Http2Connection.EnhanceYourCalmTickWindowCount * Http2Connection.EnhanceYourCalmMaximumCount;
        var streamId = 1;

        // There's a burst of activity, but the limit isn't exceeded

        for (var i = 0; i < enhanceYourCalmMaximumCount; i++)
        {
            await StartStreamAsync(streamId, _browserRequestHeaders, endStream: false);
            await WaitForStreamErrorAsync(
                expectedStreamId: streamId,
                expectedErrorCode: Http2ErrorCode.ENHANCE_YOUR_CALM,
                expectedErrorMessage: CoreStrings.Http2TellClientToCalmDown);
            streamId += 2;
        }

        // The window expires

        for (var i = 0; i < Http2Connection.EnhanceYourCalmTickWindowCount; i++)
        {
            TriggerTick();
        }

        // We have room for more streams

        for (var i = 0; i < enhanceYourCalmMaximumCount; i++)
        {
            await StartStreamAsync(streamId, _browserRequestHeaders, endStream: false);
            await WaitForStreamErrorAsync(
                expectedStreamId: streamId,
                expectedErrorCode: Http2ErrorCode.ENHANCE_YOUR_CALM,
                expectedErrorMessage: CoreStrings.Http2TellClientToCalmDown);
            streamId += 2;
        }

        // The last stream exceeds the limit

        await StartStreamAsync(streamId, _browserRequestHeaders, endStream: false);
        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: int.MaxValue,
            expectedErrorCode: Http2ErrorCode.ENHANCE_YOUR_CALM,
            expectedErrorMessage: CoreStrings.Http2ConnectionFaulted);

        AssertConnectionEndReason(ConnectionEndReason.StreamResetLimitExceeded);
    }

    private async Task RequestUntilEnhanceYourCalm(int maxStreamsPerConnection, int sentStreams)
    {
        _serviceContext.ServerOptions.Limits.Http2.MaxStreamsPerConnection = maxStreamsPerConnection;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await InitializeConnectionAsync(async context =>
        {
            await tcs.Task.DefaultTimeout();
        });

        var streamId = 1;
        for (var i = 0; i < sentStreams - 1; i++)
        {
            await StartStreamAsync(streamId, _browserRequestHeaders, endStream: true);
            await SendRstStreamAsync(streamId);

            streamId += 2;
        }

        await StartStreamAsync(streamId, _browserRequestHeaders, endStream: true);
        await WaitForStreamErrorAsync(
            expectedStreamId: streamId,
            expectedErrorCode: Http2ErrorCode.ENHANCE_YOUR_CALM,
            expectedErrorMessage: CoreStrings.Http2TellClientToCalmDown);

        tcs.SetResult();

        await StopConnectionAsync(streamId, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task DATA_Received_StreamClosedImplicitly_ConnectionError()
    {
        // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1
        //
        // The first use of a new stream identifier implicitly closes all streams in the "idle" state that
        // might have been initiated by that peer with a lower-valued stream identifier. For example, if a
        // client sends a HEADERS frame on stream 7 without ever sending a frame on stream 5, then stream 5
        // transitions to the "closed" state when the first frame for stream 7 is sent or received.

        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        await SendDataAsync(1, _helloWorldBytes, endStream: true);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 3,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.DATA, streamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnknownStream);
    }

    [Fact]
    public async Task DATA_Received_NoStreamWindowSpace_ConnectionError()
    {
        var initialWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialStreamWindowSize;
        var framesInWindow = (initialWindowSize / Http2PeerSettings.DefaultMaxFrameSize) + 1; // Round up to overflow the window

        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        for (var i = 0; i < framesInWindow; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.FLOW_CONTROL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorFlowControlWindowExceeded);

        AssertConnectionEndReason(ConnectionEndReason.FlowControlWindowExceeded);
    }

    [Fact]
    public async Task DATA_Received_NoConnectionWindowSpace_ConnectionError()
    {
        var initialWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
        var framesInWindow = initialWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        for (var i = 0; i < framesInWindow / 2; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }

        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);
        for (var i = 0; i < framesInWindow / 2; i++)
        {
            await SendDataAsync(3, _maxData, endStream: false);
        }
        // One extra to overflow the connection window
        await SendDataAsync(3, _maxData, endStream: false);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 3,
            expectedErrorCode: Http2ErrorCode.FLOW_CONTROL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorFlowControlWindowExceeded);

        AssertConnectionEndReason(ConnectionEndReason.FlowControlWindowExceeded);
    }

    [Fact]
    public async Task DATA_Sent_DespiteConnectionOutputFlowControl_IfEmptyAndEndsStream()
    {
        // Zero-length data frames are allowed to be sent even if there is no space available in the flow control window.
        // https://httpwg.org/specs/rfc7540.html#rfc.section.6.9.1

        var expectedFullFrameCountBeforeBackpressure = Http2PeerSettings.DefaultInitialWindowSize / _maxData.Length;
        var remainingBytesBeforeBackpressure = (int)Http2PeerSettings.DefaultInitialWindowSize % _maxData.Length;
        var remainingBytesAfterBackpressure = _maxData.Length - remainingBytesBeforeBackpressure;

        // Double the stream window to be 128KiB so it doesn't interfere with the rest of the test.
        _clientSettings.InitialWindowSize = Http2PeerSettings.DefaultInitialWindowSize * 2;

        await InitializeConnectionAsync(async context =>
        {
            var streamId = context.Features.Get<IHttp2StreamIdFeature>().StreamId;

            try
            {
                if (streamId == 1)
                {
                    for (var i = 0; i < expectedFullFrameCountBeforeBackpressure + 1; i++)
                    {
                        await context.Response.Body.WriteAsync(_maxData, 0, _maxData.Length);
                    }
                }

                _runningStreams[streamId].SetResult();
            }
            catch (Exception ex)
            {
                _runningStreams[streamId].SetException(ex);
                throw;
            }
        });

        // Start one stream that consumes the entire connection output window.
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        for (var i = 0; i < expectedFullFrameCountBeforeBackpressure; i++)
        {
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
        }

        await ExpectAsync(Http2FrameType.DATA,
            withLength: remainingBytesBeforeBackpressure,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Start one more stream that receives an empty response despite connection backpressure.
        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        // Relieve connection backpressure to receive the rest of the first streams body.
        await SendWindowUpdateAsync(0, remainingBytesAfterBackpressure);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: remainingBytesAfterBackpressure,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        await WaitForAllStreamsAsync();

        AssertConnectionNoError();
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/44415")]
    public async Task OutputFlowControl_ConnectionAndRequestAborted_NoException()
    {
        // Ensure the stream window size is bigger than the connection window size
        _clientSettings.InitialWindowSize = _clientSettings.InitialWindowSize * 2;

        var connectionAbortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestAbortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await InitializeConnectionAsync(async context =>
        {
            // Exceed connection window size
            await context.Response.WriteAsync(new string('!', 65536));

            await connectionAbortedTcs.Task;

            try
            {
                context.Abort();
                requestAbortedTcs.SetResult();
            }
            catch (Exception ex)
            {
                requestAbortedTcs.SetException(ex);
            }
        }).DefaultTimeout();

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true).DefaultTimeout();

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1).DefaultTimeout();

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16384,
            withFlags: (byte)(Http2DataFrameFlags.NONE),
            withStreamId: 1).DefaultTimeout();

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16384,
            withFlags: (byte)(Http2DataFrameFlags.NONE),
            withStreamId: 1).DefaultTimeout();

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16384,
            withFlags: (byte)(Http2DataFrameFlags.NONE),
            withStreamId: 1).DefaultTimeout();

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16383,
            withFlags: (byte)(Http2DataFrameFlags.NONE),
            withStreamId: 1).DefaultTimeout();

        _connection.HandleReadDataRateTimeout();

        connectionAbortedTcs.SetResult();

        // Task completing successfully means HttpContext.Abort didn't throw
        await requestAbortedTcs.Task.DefaultTimeout();
    }

    [Fact]
    public async Task DATA_Sent_DespiteStreamOutputFlowControl_IfEmptyAndEndsStream()
    {
        // Zero-length data frames are allowed to be sent even if there is no space available in the flow control window.
        // https://httpwg.org/specs/rfc7540.html#rfc.section.6.9.1

        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = 0;

        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task StreamWindow_BiggerThan_ConnectionWindow()
    {
        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = 16384 * 5;

        await InitializeConnectionAsync(async context =>
        {
            var data = new byte[16384 * 4];
            data.AsSpan().Fill(1);
            await context.Response.Body.WriteAsync(data);
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1).DefaultTimeout();

        for (int i = 0; i < 3; i++)
        {
            await ExpectAsync(Http2FrameType.DATA,
                withLength: 16384,
                withFlags: (byte)(Http2DataFrameFlags.NONE),
                withStreamId: 1).DefaultTimeout();
        }

        // Now we've consumed the entire connection window, and there's one more frame to write
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 16383,
            withFlags: (byte)(Http2DataFrameFlags.NONE),
            withStreamId: 1).DefaultTimeout();

        // Send more than enough bytes for the window update
        await SendWindowUpdateAsync(0, 1);

        // Expect the last frame
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 1,
            withFlags: (byte)(Http2DataFrameFlags.NONE),
            withStreamId: 1).DefaultTimeout();

        // 0 length end of stream frame
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task HEADERS_Received_Decoded()
    {
        await InitializeConnectionAsync(_readHeadersApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        VerifyDecodedRequestHeaders(_browserRequestHeaders);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public async Task HEADERS_Received_WithPadding_Decoded(byte padLength)
    {
        await InitializeConnectionAsync(_readHeadersApplication);

        await SendHeadersWithPaddingAsync(1, _browserRequestHeaders, padLength, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        VerifyDecodedRequestHeaders(_browserRequestHeaders);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task HEADERS_Received_WithPriority_Decoded()
    {
        await InitializeConnectionAsync(_readHeadersApplication);

        await SendHeadersWithPriorityAsync(1, _browserRequestHeaders, priority: 42, streamDependency: 0, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        VerifyDecodedRequestHeaders(_browserRequestHeaders);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public async Task HEADERS_Received_WithPriorityAndPadding_Decoded(byte padLength)
    {
        await InitializeConnectionAsync(_readHeadersApplication);

        await SendHeadersWithPaddingAndPriorityAsync(1, _browserRequestHeaders, padLength, priority: 42, streamDependency: 0, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        VerifyDecodedRequestHeaders(_browserRequestHeaders);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HEADERS_Received_WithTrailers_Available(bool sendData)
    {
        await InitializeConnectionAsync(_readTrailersApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);

        // Initialize another stream with a higher stream ID, and verify that after trailers are
        // decoded by the other stream, the highest opened stream ID is not reset to the lower ID
        // (the highest opened stream ID is sent by the server in the GOAWAY frame when shutting
        // down the connection).
        await SendHeadersAsync(3, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

        // The second stream should end first, since the first one is waiting for the request body.
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        if (sendData)
        {
            await SendDataAsync(1, _helloBytes, endStream: false);
        }

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _requestTrailers);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        VerifyDecodedRequestHeaders(_browserRequestHeaders);

        // Make sure the trailers are in the trailers collection.
        foreach (var header in _requestTrailers)
        {
            Assert.False(_receivedHeaders.ContainsKey(header.Key));
            Assert.True(_receivedTrailers.ContainsKey(header.Key));
            Assert.Equal(header.Value, _receivedTrailers[header.Key]);
        }

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task HEADERS_Received_ContainsExpect100Continue_100ContinueSent()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _expectContinueRequestHeaders, false);

        var frame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 5,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        Assert.Equal(new byte[] { 0x08, 0x03, (byte)'1', (byte)'0', (byte)'0' }, frame.PayloadSequence.ToArray());

        await SendDataAsync(1, _helloBytes, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task HEADERS_Received_AppCannotBlockOtherFrames()
    {
        var firstRequestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishFirstRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRequestReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishSecondRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await InitializeConnectionAsync(async context =>
        {
            if (!firstRequestReceived.Task.IsCompleted)
            {
                firstRequestReceived.TrySetResult();

                await finishFirstRequest.Task.DefaultTimeout();
            }
            else
            {
                secondRequestReceived.TrySetResult();

                await finishSecondRequest.Task.DefaultTimeout();
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await firstRequestReceived.Task.DefaultTimeout();

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        await secondRequestReceived.Task.DefaultTimeout();

        finishSecondRequest.TrySetResult();

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        finishFirstRequest.TrySetResult();

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task HEADERS_HeaderTableSizeLimitZero_Received_DynamicTableUpdate()
    {
        _serviceContext.ServerOptions.Limits.Http2.HeaderTableSize = 0;

        await InitializeConnectionAsync(_noopApplication, expectedSettingsCount: 5);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        _hpackEncoder.UpdateMaxHeaderTableSize(0);

        var headerFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 38,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        const byte DynamicTableSizeUpdateMask = 0xe0;

        var integerDecoder = new IntegerDecoder();
        Assert.True(integerDecoder.BeginTryDecode((byte)(headerFrame.Payload.Span[0] & ~DynamicTableSizeUpdateMask), prefixLength: 5, out var result));

        // Dynamic table update from the server
        Assert.Equal(0, result);

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 37,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task HEADERS_ResponseSetsIgnoreIndexAndNeverIndexValues_HeadersParsed()
    {
        await InitializeConnectionAsync(c =>
        {
            c.Response.ContentLength = 0;
            c.Response.Headers.SetCookie = "SetCookie!";
            c.Response.Headers.ContentDisposition = "ContentDisposition!";

            return Task.CompletedTask;
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var frame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 90,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        var handler = new TestHttpHeadersHandler();

        var hpackDecoder = new HPackDecoder();
        hpackDecoder.Decode(new ReadOnlySequence<byte>(frame.Payload), endHeaders: true, handler);
        hpackDecoder.CompleteDecode();

        Assert.Equal("200", handler.Headers[":status"]);
        Assert.Equal("SetCookie!", handler.Headers[HeaderNames.SetCookie]);
        Assert.Equal("ContentDisposition!", handler.Headers[HeaderNames.ContentDisposition]);
        Assert.Equal("0", handler.Headers[HeaderNames.ContentLength]);

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        frame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 60,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        handler = new TestHttpHeadersHandler();

        hpackDecoder.Decode(new ReadOnlySequence<byte>(frame.Payload), endHeaders: true, handler);
        hpackDecoder.CompleteDecode();

        Assert.Equal("200", handler.Headers[":status"]);
        Assert.Equal("SetCookie!", handler.Headers[HeaderNames.SetCookie]);
        Assert.Equal("ContentDisposition!", handler.Headers[HeaderNames.ContentDisposition]);
        Assert.Equal("0", handler.Headers[HeaderNames.ContentLength]);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
    }

    private class TestHttpHeadersHandler : IHttpStreamHeadersHandler
    {
        public readonly Dictionary<string, StringValues> Headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        public void OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            OnHeader(name, value);
        }

        public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            var nameString = Encoding.ASCII.GetString(name);
            var valueString = Encoding.ASCII.GetString(value);

            if (Headers.TryGetValue(nameString, out var values))
            {
                var l = values.ToList();
                l.Add(valueString);

                Headers[nameString] = new StringValues(l.ToArray());
            }
            else
            {
                Headers[nameString] = new StringValues(valueString);
            }
        }

        public void OnHeadersComplete(bool endStream)
        {
            throw new NotImplementedException();
        }

        public void OnStaticIndexedHeader(int index)
        {
            ref readonly var entry = ref H2StaticTable.Get(index - 1);
            OnHeader(entry.Name, entry.Value);
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            OnHeader(H2StaticTable.Get(index - 1).Name, value);
        }
    }

    [Fact]
    public async Task HEADERS_DisableDynamicHeaderCompression_HeadersNotCompressed()
    {
        _serviceContext.ServerOptions.AllowResponseHeaderCompression = false;

        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 37,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 37,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task HEADERS_OverMaxStreamLimit_Refused()
    {
        CreateConnection();

        _connection.ServerSettings.MaxConcurrentStreams = 1;

        var requestBlocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await InitializeConnectionAsync(context => requestBlocker.Task);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        await WaitForStreamErrorAsync(3, Http2ErrorCode.REFUSED_STREAM, CoreStrings.Http2ErrorMaxStreams);

        requestBlocker.SetResult();

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task HEADERS_Received_StreamIdZero_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(0, _browserRequestHeaders, endStream: true);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.HEADERS));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task HEADERS_Received_StreamIdEven_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(2, _browserRequestHeaders, endStream: true);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.HEADERS, streamId: 2));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task HEADERS_Received_StreamClosed_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        // Try to re-use the stream ID (http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1)
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        // There's a race where either of these messages could be logged, depending on if the stream cleanup has finished yet.
        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: new[] {
                    CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1),
                    CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.HEADERS, streamId: 1)
            });

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task HEADERS_Received_StreamHalfClosedRemote_ConnectionError()
    {
        // Use _waitForAbortApplication so we know the stream will still be active when we send the illegal DATA frame
        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.HEADERS, streamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.FrameAfterStreamClose);
    }

    [Fact]
    public async Task HEADERS_Received_StreamClosedImplicitly_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        // Stream 1 was implicitly closed by opening stream 3 before (http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1)
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 3,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(255)]
    public async Task HEADERS_Received_PaddingEqualToFramePayloadLength_ConnectionError(byte padLength)
    {
        await InitializeConnectionAsync(_noopApplication);

        // The payload length includes the pad length field
        await SendInvalidHeadersFrameAsync(1, payloadLength: padLength, padLength: padLength);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.HEADERS));

        AssertConnectionEndReason(ConnectionEndReason.InvalidDataPadding);
    }

    [Fact]
    public async Task HEADERS_Received_PaddingFieldMissing_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendInvalidHeadersFrameAsync(1, payloadLength: 0, padLength: 1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.HEADERS, expectedLength: 1));

        AssertConnectionEndReason(ConnectionEndReason.InvalidFrameLength);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(254, 255)]
    public async Task HEADERS_Received_PaddingGreaterThanFramePayloadLength_ConnectionError(int frameLength, byte padLength)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendInvalidHeadersFrameAsync(1, frameLength, padLength);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorPaddingTooLong(Http2FrameType.HEADERS));

        AssertConnectionEndReason(ConnectionEndReason.InvalidDataPadding);
    }

    [Fact]
    public async Task HEADERS_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendHeadersAsync(3, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.HEADERS, streamId: 3, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Fact]
    public async Task HEADERS_Received_WithPriority_StreamDependencyOnSelf_ConnectionError()
    {
        await InitializeConnectionAsync(_readHeadersApplication);

        await SendHeadersWithPriorityAsync(1, _browserRequestHeaders, priority: 42, streamDependency: 1, endStream: true);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamSelfDependency(Http2FrameType.HEADERS, streamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.StreamSelfDependency);
    }

    [Fact]
    public async Task HEADERS_Received_IncompleteHeaderBlock_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendIncompleteHeadersFrameAsync(streamId: 1);

        await WaitForConnectionErrorAsync<HPackDecodingException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.COMPRESSION_ERROR,
            expectedErrorMessage: SR.net_http_hpack_incomplete_header_block);

        AssertConnectionEndReason(ConnectionEndReason.ErrorReadingHeaders);
    }

    [Fact]
    public async Task HEADERS_Received_IntegerOverLimit_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        var outputWriter = _pair.Application.Output;
        var frame = new Http2Frame();

        frame.PrepareHeaders(Http2HeadersFrameFlags.END_HEADERS, 1);
        frame.PayloadLength = 7;
        var payload = new byte[]
        {
                // Set up an incomplete Literal Header Field w/ Incremental Indexing frame,
                0x00,
                // with an name of size that's greater than int.MaxValue
                0x7f, 0x80, 0x80, 0x80, 0x80, 0x7f
        };

        Http2FrameWriter.WriteHeader(frame, outputWriter);
        await SendAsync(payload);

        await WaitForConnectionErrorAsync<HPackDecodingException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.COMPRESSION_ERROR,
            expectedErrorMessage: SR.net_http_hpack_bad_integer);

        AssertConnectionEndReason(ConnectionEndReason.ErrorReadingHeaders);
    }

    [Theory]
    [MemberData(nameof(IllegalTrailerData))]
    public async Task HEADERS_Received_WithTrailers_ContainsIllegalTrailer_ConnectionError(byte[] trailers, string expectedErrorMessage)
    {
        await InitializeConnectionAsync(_readTrailersApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, trailers);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: expectedErrorMessage);

        AssertConnectionEndReason(ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [InlineData((int)Http2HeadersFrameFlags.NONE)]
    [InlineData((int)Http2HeadersFrameFlags.END_HEADERS)]
    public async Task HEADERS_Received_WithTrailers_EndStreamNotSet_ConnectionError(int intFlags)
    {
        var flags = (Http2HeadersFrameFlags)intFlags;
        await InitializeConnectionAsync(_readTrailersApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);
        await SendHeadersAsync(1, flags, _requestTrailers);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorHeadersWithTrailersNoEndStream);

        AssertConnectionEndReason(ConnectionEndReason.MissingStreamEnd);
    }

    [Theory]
    [MemberData(nameof(UpperCaseHeaderNameData))]
    public async Task HEADERS_Received_HeaderNameContainsUpperCaseCharacter_ConnectionError(byte[] headerBlock)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, headerBlock);
        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.HttpErrorHeaderNameUppercase);

        AssertConnectionEndReason(ConnectionEndReason.InvalidRequestHeaders);
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

        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            expectedErrorMessage: CoreStrings.HttpErrorUnknownPseudoHeaderField,
            expectedEndReason: ConnectionEndReason.InvalidRequestHeaders);
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

        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            expectedErrorMessage: CoreStrings.HttpErrorResponsePseudoHeaderField,
            expectedEndReason: ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [MemberData(nameof(DuplicatePseudoHeaderFieldData))]
    public Task HEADERS_Received_HeaderBlockContainsDuplicatePseudoHeaderField_ConnectionError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            expectedErrorMessage: CoreStrings.HttpErrorDuplicatePseudoHeaderField,
            expectedEndReason: ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [MemberData(nameof(ConnectMissingPseudoHeaderFieldData))]
    public async Task HEADERS_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_MethodIsCONNECT_NoError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Theory]
    [MemberData(nameof(PseudoHeaderFieldAfterRegularHeadersData))]
    public Task HEADERS_Received_HeaderBlockContainsPseudoHeaderFieldAfterRegularHeaders_ConnectionError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            expectedErrorMessage: CoreStrings.HttpErrorPseudoHeaderFieldAfterRegularHeaders,
            expectedEndReason: ConnectionEndReason.InvalidRequestHeaders);
    }

    private async Task HEADERS_Received_InvalidHeaderFields_ConnectionError(IEnumerable<KeyValuePair<string, string>> headers, string expectedErrorMessage, ConnectionEndReason expectedEndReason)
    {
        await InitializeConnectionAsync(_noopApplication);
        await StartStreamAsync(1, headers, endStream: true);
        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: expectedErrorMessage);

        AssertConnectionEndReason(expectedEndReason);
    }

    [Theory]
    [MemberData(nameof(MissingPseudoHeaderFieldData))]
    public async Task HEADERS_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_StreamError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);
        await WaitForStreamErrorAsync(
             expectedStreamId: 1,
             expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
             expectedErrorMessage: CoreStrings.HttpErrorMissingMandatoryPseudoHeaderFields);

        // Verify that the stream ID can't be re-used
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);
        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public Task HEADERS_Received_HeaderBlockOverLimit_ConnectionError()
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

        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            CoreStrings.BadRequest_HeadersExceedMaxTotalSize,
            expectedEndReason: ConnectionEndReason.MaxRequestHeadersTotalSizeExceeded);
    }

    [Fact]
    public Task HEADERS_Received_TooManyHeaders_ConnectionError()
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

        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            CoreStrings.BadRequest_TooManyHeaders,
            expectedEndReason: ConnectionEndReason.MaxRequestHeaderCountExceeded);
    }

    [Fact]
    public Task HEADERS_Received_InvalidCharacters_ConnectionError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>("Custom", "val\0ue"),
        };

        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            CoreStrings.BadRequest_MalformedRequestInvalidHeaders,
            expectedEndReason: ConnectionEndReason.InvalidRequestHeaders);
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

        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            CoreStrings.HttpErrorConnectionSpecificHeaderField,
            expectedEndReason: ConnectionEndReason.InvalidRequestHeaders);
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

        return HEADERS_Received_InvalidHeaderFields_ConnectionError(
            headers,
            CoreStrings.HttpErrorConnectionSpecificHeaderField,
            expectedEndReason: ConnectionEndReason.InvalidRequestHeaders);
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

        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task HEADERS_Received_RequestLineLength_StreamError()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, new string('A', 8192 / 2)),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/" + new string('A', 8192 / 2)),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http")
        };

        await InitializeConnectionAsync(_noopApplication);
        await StartStreamAsync(1, headers, endStream: true);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.PROTOCOL_ERROR, CoreStrings.BadRequest_RequestLineTooLong);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task HEADERS_CookiesMergedIntoOne()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Cookie, "a=0"),
            new KeyValuePair<string, string>(HeaderNames.Cookie, "b=1"),
            new KeyValuePair<string, string>(HeaderNames.Cookie, "c=2"),
        };

        await InitializeConnectionAsync(_readHeadersApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        Assert.Equal("a=0; b=1; c=2", _receivedHeaders[HeaderNames.Cookie]);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task PRIORITY_Received_StreamIdZero_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendPriorityAsync(0);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.PRIORITY));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task PRIORITY_Received_StreamIdEven_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendPriorityAsync(2);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.PRIORITY, streamId: 2));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public async Task PRIORITY_Received_LengthNotFive_ConnectionError(int length)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendInvalidPriorityFrameAsync(1, length);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.PRIORITY, expectedLength: 5));

        AssertConnectionEndReason(ConnectionEndReason.InvalidFrameLength);
    }

    [Fact]
    public async Task PRIORITY_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendPriorityAsync(1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.PRIORITY, streamId: 1, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Fact]
    public async Task PRIORITY_Received_StreamDependencyOnSelf_ConnectionError()
    {
        await InitializeConnectionAsync(_readHeadersApplication);

        await SendPriorityAsync(1, streamDependency: 1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamSelfDependency(Http2FrameType.PRIORITY, 1));

        AssertConnectionEndReason(ConnectionEndReason.StreamSelfDependency);
    }

    [Fact]
    public async Task RST_STREAM_Received_ContinuesAppsAwaitingConnectionOutputFlowControl()
    {
        var writeTasks = new Task[4];

        var expectedFullFrameCountBeforeBackpressure = Http2PeerSettings.DefaultInitialWindowSize / _maxData.Length;
        var remainingBytesBeforeBackpressure = (int)Http2PeerSettings.DefaultInitialWindowSize % _maxData.Length;

        // Double the stream window to be 128KiB so it doesn't interfere with the rest of the test.
        _clientSettings.InitialWindowSize = Http2PeerSettings.DefaultInitialWindowSize * 2;

        await InitializeConnectionAsync(async context =>
        {
            var streamId = context.Features.Get<IHttp2StreamIdFeature>().StreamId;

            var abortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.RequestAborted.Register(() =>
            {
                lock (_abortedStreamIdsLock)
                {
                    _abortedStreamIds.Add(streamId);
                    abortedTcs.SetResult();
                }
            });

            try
            {
                writeTasks[streamId] = writeTcs.Task;

                // Flush headers even if the body can't yet be written because of flow control.
                await context.Response.Body.FlushAsync();

                for (var i = 0; i < expectedFullFrameCountBeforeBackpressure; i++)
                {
                    await context.Response.Body.WriteAsync(_maxData, 0, _maxData.Length);
                }

                await context.Response.Body.WriteAsync(_maxData, 0, remainingBytesBeforeBackpressure + 1);

                writeTcs.SetResult();

                await abortedTcs.Task;

                _runningStreams[streamId].SetResult();
            }
            catch (Exception ex)
            {
                _runningStreams[streamId].SetException(ex);
                throw;
            }
        });

        // Start one stream that consumes the entire connection output window.
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        for (var i = 0; i < expectedFullFrameCountBeforeBackpressure; i++)
        {
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
        }

        await ExpectAsync(Http2FrameType.DATA,
            withLength: remainingBytesBeforeBackpressure,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Ensure connection-level backpressure was hit.
        Assert.False(writeTasks[1].IsCompleted);

        // Start another stream that immediately experiences backpressure.
        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);

        // The headers, but not the data for stream 3, can be sent prior to any window updates.
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);

        await SendRstStreamAsync(1);
        // Any paused writes for stream 1 should complete after an RST_STREAM
        // even without any preceding window updates.
        await _runningStreams[1].Task.DefaultTimeout();

        // A connection-level window update allows the non-reset stream to continue.
        await SendWindowUpdateAsync(0, (int)Http2PeerSettings.DefaultInitialWindowSize);

        for (var i = 0; i < expectedFullFrameCountBeforeBackpressure; i++)
        {
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 3);
        }

        await ExpectAsync(Http2FrameType.DATA,
            withLength: remainingBytesBeforeBackpressure,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);

        Assert.False(writeTasks[3].IsCompleted);

        await SendRstStreamAsync(3);
        await _runningStreams[3].Task.DefaultTimeout();

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);

        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);
        Assert.Contains(3, _abortedStreamIds);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task RST_STREAM_Received_ContinuesAppsAwaitingStreamOutputFlowControl()
    {
        var writeTasks = new Task[6];
        var initialWindowSize = _helloWorldBytes.Length / 2;

        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = (uint)initialWindowSize;

        await InitializeConnectionAsync(async context =>
        {
            var streamId = context.Features.Get<IHttp2StreamIdFeature>().StreamId;

            var abortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.RequestAborted.Register(() =>
            {
                lock (_abortedStreamIdsLock)
                {
                    _abortedStreamIds.Add(streamId);
                    abortedTcs.SetResult();
                }
            });

            try
            {
                writeTasks[streamId] = writeTcs.Task;
                await context.Response.Body.WriteAsync(_helloWorldBytes, 0, _helloWorldBytes.Length);
                writeTcs.SetResult();

                await abortedTcs.Task;

                _runningStreams[streamId].SetResult();
            }
            catch (Exception ex)
            {
                _runningStreams[streamId].SetException(ex);
                throw;
            }
        });

        async Task VerifyStreamBackpressure(int streamId, int headersLength)
        {
            await StartStreamAsync(streamId, _browserRequestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: headersLength,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: streamId);

            var dataFrame = await ExpectAsync(Http2FrameType.DATA,
                withLength: initialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: streamId);

            Assert.True(_helloWorldBytes.AsSpan(0, initialWindowSize).SequenceEqual(dataFrame.PayloadSequence.ToArray()));
            Assert.False(writeTasks[streamId].IsCompleted);
        }

        await VerifyStreamBackpressure(1, 32);
        await VerifyStreamBackpressure(3, 2);
        await VerifyStreamBackpressure(5, 2);

        await SendRstStreamAsync(1);
        await writeTasks[1].DefaultTimeout();
        Assert.False(writeTasks[3].IsCompleted);
        Assert.False(writeTasks[5].IsCompleted);

        await SendRstStreamAsync(3);
        await writeTasks[3].DefaultTimeout();
        Assert.False(writeTasks[5].IsCompleted);

        await SendRstStreamAsync(5);
        await writeTasks[5].DefaultTimeout();

        await StopConnectionAsync(expectedLastStreamId: 5, ignoreNonGoAwayFrames: false);

        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);
        Assert.Contains(3, _abortedStreamIds);
        Assert.Contains(5, _abortedStreamIds);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task RST_STREAM_Received_ReturnsSpaceToConnectionInputFlowControlWindow()
    {
        var initialConnectionWindowSize = _serviceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
        var framesInConnectionWindow = initialConnectionWindowSize / Http2PeerSettings.DefaultMaxFrameSize;

        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        // Rounds down so we don't go over the half window size and trigger an update
        for (var i = 0; i < framesInConnectionWindow / 2; i++)
        {
            await SendDataAsync(1, _maxData, endStream: false);
        }

        // Go over the threshold and trigger an update
        await SendDataAsync(1, _maxData, endStream: false);

        await SendRstStreamAsync(1);
        await WaitForAllStreamsAsync();

        var connectionWindowUpdateFrame = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 0);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        Assert.Contains(1, _abortedStreamIds);
        var updateSize = ((framesInConnectionWindow / 2) + 1) * _maxData.Length;
        Assert.Equal(updateSize, connectionWindowUpdateFrame.WindowUpdateSizeIncrement);
    }

    [Fact]
    public async Task RST_STREAM_Received_StreamIdZero_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendRstStreamAsync(0);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.RST_STREAM));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task RST_STREAM_Received_StreamIdEven_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendRstStreamAsync(2);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.RST_STREAM, streamId: 2));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task RST_STREAM_Received_StreamIdle_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendRstStreamAsync(1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdle(Http2FrameType.RST_STREAM, streamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public async Task RST_STREAM_Received_LengthNotFour_ConnectionError(int length)
    {
        await InitializeConnectionAsync(_noopApplication);

        // Start stream 1 so it's legal to send it RST_STREAM frames
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await SendInvalidRstStreamFrameAsync(1, length);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.RST_STREAM, expectedLength: 4));

        AssertConnectionEndReason(ConnectionEndReason.InvalidFrameLength);
    }

    [Fact]
    public async Task RST_STREAM_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendRstStreamAsync(1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.RST_STREAM, streamId: 1, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    // Compare to h2spec http2/5.1/8
    [Fact]
    public async Task RST_STREAM_IncompleteRequest_AdditionalDataFrames_ConnectionAborted()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(context => tcs.Task);

        await StartStreamAsync(1, headers, endStream: false);
        await SendDataAsync(1, new byte[1], endStream: false);
        await SendDataAsync(1, new byte[2], endStream: false);
        await SendRstStreamAsync(1);
        await SendDataAsync(1, new byte[10], endStream: false);
        tcs.TrySetResult();

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(ignoreNonGoAwayFrames: false, expectedLastStreamId: 1,
            Http2ErrorCode.STREAM_CLOSED, CoreStrings.FormatHttp2ErrorStreamAborted(Http2FrameType.DATA, 1));

        AssertConnectionEndReason(ConnectionEndReason.FrameAfterStreamClose);
    }

    [Fact]
    public async Task RST_STREAM_IncompleteRequest_AdditionalTrailerFrames_ConnectionAborted()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(context => tcs.Task);

        await StartStreamAsync(1, headers, endStream: false);
        await SendDataAsync(1, new byte[1], endStream: false);
        await SendDataAsync(1, new byte[2], endStream: false);
        await SendRstStreamAsync(1);
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _requestTrailers);
        tcs.TrySetResult();

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(ignoreNonGoAwayFrames: false, expectedLastStreamId: 1,
            Http2ErrorCode.STREAM_CLOSED, CoreStrings.FormatHttp2ErrorStreamAborted(Http2FrameType.HEADERS, 1));

        AssertConnectionEndReason(ConnectionEndReason.FrameAfterStreamClose);
    }

    [Fact]
    public async Task RST_STREAM_IncompleteRequest_AdditionalResetFrame_IgnoreAdditionalReset()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(context => tcs.Task);

        await StartStreamAsync(1, headers, endStream: false);
        await SendDataAsync(1, new byte[1], endStream: false);
        await SendRstStreamAsync(1);
        await SendRstStreamAsync(1);
        tcs.TrySetResult();

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/53744")]
    [Fact]
    public async Task RST_STREAM_IncompleteRequest_AdditionalWindowUpdateFrame_ConnectionAborted()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(context => tcs.Task);

        await StartStreamAsync(1, headers, endStream: false);
        await SendDataAsync(1, new byte[1], endStream: false);
        await SendRstStreamAsync(1);
        await SendWindowUpdateAsync(1, 1024);
        tcs.TrySetResult();

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(ignoreNonGoAwayFrames: false, expectedLastStreamId: 1,
            Http2ErrorCode.STREAM_CLOSED, CoreStrings.FormatHttp2ErrorStreamAborted(Http2FrameType.WINDOW_UPDATE, 1));
    }

    [Fact]
    public async Task SETTINGS_KestrelDefaults_Sent()
    {
        CreateConnection();

        _connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(_noopApplication));

        await SendPreambleAsync().ConfigureAwait(false);
        await SendSettingsAsync();

        var frame = await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: Http2FrameReader.SettingSize * 4,
            withFlags: 0,
            withStreamId: 0);

        // Only non protocol defaults are sent
        var settings = Http2FrameReader.ReadSettings(frame.PayloadSequence);
        Assert.Equal(4, settings.Count);

        var setting = settings[0];
        Assert.Equal(Http2SettingsParameter.SETTINGS_MAX_CONCURRENT_STREAMS, setting.Parameter);
        Assert.Equal(100u, setting.Value);

        setting = settings[1];
        Assert.Equal(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE, setting.Parameter);
        Assert.Equal(768 * 1024u, setting.Value);

        setting = settings[2];
        Assert.Equal(Http2SettingsParameter.SETTINGS_MAX_HEADER_LIST_SIZE, setting.Parameter);
        Assert.Equal(32 * 1024u, setting.Value);

        setting = settings[3];
        Assert.Equal(Http2SettingsParameter.SETTINGS_ENABLE_CONNECT_PROTOCOL, setting.Parameter);
        Assert.Equal(1u, setting.Value);

        var update = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2SettingsFrameFlags.NONE,
            withStreamId: 0);

        Assert.Equal(1024 * 1024 - (int)Http2PeerSettings.DefaultInitialWindowSize, update.WindowUpdateSizeIncrement);

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task SETTINGS_Custom_Sent()
    {
        CreateConnection();

        _connection.ServerSettings.HeaderTableSize = 0;
        _connection.ServerSettings.MaxConcurrentStreams = 1;
        _connection.ServerSettings.MaxHeaderListSize = 4 * 1024;
        _connection.ServerSettings.InitialWindowSize = 1024 * 1024 * 10;

        _connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(_noopApplication));

        await SendPreambleAsync().ConfigureAwait(false);
        await SendSettingsAsync();

        var frame = await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: Http2FrameReader.SettingSize * 5,
            withFlags: 0,
            withStreamId: 0);

        // Only non protocol defaults are sent
        var settings = Http2FrameReader.ReadSettings(frame.PayloadSequence);
        Assert.Equal(5, settings.Count);

        var setting = settings[0];
        Assert.Equal(Http2SettingsParameter.SETTINGS_HEADER_TABLE_SIZE, setting.Parameter);
        Assert.Equal(0u, setting.Value);

        setting = settings[1];
        Assert.Equal(Http2SettingsParameter.SETTINGS_MAX_CONCURRENT_STREAMS, setting.Parameter);
        Assert.Equal(1u, setting.Value);

        setting = settings[2];
        Assert.Equal(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE, setting.Parameter);
        Assert.Equal(1024 * 1024 * 10u, setting.Value);

        setting = settings[3];
        Assert.Equal(Http2SettingsParameter.SETTINGS_MAX_HEADER_LIST_SIZE, setting.Parameter);
        Assert.Equal(4 * 1024u, setting.Value);

        setting = settings[4];
        Assert.Equal(Http2SettingsParameter.SETTINGS_ENABLE_CONNECT_PROTOCOL, setting.Parameter);
        Assert.Equal(1u, setting.Value);

        var update = await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: (byte)Http2SettingsFrameFlags.NONE,
            withStreamId: 0);

        Assert.Equal(1024 * 1024u - Http2PeerSettings.DefaultInitialWindowSize, (uint)update.WindowUpdateSizeIncrement);

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task SETTINGS_Received_Sends_ACK()
    {
        await InitializeConnectionAsync(_noopApplication);

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task SETTINGS_ACK_Received_DoesNotSend_ACK()
    {
        await InitializeConnectionAsync(_noopApplication);

        var frame = new Http2Frame();
        frame.PrepareSettings(Http2SettingsFrameFlags.ACK);
        Http2FrameWriter.WriteHeader(frame, _pair.Application.Output);
        await FlushAsync(_pair.Application.Output);

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task SETTINGS_Received_StreamIdNotZero_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendSettingsWithInvalidStreamIdAsync(1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdNotZero(Http2FrameType.SETTINGS));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Theory]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_ENABLE_PUSH), 2, (int)(Http2ErrorCode.PROTOCOL_ERROR))]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_ENABLE_PUSH), uint.MaxValue, (int)(Http2ErrorCode.PROTOCOL_ERROR))]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE), (uint)int.MaxValue + 1, (int)(Http2ErrorCode.FLOW_CONTROL_ERROR))]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE), uint.MaxValue, (int)(Http2ErrorCode.FLOW_CONTROL_ERROR))]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE), 0, (int)(Http2ErrorCode.PROTOCOL_ERROR))]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE), 1, (int)(Http2ErrorCode.PROTOCOL_ERROR))]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE), 16 * 1024 - 1, (int)(Http2ErrorCode.PROTOCOL_ERROR))]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE), 16 * 1024 * 1024, (int)(Http2ErrorCode.PROTOCOL_ERROR))]
    [InlineData((int)(Http2SettingsParameter.SETTINGS_MAX_FRAME_SIZE), uint.MaxValue, (int)(Http2ErrorCode.PROTOCOL_ERROR))]
    public async Task SETTINGS_Received_InvalidParameterValue_ConnectionError(int intParameter, uint value, int intExpectedErrorCode)
    {
        var parameter = (Http2SettingsParameter)intParameter;
        var expectedErrorCode = (Http2ErrorCode)intExpectedErrorCode;

        await InitializeConnectionAsync(_noopApplication);

        await SendSettingsWithInvalidParameterValueAsync(parameter, value);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: expectedErrorCode,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorSettingsParameterOutOfRange(parameter));

        AssertConnectionEndReason(ConnectionEndReason.InvalidSettings);
    }

    [Fact]
    public async Task SETTINGS_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendSettingsAsync();

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.SETTINGS, streamId: 0, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16 * 1024 - 9)] // Min. max. frame size minus header length
    public async Task SETTINGS_Received_WithACK_LengthNotZero_ConnectionError(int length)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendSettingsAckWithInvalidLengthAsync(length);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorSettingsAckLengthNotZero);

        AssertConnectionEndReason(ConnectionEndReason.InvalidFrameLength);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(34)]
    [InlineData(37)]
    public async Task SETTINGS_Received_LengthNotMultipleOfSix_ConnectionError(int length)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendSettingsWithInvalidLengthAsync(length);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorSettingsLengthNotMultipleOfSix);

        AssertConnectionEndReason(ConnectionEndReason.InvalidFrameLength);
    }

    [Fact]
    public async Task SETTINGS_Received_WithInitialWindowSizePushingStreamWindowOverMax_ConnectionError()
    {
        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        await SendWindowUpdateAsync(1, (int)(Http2PeerSettings.MaxWindowSize - _clientSettings.InitialWindowSize));

        _clientSettings.InitialWindowSize += 1;
        await SendSettingsAsync();

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.FLOW_CONTROL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorInitialWindowSizeInvalid);

        AssertConnectionEndReason(ConnectionEndReason.InvalidSettings);
    }

    [Fact]
    public async Task SETTINGS_Received_ChangesAllowedResponseMaxFrameSize()
    {
        CreateConnection();

        _connection.ServerSettings.MaxFrameSize = Http2PeerSettings.MaxAllowedMaxFrameSize;
        // This includes the default response headers such as :status, etc
        var defaultResponseHeaderLength = 32;
        var headerValueLength = Http2PeerSettings.MinAllowedMaxFrameSize;
        // First byte is always 0
        // Second byte is the length of header name which is 1
        // Third byte is the header name which is A/B
        // Next three bytes are the 7-bit integer encoding representation of the header length which is 16*1024
        var encodedHeaderLength = 1 + 1 + 1 + 3 + headerValueLength;
        // Adding 10 additional bytes for encoding overhead
        var payloadLength = defaultResponseHeaderLength + encodedHeaderLength;

        await InitializeConnectionAsync(context =>
        {
            context.Response.Headers["A"] = new string('a', headerValueLength);
            context.Response.Headers["B"] = new string('b', headerValueLength);
            return context.Response.Body.WriteAsync(new byte[payloadLength], 0, payloadLength);
        }, expectedSettingsCount: 5);

        // Update client settings
        _clientSettings.MaxFrameSize = (uint)payloadLength;
        await SendSettingsAsync();

        // ACK
        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        // Start request
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: defaultResponseHeaderLength + encodedHeaderLength,
            withFlags: (byte)Http2HeadersFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.CONTINUATION,
            withLength: encodedHeaderLength,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: payloadLength,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task SETTINGS_Received_ClientMaxFrameSizeCannotExceedServerMaxFrameSize()
    {
        var serverMaxFrame = Http2PeerSettings.MinAllowedMaxFrameSize + 1024;

        CreateConnection();

        _connection.ServerSettings.MaxFrameSize = Http2PeerSettings.MinAllowedMaxFrameSize + 1024;
        var clientMaxFrame = serverMaxFrame + 1024 * 5;
        _clientSettings.MaxFrameSize = (uint)clientMaxFrame;

        await InitializeConnectionAsync(context =>
        {
            return context.Response.Body.WriteAsync(new byte[clientMaxFrame], 0, clientMaxFrame);
        }, expectedSettingsCount: 5);

        // Start request
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: serverMaxFrame,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: clientMaxFrame - serverMaxFrame,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task SETTINGS_Received_ChangesHeaderTableSize()
    {
        await InitializeConnectionAsync(_noopApplication);

        // Update client settings
        _clientSettings.HeaderTableSize = 65536; // Chrome's default, larger than the 4kb spec default
        await SendSettingsAsync();

        // ACK
        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        // Start request
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headerFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        // Headers start with :status = 200
        Assert.Equal(0x88, headerFrame.Payload.Span[0]);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task SETTINGS_Received_WithLargeHeaderTableSizeLimit_ChangesHeaderTableSize()
    {
        _serviceContext.ServerOptions.Limits.Http2.HeaderTableSize = 40000;

        await InitializeConnectionAsync(_noopApplication, expectedSettingsCount: 5);

        // Update client settings
        _clientSettings.HeaderTableSize = 65536; // Chrome's default, larger than the 4kb spec default
        await SendSettingsAsync();

        // ACK
        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        // Start request
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headerFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 40,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        const byte DynamicTableSizeUpdateMask = 0xe0;

        var integerDecoder = new IntegerDecoder();
        Assert.False(integerDecoder.BeginTryDecode((byte)(headerFrame.Payload.Span[0] & ~DynamicTableSizeUpdateMask), prefixLength: 5, out _));
        Assert.False(integerDecoder.TryDecode(headerFrame.Payload.Span[1], out _));
        Assert.False(integerDecoder.TryDecode(headerFrame.Payload.Span[2], out _));
        Assert.True(integerDecoder.TryDecode(headerFrame.Payload.Span[3], out var result));

        Assert.Equal(40000, result);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task PUSH_PROMISE_Received_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendPushPromiseFrameAsync();

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorPushPromiseReceived);

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Fact]
    public async Task PING_Received_SendsACK()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendPingAsync(Http2PingFrameFlags.NONE);
        await ExpectAsync(Http2FrameType.PING,
            withLength: 8,
            withFlags: (byte)Http2PingFrameFlags.ACK,
            withStreamId: 0);

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task PING_Received_WithACK_DoesNotSendACK()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendPingAsync(Http2PingFrameFlags.ACK);

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task PING_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendPingAsync(Http2PingFrameFlags.NONE);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.PING, streamId: 0, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Fact]
    public async Task PING_Received_StreamIdNotZero_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendPingWithInvalidStreamIdAsync(streamId: 1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdNotZero(Http2FrameType.PING));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(9)]
    public async Task PING_Received_LengthNotEight_ConnectionError(int length)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendPingWithInvalidLengthAsync(length);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.PING, expectedLength: 8));

        AssertConnectionEndReason(ConnectionEndReason.InvalidFrameLength);
    }

    [Fact]
    public async Task GOAWAY_Received_ConnectionStops()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendGoAwayAsync();

        await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task GOAWAY_Received_ConnectionLifetimeNotification_Cancelled()
    {
        await InitializeConnectionAsync(_noopApplication, addKestrelFeatures: true);
        var lifetime = _connection.ConnectionFeatures.Get<IConnectionLifetimeNotificationFeature>();
        Assert.False(lifetime.ConnectionClosedRequested.IsCancellationRequested);

        await SendGoAwayAsync();

        Assert.True(lifetime.ConnectionClosedRequested.IsCancellationRequested);
        await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task GOAWAY_Received_SetsConnectionStateToClosingAndWaitForAllStreamsToComplete()
    {
        await InitializeConnectionAsync(_echoApplication);

        // Start some streams
        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);

        await SendGoAwayAsync();

        await _closingStateReached.Task.DefaultTimeout();

        await SendDataAsync(1, _helloBytes, true);
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);
        await SendDataAsync(3, _helloBytes, true);
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        TriggerTick();
        await WaitForConnectionStopAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
        await _closedStateReached.Task.DefaultTimeout();

        AssertConnectionNoError();
    }

    [Fact]
    public async Task GOAWAY_Received_ContinuesAppsAwaitingConnectionOutputFlowControl()
    {
        var writeTasks = new Task[6];
        var expectedFullFrameCountBeforeBackpressure = Http2PeerSettings.DefaultInitialWindowSize / _maxData.Length;
        var remainingBytesBeforeBackpressure = (int)Http2PeerSettings.DefaultInitialWindowSize % _maxData.Length;

        // Double the stream window to be 128KiB so it doesn't interfere with the rest of the test.
        _clientSettings.InitialWindowSize = Http2PeerSettings.DefaultInitialWindowSize * 2;

        await InitializeConnectionAsync(async context =>
        {
            var streamId = context.Features.Get<IHttp2StreamIdFeature>().StreamId;

            var abortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.RequestAborted.Register(() =>
            {
                lock (_abortedStreamIdsLock)
                {
                    _abortedStreamIds.Add(streamId);
                    abortedTcs.SetResult();
                }
            });

            try
            {
                writeTasks[streamId] = writeTcs.Task;

                // Flush headers even if the body can't yet be written because of flow control.
                await context.Response.Body.FlushAsync();

                for (var i = 0; i < expectedFullFrameCountBeforeBackpressure; i++)
                {
                    await context.Response.Body.WriteAsync(_maxData, 0, _maxData.Length);
                }

                await context.Response.Body.WriteAsync(_maxData, 0, remainingBytesBeforeBackpressure + 1);

                writeTcs.SetResult();

                await abortedTcs.Task;

                _runningStreams[streamId].SetResult();
            }
            catch (Exception ex)
            {
                _runningStreams[streamId].SetException(ex);
                throw;
            }
        });

        // Start one stream that consumes the entire connection output window.
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        for (var i = 0; i < expectedFullFrameCountBeforeBackpressure; i++)
        {
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
        }

        await ExpectAsync(Http2FrameType.DATA,
            withLength: remainingBytesBeforeBackpressure,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        Assert.False(writeTasks[1].IsCompleted);

        // Start two more streams that immediately experience backpressure.
        // The headers, but not the data for the stream, can still be sent.
        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);

        await StartStreamAsync(5, _browserRequestHeaders, endStream: true);
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 5);

        // Close all pipes and wait for response to drain
        _pair.Application.Output.Complete();
        _pair.Transport.Input.Complete();
        _pair.Transport.Output.Complete();

        await WaitForConnectionStopAsync(expectedLastStreamId: 5, ignoreNonGoAwayFrames: false);

        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);
        Assert.Contains(3, _abortedStreamIds);
        Assert.Contains(5, _abortedStreamIds);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task GOAWAY_Received_ContinuesAppsAwaitingStreamOutputFlowControl()
    {
        var writeTasks = new Task[6];
        var initialWindowSize = _helloWorldBytes.Length / 2;

        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = (uint)initialWindowSize;

        await InitializeConnectionAsync(async context =>
        {
            var streamId = context.Features.Get<IHttp2StreamIdFeature>().StreamId;

            var abortedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var writeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            context.RequestAborted.Register(() =>
            {
                lock (_abortedStreamIdsLock)
                {
                    _abortedStreamIds.Add(streamId);
                    abortedTcs.SetResult();
                }
            });

            try
            {
                writeTasks[streamId] = writeTcs.Task;
                await context.Response.Body.WriteAsync(_helloWorldBytes, 0, _helloWorldBytes.Length);
                writeTcs.SetResult();

                await abortedTcs.Task;

                _runningStreams[streamId].SetResult();
            }
            catch (Exception ex)
            {
                _runningStreams[streamId].SetException(ex);
                throw;
            }
        });

        async Task VerifyStreamBackpressure(int streamId, int headersLength)
        {
            await StartStreamAsync(streamId, _browserRequestHeaders, endStream: true);

            await ExpectAsync(Http2FrameType.HEADERS,
                withLength: headersLength,
                withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
                withStreamId: streamId);

            var dataFrame = await ExpectAsync(Http2FrameType.DATA,
                withLength: initialWindowSize,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: streamId);

            Assert.True(_helloWorldBytes.AsSpan(0, initialWindowSize).SequenceEqual(dataFrame.PayloadSequence.ToArray()));
            Assert.False(writeTasks[streamId].IsCompleted);
        }

        await VerifyStreamBackpressure(1, 32);
        await VerifyStreamBackpressure(3, 2);
        await VerifyStreamBackpressure(5, 2);

        // Close all pipes and wait for response to drain
        _pair.Application.Output.Complete();
        _pair.Transport.Input.Complete();
        _pair.Transport.Output.Complete();

        await WaitForConnectionStopAsync(expectedLastStreamId: 5, ignoreNonGoAwayFrames: false);

        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);
        Assert.Contains(3, _abortedStreamIds);
        Assert.Contains(5, _abortedStreamIds);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task GOAWAY_Received_StreamIdNotZero_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendInvalidGoAwayFrameAsync();

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdNotZero(Http2FrameType.GOAWAY));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task GOAWAY_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendGoAwayAsync();

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.GOAWAY, streamId: 0, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_StreamIdEven_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendWindowUpdateAsync(2, sizeIncrement: 42);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdEven(Http2FrameType.WINDOW_UPDATE, streamId: 2));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendWindowUpdateAsync(1, sizeIncrement: 42);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.WINDOW_UPDATE, streamId: 1, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(0, 5)]
    [InlineData(1, 3)]
    [InlineData(1, 5)]
    public async Task WINDOW_UPDATE_Received_LengthNotFour_ConnectionError(int streamId, int length)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendInvalidWindowUpdateAsync(streamId, sizeIncrement: 42, length: length);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.FRAME_SIZE_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(Http2FrameType.WINDOW_UPDATE, expectedLength: 4));

        AssertConnectionEndReason(ConnectionEndReason.InvalidFrameLength);
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_OnConnection_SizeIncrementZero_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendWindowUpdateAsync(0, sizeIncrement: 0);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorWindowUpdateIncrementZero);

        AssertConnectionEndReason(ConnectionEndReason.InvalidWindowUpdateSize);
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_OnStream_SizeIncrementZero_ConnectionError()
    {
        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
        await SendWindowUpdateAsync(1, sizeIncrement: 0);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorWindowUpdateIncrementZero);

        AssertConnectionEndReason(ConnectionEndReason.InvalidWindowUpdateSize);
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_StreamIdle_ConnectionError()
    {
        await InitializeConnectionAsync(_waitForAbortApplication);

        await SendWindowUpdateAsync(1, sizeIncrement: 1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdle(Http2FrameType.WINDOW_UPDATE, streamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_OnConnection_IncreasesWindowAboveMaxValue_ConnectionError()
    {
        var maxIncrement = (int)(Http2PeerSettings.MaxWindowSize - Http2PeerSettings.DefaultInitialWindowSize);

        await InitializeConnectionAsync(_noopApplication);

        await SendWindowUpdateAsync(0, sizeIncrement: maxIncrement);
        await SendWindowUpdateAsync(0, sizeIncrement: 1);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.FLOW_CONTROL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorWindowUpdateSizeInvalid);

        AssertConnectionEndReason(ConnectionEndReason.InvalidWindowUpdateSize);
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_OnStream_IncreasesWindowAboveMaxValue_StreamError()
    {
        var maxIncrement = (int)(Http2PeerSettings.MaxWindowSize - Http2PeerSettings.DefaultInitialWindowSize);

        await InitializeConnectionAsync(_waitForAbortApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
        await SendWindowUpdateAsync(1, sizeIncrement: maxIncrement);
        await SendWindowUpdateAsync(1, sizeIncrement: 1);

        await WaitForStreamErrorAsync(
            expectedStreamId: 1,
            expectedErrorCode: Http2ErrorCode.FLOW_CONTROL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorWindowUpdateSizeInvalid);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_OnConnection_Respected()
    {
        var expectedFullFrameCountBeforeBackpressure = Http2PeerSettings.DefaultInitialWindowSize / _maxData.Length;

        // Use this semaphore to wait until a new data frame is expected before trying to send it.
        // This way we're sure that if Response.Body.WriteAsync returns an incomplete task, it's because
        // of the flow control window and not Pipe backpressure.
        var expectingDataSem = new SemaphoreSlim(0);
        var backpressureObservedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var backpressureReleasedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Double the stream window to be 128KiB so it doesn't interfere with the rest of the test.
        _clientSettings.InitialWindowSize = Http2PeerSettings.DefaultInitialWindowSize * 2;

        await InitializeConnectionAsync(async context =>
        {
            try
            {
                // Flush the headers so expectingDataSem is released.
                await context.Response.Body.FlushAsync();

                for (var i = 0; i < expectedFullFrameCountBeforeBackpressure; i++)
                {
                    await expectingDataSem.WaitAsync();
                    await context.Response.Body.WriteAsync(_maxData, 0, _maxData.Length);
                }

                await expectingDataSem.WaitAsync();
                var lastWriteTask = context.Response.Body.WriteAsync(_maxData, 0, _maxData.Length);

                Assert.False(lastWriteTask.IsCompleted);
                backpressureObservedTcs.TrySetResult();

                await lastWriteTask;
                backpressureReleasedTcs.TrySetResult();
            }
            catch (Exception ex)
            {
                backpressureObservedTcs.TrySetException(ex);
                backpressureReleasedTcs.TrySetException(ex);
                throw;
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        for (var i = 0; i < expectedFullFrameCountBeforeBackpressure; i++)
        {
            expectingDataSem.Release();
            await ExpectAsync(Http2FrameType.DATA,
                withLength: _maxData.Length,
                withFlags: (byte)Http2DataFrameFlags.NONE,
                withStreamId: 1);
        }

        var remainingBytesBeforeBackpressure = (int)Http2PeerSettings.DefaultInitialWindowSize % _maxData.Length;
        var remainingBytesAfterBackpressure = _maxData.Length - remainingBytesBeforeBackpressure;

        expectingDataSem.Release();
        await ExpectAsync(Http2FrameType.DATA,
            withLength: remainingBytesBeforeBackpressure,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await backpressureObservedTcs.Task.DefaultTimeout();

        await SendWindowUpdateAsync(0, remainingBytesAfterBackpressure);

        await backpressureReleasedTcs.Task.DefaultTimeout();

        // This is the remaining data that could have come in the last frame if not for the flow control window,
        // so there's no need to release the semaphore again.
        await ExpectAsync(Http2FrameType.DATA,
            withLength: remainingBytesAfterBackpressure,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_OnStream_Respected()
    {
        var initialWindowSize = _helloWorldBytes.Length / 2;

        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = (uint)initialWindowSize;

        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _helloWorldBytes, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        var dataFrame1 = await ExpectAsync(Http2FrameType.DATA,
            withLength: initialWindowSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendWindowUpdateAsync(1, initialWindowSize);

        var dataFrame2 = await ExpectAsync(Http2FrameType.DATA,
            withLength: initialWindowSize,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        Assert.True(_helloWorldBytes.AsSpan(0, initialWindowSize).SequenceEqual(dataFrame1.PayloadSequence.ToArray()));
        Assert.True(_helloWorldBytes.AsSpan(initialWindowSize, initialWindowSize).SequenceEqual(dataFrame2.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task WINDOW_UPDATE_Received_OnStream_Respected_WhenInitialWindowSizeReducedMidStream()
    {
        // This only affects the stream windows. The connection-level window is always initialized at 64KiB.
        _clientSettings.InitialWindowSize = 6;

        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);
        await SendDataAsync(1, _helloWorldBytes, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);

        var dataFrame1 = await ExpectAsync(Http2FrameType.DATA,
            withLength: 6,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        // Reduce the initial window size for response data by 3 bytes.
        _clientSettings.InitialWindowSize = 3;
        await SendSettingsAsync();

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        await SendWindowUpdateAsync(1, 6);

        var dataFrame2 = await ExpectAsync(Http2FrameType.DATA,
            withLength: 3,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await SendWindowUpdateAsync(1, 3);

        var dataFrame3 = await ExpectAsync(Http2FrameType.DATA,
            withLength: 3,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        Assert.True(_helloWorldBytes.AsSpan(0, 6).SequenceEqual(dataFrame1.PayloadSequence.ToArray()));
        Assert.True(_helloWorldBytes.AsSpan(6, 3).SequenceEqual(dataFrame2.PayloadSequence.ToArray()));
        Assert.True(_helloWorldBytes.AsSpan(9, 3).SequenceEqual(dataFrame3.PayloadSequence.ToArray()));
    }

    [Fact]
    public async Task CONTINUATION_Received_Decoded()
    {
        await InitializeConnectionAsync(_readHeadersApplication);

        await StartStreamAsync(1, _twoContinuationsRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        VerifyDecodedRequestHeaders(_twoContinuationsRequestHeaders);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CONTINUATION_Received_WithTrailers_Available(bool sendData)
    {
        await InitializeConnectionAsync(_readTrailersApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);

        // Initialize another stream with a higher stream ID, and verify that after trailers are
        // decoded by the other stream, the highest opened stream ID is not reset to the lower ID
        // (the highest opened stream ID is sent by the server in the GOAWAY frame when shutting
        // down the connection).
        await SendHeadersAsync(3, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

        // The second stream should end first, since the first one is waiting for the request body.
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 3);

        if (sendData)
        {
            await SendDataAsync(1, _helloBytes, endStream: false);
        }

        // Trailers encoded as Literal Header Field without Indexing - New Name
        //   trailer-1: 1
        //   trailer-2: 2
        var trailers = new byte[] { 0x00, 0x09 }
            .Concat(Encoding.ASCII.GetBytes("trailer-1"))
            .Concat(new byte[] { 0x01, (byte)'1' })
            .Concat(new byte[] { 0x00, 0x09 })
            .Concat(Encoding.ASCII.GetBytes("trailer-2"))
            .Concat(new byte[] { 0x01, (byte)'2' })
            .ToArray();
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, new byte[0]);
        await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, trailers);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 6,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        VerifyDecodedRequestHeaders(_browserRequestHeaders);

        // Make sure the trailers are in the trailers collection.
        Assert.False(_receivedHeaders.ContainsKey("trailer-1"));
        Assert.False(_receivedHeaders.ContainsKey("trailer-2"));
        Assert.True(_receivedTrailers.ContainsKey("trailer-1"));
        Assert.True(_receivedTrailers.ContainsKey("trailer-2"));
        Assert.Equal("1", _receivedTrailers["trailer-1"]);
        Assert.Equal("2", _receivedTrailers["trailer-2"]);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);
    }

    [Fact]
    public async Task CONTINUATION_Received_StreamIdMismatch_ConnectionError()
    {
        await InitializeConnectionAsync(_readHeadersApplication);

        var headersEnumerator = GetHeadersEnumerator(_oneContinuationRequestHeaders);
        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, headersEnumerator);
        await SendContinuationAsync(3, Http2ContinuationFrameFlags.END_HEADERS, headersEnumerator);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(Http2FrameType.CONTINUATION, streamId: 3, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Fact]
    public async Task CONTINUATION_Received_IncompleteHeaderBlock_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _postRequestHeaders);
        await SendIncompleteContinuationFrameAsync(streamId: 1);

        await WaitForConnectionErrorAsync<HPackDecodingException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.COMPRESSION_ERROR,
            expectedErrorMessage: SR.net_http_hpack_incomplete_header_block);

        AssertConnectionEndReason(ConnectionEndReason.ErrorReadingHeaders);
    }

    [Theory]
    [MemberData(nameof(IllegalTrailerData))]
    public async Task CONTINUATION_Received_WithTrailers_ContainsIllegalTrailer_ConnectionError(byte[] trailers, string expectedErrorMessage)
    {
        await InitializeConnectionAsync(_readTrailersApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS, _browserRequestHeaders);
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, new byte[0]);
        await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, trailers);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: expectedErrorMessage);

        AssertConnectionEndReason(ConnectionEndReason.InvalidRequestHeaders);
    }

    [Theory]
    [MemberData(nameof(MissingPseudoHeaderFieldData))]
    public async Task CONTINUATION_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_StreamError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        await InitializeConnectionAsync(_noopApplication);

        Assert.True(await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, headers));
        await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.END_HEADERS);

        await WaitForStreamErrorAsync(
            expectedStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.HttpErrorMissingMandatoryPseudoHeaderFields);

        // Verify that the stream ID can't be re-used
        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, headers);
        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Theory]
    [MemberData(nameof(ConnectMissingPseudoHeaderFieldData))]
    public async Task CONTINUATION_Received_HeaderBlockDoesNotContainMandatoryPseudoHeaderField_MethodIsCONNECT_NoError(IEnumerable<KeyValuePair<string, string>> headers)
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, headers);
        await SendEmptyContinuationFrameAsync(1, Http2ContinuationFrameFlags.END_HEADERS);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task CONTINUATION_Sent_WhenHeadersLargerThanFrameLength()
    {
        await InitializeConnectionAsync(_largeHeadersApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var headersFrame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 12342,
            withFlags: (byte)Http2HeadersFrameFlags.END_STREAM,
            withStreamId: 1);
        var continuationFrame1 = await ExpectAsync(Http2FrameType.CONTINUATION,
            withLength: 12306,
            withFlags: (byte)Http2ContinuationFrameFlags.NONE,
            withStreamId: 1);
        var continuationFrame2 = await ExpectAsync(Http2FrameType.CONTINUATION,
            withLength: 8204,
            withFlags: (byte)Http2ContinuationFrameFlags.END_HEADERS,
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        _hpackDecoder.Decode(headersFrame.PayloadSequence, endHeaders: false, handler: this);
        _hpackDecoder.Decode(continuationFrame1.PayloadSequence, endHeaders: false, handler: this);
        _hpackDecoder.Decode(continuationFrame2.PayloadSequence, endHeaders: true, handler: this);

        Assert.Equal(11, _decodedHeaders.Count);
        Assert.Contains("date", _decodedHeaders.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("200", _decodedHeaders[InternalHeaderNames.Status]);
        Assert.Equal("0", _decodedHeaders["content-length"]);
        Assert.Equal(_4kHeaderValue, _decodedHeaders["a"]);
        Assert.Equal(_4kHeaderValue, _decodedHeaders["b"]);
        Assert.Equal(_4kHeaderValue, _decodedHeaders["c"]);
        Assert.Equal(_4kHeaderValue, _decodedHeaders["d"]);
        Assert.Equal(_4kHeaderValue, _decodedHeaders["e"]);
        Assert.Equal(_4kHeaderValue, _decodedHeaders["f"]);
        Assert.Equal(_4kHeaderValue, _decodedHeaders["g"]);
        Assert.Equal(_4kHeaderValue, _decodedHeaders["h"]);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task UnknownFrameType_Received_Ignored()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendUnknownFrameTypeAsync(streamId: 1, frameType: 42);

        // Check that the connection is still alive
        await SendPingAsync(Http2PingFrameFlags.NONE);
        await ExpectAsync(Http2FrameType.PING,
            withLength: 8,
            withFlags: (byte)Http2PingFrameFlags.ACK,
            withStreamId: 0);

        await StopConnectionAsync(0, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task UnknownFrameType_Received_InterleavedWithHeaders_ConnectionError()
    {
        await InitializeConnectionAsync(_noopApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.NONE, _browserRequestHeaders);
        await SendUnknownFrameTypeAsync(streamId: 1, frameType: 42);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorHeadersInterleaved(frameType: 42, streamId: 1, headersStreamId: 1));

        AssertConnectionEndReason(ConnectionEndReason.UnexpectedFrame);
    }

    [Fact]
    public async Task ConnectionErrorAbortsAllStreams()
    {
        await InitializeConnectionAsync(_waitForAbortApplication);

        // Start some streams
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);
        await StartStreamAsync(3, _browserRequestHeaders, endStream: true);
        await StartStreamAsync(5, _browserRequestHeaders, endStream: true);

        // Cause a connection error by sending an invalid frame
        await SendDataAsync(0, _noData, endStream: false);

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 5,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.FormatHttp2ErrorStreamIdZero(Http2FrameType.DATA));

        await WaitForAllStreamsAsync();
        Assert.Contains(1, _abortedStreamIds);
        Assert.Contains(3, _abortedStreamIds);
        Assert.Contains(5, _abortedStreamIds);

        AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
    }

    [Fact]
    public async Task ConnectionResetLoggedWithActiveStreams()
    {
        await InitializeConnectionAsync(_waitForAbortApplication);

        await SendHeadersAsync(1, Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM, _browserRequestHeaders);

        _pair.Application.Output.Complete(new ConnectionResetException(string.Empty));

        await StopConnectionAsync(1, ignoreNonGoAwayFrames: false);
        Assert.Single(LogMessages, m => m.Exception is ConnectionResetException);

        AssertConnectionEndReason(ConnectionEndReason.ConnectionReset);
    }

    [Fact]
    public async Task ConnectionResetNotLoggedWithNoActiveStreams()
    {
        await InitializeConnectionAsync(_waitForAbortApplication);

        _pair.Application.Output.Complete(new ConnectionResetException(string.Empty));

        await WaitForConnectionStopAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        Assert.DoesNotContain(LogMessages, m => m.Exception is ConnectionResetException);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task OnInputOrOutputCompletedCompletesOutput()
    {
        await InitializeConnectionAsync(_noopApplication);

        _connection.OnInputOrOutputCompleted();
        await _closedStateReached.Task.DefaultTimeout();

        var result = await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();
        Assert.True(result.IsCompleted);
        Assert.True(result.Buffer.IsEmpty);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task AbortSendsFinalGOAWAY()
    {
        await InitializeConnectionAsync(_noopApplication);

        _connection.Abort(new ConnectionAbortedException(), ConnectionEndReason.AbortedByApp);
        await _closedStateReached.Task.DefaultTimeout();

        VerifyGoAway(await ReceiveFrameAsync(), int.MaxValue, Http2ErrorCode.INTERNAL_ERROR);

        AssertConnectionEndReason(ConnectionEndReason.AbortedByApp);
    }

    [Fact]
    public async Task CompletionSendsFinalGOAWAY()
    {
        await InitializeConnectionAsync(_noopApplication);

        // Completes ProcessRequestsAsync
        _pair.Application.Output.Complete();
        await _closedStateReached.Task.DefaultTimeout();

        VerifyGoAway(await ReceiveFrameAsync(), 0, Http2ErrorCode.NO_ERROR);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task StopProcessingNextRequestSendsGracefulGOAWAYAndWaitsForStreamsToComplete()
    {
        var task = Task.CompletedTask;
        await InitializeConnectionAsync(context => task);

        // Send and receive an unblocked request
        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        // Send a blocked request
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        task = tcs.Task;
        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);

        // Close pipe
        _pair.Application.Output.Complete();

        // Assert connection closed
        await _closedStateReached.Task.DefaultTimeout();
        VerifyGoAway(await ReceiveFrameAsync(), 3, Http2ErrorCode.NO_ERROR);

        // Assert connection shutdown is still blocked
        // ProcessRequestsAsync completes the connection's Input pipe
        var readTask = _pair.Application.Input.ReadAsync();
        _pair.Application.Input.CancelPendingRead();
        var result = await readTask;
        Assert.False(result.IsCompleted);

        // Unblock the request and ProcessRequestsAsync
        tcs.TrySetResult();
        await _connectionTask;

        // Assert connection's Input pipe is completed
        readTask = _pair.Application.Input.ReadAsync();
        _pair.Application.Input.CancelPendingRead();
        result = await readTask;
        Assert.True(result.IsCompleted);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task StopProcessingNextRequestSendsGracefulGOAWAYThenFinalGOAWAYWhenAllStreamsComplete()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        _connection.StopProcessingNextRequest(ConnectionEndReason.AppShutdownTimeout);
        await _closingStateReached.Task.DefaultTimeout();

        VerifyGoAway(await ReceiveFrameAsync(), Int32.MaxValue, Http2ErrorCode.NO_ERROR);

        await SendDataAsync(1, _helloBytes, true);
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);

        TriggerTick();
        await _closedStateReached.Task.DefaultTimeout();
        VerifyGoAway(await ReceiveFrameAsync(), 1, Http2ErrorCode.NO_ERROR);

        AssertConnectionEndReason(ConnectionEndReason.AppShutdownTimeout);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/41172")]
    public async Task AcceptNewStreamsDuringClosingConnection()
    {
        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        _connection.StopProcessingNextRequest(ConnectionEndReason.AppShutdownTimeout);
        VerifyGoAway(await ReceiveFrameAsync(), Int32.MaxValue, Http2ErrorCode.NO_ERROR);

        await _closingStateReached.Task.DefaultTimeout();

        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);

        await SendDataAsync(1, _helloBytes, true);
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 1);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 1);
        await SendDataAsync(3, _helloBytes, true);
        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 2,
            withFlags: (byte)Http2HeadersFrameFlags.END_HEADERS,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 5,
            withFlags: (byte)Http2DataFrameFlags.NONE,
            withStreamId: 3);
        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)Http2DataFrameFlags.END_STREAM,
            withStreamId: 3);

        TriggerTick();

        await WaitForConnectionStopAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: false);

        AssertConnectionEndReason(ConnectionEndReason.AppShutdownTimeout);
    }

    [Fact]
    public async Task IgnoreNewStreamsDuringClosedConnection()
    {
        // Remove callback that completes _pair.Application.Output on abort.
        _mockConnectionContext.Reset();

        await InitializeConnectionAsync(_echoApplication);

        await StartStreamAsync(1, _browserRequestHeaders, endStream: false);

        _connection.OnInputOrOutputCompleted();
        await _closedStateReached.Task.DefaultTimeout();

        await StartStreamAsync(3, _browserRequestHeaders, endStream: false);

        var result = await _pair.Application.Input.ReadAsync().AsTask().DefaultTimeout();
        Assert.True(result.IsCompleted);
        Assert.True(result.Buffer.IsEmpty);

        AssertConnectionEndReason(ConnectionEndReason.ConnectionReset);
    }

    [Fact]
    public async Task IOExceptionDuringFrameProcessingIsNotLoggedHigherThanDebug()
    {
        CreateConnection();

        var ioException = new IOException();
        _pair.Application.Output.Complete(ioException);

        await _connection.ProcessRequestsAsync(new DummyApplication(_noopApplication)).DefaultTimeout();

        Assert.All(LogMessages, w => Assert.InRange(w.LogLevel, LogLevel.Trace, LogLevel.Debug));

        var logMessage = LogMessages.Single(m => m.EventId == 20);

        Assert.Equal("Connection id \"TestConnectionId\" request processing ended abnormally.", logMessage.Message);
        Assert.Same(ioException, logMessage.Exception);

        AssertConnectionEndReason(ConnectionEndReason.IOError);
    }

    [Fact]
    public async Task UnexpectedExceptionDuringFrameProcessingLoggedAWarning()
    {
        CreateConnection();

        var exception = new Exception();
        _pair.Application.Output.Complete(exception);

        await _connection.ProcessRequestsAsync(new DummyApplication(_noopApplication)).DefaultTimeout();

        var logMessage = LogMessages.Single(m => m.LogLevel >= LogLevel.Information);

        Assert.Equal(LogLevel.Warning, logMessage.LogLevel);
        Assert.Equal(CoreStrings.RequestProcessingEndError, logMessage.Message);
        Assert.Same(exception, logMessage.Exception);

        AssertConnectionEndReason(ConnectionEndReason.OtherError);
    }

    [Theory]
    [InlineData((int)(Http2FrameType.DATA))]
    [InlineData((int)(Http2FrameType.WINDOW_UPDATE))]
    [InlineData((int)(Http2FrameType.HEADERS))]
    [InlineData((int)(Http2FrameType.CONTINUATION))]
    public async Task AppDoesNotReadRequestBody_ResetsAndDrainsRequest(int intFinalFrameType)
    {
        var finalFrameType = (Http2FrameType)intFinalFrameType;

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(_noopApplication);

        await StartStreamAsync(1, headers, endStream: false);

        await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 36,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS | Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.NO_ERROR, null);
        // Logged without an exception.
        Assert.Contains(LogMessages, m => m.Message.Contains("the application completed without reading the entire request body."));

        // These would be refused if the cool-down period had expired
        switch (finalFrameType)
        {
            case Http2FrameType.DATA:
                await SendDataAsync(1, new byte[100], endStream: true);
                break;
            case Http2FrameType.WINDOW_UPDATE:
                await SendWindowUpdateAsync(1, 1024);
                break;
            case Http2FrameType.HEADERS:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS, _requestTrailers);
                break;
            case Http2FrameType.CONTINUATION:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _requestTrailers);
                await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, _requestTrailers);
                break;
            default:
                throw new NotImplementedException(finalFrameType.ToString());
        }

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Theory]
    [InlineData((int)(Http2FrameType.DATA))]
    [InlineData((int)(Http2FrameType.WINDOW_UPDATE))]
    [InlineData((int)(Http2FrameType.HEADERS))]
    [InlineData((int)(Http2FrameType.CONTINUATION))]
    public async Task AbortedStream_ResetsAndDrainsRequest(int intFinalFrameType)
    {
        var finalFrameType = (Http2FrameType)intFinalFrameType;

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(_appAbort);

        await StartStreamAsync(1, headers, endStream: false);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, "The connection was aborted by the application.");

        // These would be refused if the cool-down period had expired
        switch (finalFrameType)
        {
            case Http2FrameType.DATA:
                await SendDataAsync(1, new byte[100], endStream: true);
                break;
            case Http2FrameType.WINDOW_UPDATE:
                await SendWindowUpdateAsync(1, 1024);
                break;
            case Http2FrameType.HEADERS:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS, _requestTrailers);
                break;
            case Http2FrameType.CONTINUATION:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _requestTrailers);
                await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, _requestTrailers);
                break;
            default:
                throw new NotImplementedException(finalFrameType.ToString());
        }

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Theory]
    [InlineData((int)(Http2FrameType.DATA))]
    [InlineData((int)(Http2FrameType.WINDOW_UPDATE))]
    [InlineData((int)(Http2FrameType.HEADERS))]
    [InlineData((int)(Http2FrameType.CONTINUATION))]
    public async Task ResetStream_ResetsAndDrainsRequest(int intFinalFrameType)
    {
        var finalFrameType = (Http2FrameType)intFinalFrameType;

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(_appReset);

        await StartStreamAsync(1, headers, endStream: false);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.CANCEL, "The HTTP/2 stream was reset by the application with error code CANCEL.");

        // These would be refused if the cool-down period had expired
        switch (finalFrameType)
        {
            case Http2FrameType.DATA:
                await SendDataAsync(1, new byte[100], endStream: true);
                break;
            case Http2FrameType.WINDOW_UPDATE:
                await SendWindowUpdateAsync(1, 1024);
                break;
            case Http2FrameType.HEADERS:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS, _requestTrailers);
                break;
            case Http2FrameType.CONTINUATION:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _requestTrailers);
                await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, _requestTrailers);
                break;
            default:
                throw new NotImplementedException(finalFrameType.ToString());
        }

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Theory]
    [InlineData((int)(Http2FrameType.DATA))]
    [InlineData((int)(Http2FrameType.WINDOW_UPDATE))]
    [InlineData((int)(Http2FrameType.HEADERS))]
    [InlineData((int)(Http2FrameType.CONTINUATION))]
    public async Task RefusedStream_Post_ResetsAndDrainsRequest(int intFinalFrameType)
    {
        var finalFrameType = (Http2FrameType)intFinalFrameType;

        CreateConnection();

        _connection.ServerSettings.MaxConcurrentStreams = 0; // Refuse all streams

        var connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(_noopApplication));

        async Task CompletePipeOnTaskCompletion()
        {
            try
            {
                await connectionTask;
            }
            finally
            {
                _pair.Transport.Input.Complete();
                _pair.Transport.Output.Complete();
            }
        }

        _connectionTask = CompletePipeOnTaskCompletion();

        await SendPreambleAsync().ConfigureAwait(false);
        await SendSettingsAsync();

        // Requests can be sent before receiving and acking settings.

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        await StartStreamAsync(1, headers, endStream: false);

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 4 * Http2FrameReader.SettingSize,
            withFlags: 0,
            withStreamId: 0);

        await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: 0,
            withStreamId: 0);

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.REFUSED_STREAM, "HTTP/2 stream ID 1 error (REFUSED_STREAM): A new stream was refused because this connection has reached its stream limit.");

        // These frames should be drained and ignored while in cool-down mode.
        switch (finalFrameType)
        {
            case Http2FrameType.DATA:
                await SendDataAsync(1, new byte[100], endStream: true);
                break;
            case Http2FrameType.WINDOW_UPDATE:
                await SendWindowUpdateAsync(1, 1024);
                break;
            case Http2FrameType.HEADERS:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS, _requestTrailers);
                break;
            case Http2FrameType.CONTINUATION:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _requestTrailers);
                await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, _requestTrailers);
                break;
            default:
                throw new NotImplementedException(finalFrameType.ToString());
        }

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task RefusedStream_Post_2xLimitRefused()
    {
        var requestBlock = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        CreateConnection();

        _connection.ServerSettings.MaxConcurrentStreams = 1;

        var connectionTask = _connection.ProcessRequestsAsync(new DummyApplication(_ => requestBlock.Task));

        async Task CompletePipeOnTaskCompletion()
        {
            try
            {
                await connectionTask;
            }
            finally
            {
                _pair.Transport.Input.Complete();
                _pair.Transport.Output.Complete();
            }
        }

        _connectionTask = CompletePipeOnTaskCompletion();

        await SendPreambleAsync().ConfigureAwait(false);
        await SendSettingsAsync();

        // Requests can be sent before receiving and acking settings.

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        // This mimics gRPC, sending headers and data close together before receiving a reset.
        await StartStreamAsync(1, headers, endStream: false);
        await SendDataAsync(1, new byte[100], endStream: false);
        await StartStreamAsync(3, headers, endStream: false);
        await SendDataAsync(3, new byte[100], endStream: false);
        await StartStreamAsync(5, headers, endStream: false);
        await SendDataAsync(5, new byte[100], endStream: false);
        await StartStreamAsync(7, headers, endStream: false);
        await SendDataAsync(7, new byte[100], endStream: false);

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 4 * Http2FrameReader.SettingSize,
            withFlags: 0,
            withStreamId: 0);

        await ExpectAsync(Http2FrameType.WINDOW_UPDATE,
            withLength: 4,
            withFlags: 0,
            withStreamId: 0);

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 0,
            withFlags: (byte)Http2SettingsFrameFlags.ACK,
            withStreamId: 0);

        await WaitForStreamErrorAsync(3, Http2ErrorCode.REFUSED_STREAM, "HTTP/2 stream ID 3 error (REFUSED_STREAM): A new stream was refused because this connection has reached its stream limit.");
        await WaitForStreamErrorAsync(5, Http2ErrorCode.REFUSED_STREAM, "HTTP/2 stream ID 5 error (REFUSED_STREAM): A new stream was refused because this connection has reached its stream limit.");
        await WaitForStreamErrorAsync(7, Http2ErrorCode.REFUSED_STREAM, "HTTP/2 stream ID 7 error (REFUSED_STREAM): A new stream was refused because this connection has reached its stream limit.");
        requestBlock.SetResult();
        await StopConnectionAsync(expectedLastStreamId: 7, ignoreNonGoAwayFrames: true);

        AssertConnectionNoError();
    }

    [Fact]
    public async Task FramesInBatchAreStillProcessedAfterStreamError_WithoutHeartbeat()
    {
        // Previously, if there was a stream error, frame processing would stop and wait for either
        // the heartbeat or more data before continuing frame processing. This is testing that frames
        // continue to be processed if they were in the same read.

        CreateConnection();
        _connection.ServerSettings.MaxConcurrentStreams = 1;

        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };

        await InitializeConnectionAsync(async context =>
        {
            var buffer = new byte[2];
            var result = await context.Request.BodyReader.ReadAsync().DefaultTimeout();
            Assert.True(result.IsCompleted);
            result.Buffer.CopyTo(buffer);
            context.Request.BodyReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            tcs.SetResult(buffer);
        });

        var streamPayload = new byte[2] { 42, 24 };
        await StartStreamAsync(1, headers, endStream: false);
        // Send these 2 frames in a batch with the error in the first frame
        await StartStreamAsync(3, headers, endStream: false, flushFrame: false);
        await SendDataAsync(1, streamPayload, endStream: true);

        await WaitForStreamErrorAsync(3, Http2ErrorCode.REFUSED_STREAM, CoreStrings.Http2ErrorMaxStreams);

        var streamResponse = await tcs.Task.DefaultTimeout();
        Assert.Equal(streamPayload, streamResponse);

        await StopConnectionAsync(expectedLastStreamId: 3, ignoreNonGoAwayFrames: true);
        AssertConnectionNoError();
    }

    [Theory]
    [InlineData((int)(Http2FrameType.DATA))]
    [InlineData((int)(Http2FrameType.HEADERS))]
    [InlineData((int)(Http2FrameType.CONTINUATION))]
    public async Task AbortedStream_ResetsAndDrainsRequest_RefusesFramesAfterEndOfStream(int intFinalFrameType)
    {
        var finalFrameType = (Http2FrameType)intFinalFrameType;

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(_appAbort);

        await StartStreamAsync(1, headers, endStream: false);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, "The connection was aborted by the application.");

        switch (finalFrameType)
        {
            case Http2FrameType.DATA:
                await SendDataAsync(1, new byte[100], endStream: true);
                // An extra one to break it
                await SendDataAsync(1, new byte[100], endStream: true);

                // There's a race where either of these messages could be logged, depending on if the stream cleanup has finished yet.
                await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                    ignoreNonGoAwayFrames: false,
                    expectedLastStreamId: 1,
                    expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                    expectedErrorMessage: new[] {
                            CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.DATA, streamId: 1),
                            CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.DATA, streamId: 1)
                    });
                AssertConnectionEndReason(ConnectionEndReason.UnknownStream);
                break;

            case Http2FrameType.HEADERS:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS, _requestTrailers);
                // An extra one to break it
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS, _requestTrailers);

                // There's a race where either of these messages could be logged, depending on if the stream cleanup has finished yet.
                await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                    ignoreNonGoAwayFrames: false,
                    expectedLastStreamId: 1,
                    expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                    expectedErrorMessage: new[] {
                            CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1),
                            CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.HEADERS, streamId: 1)
                    });
                AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
                break;

            case Http2FrameType.CONTINUATION:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _requestTrailers);
                await SendContinuationAsync(1, Http2ContinuationFrameFlags.END_HEADERS, _requestTrailers);
                // An extra one to break it. It's not a Continuation because that would fail with an error that no headers were in progress.
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM, _requestTrailers);

                // There's a race where either of these messages could be logged, depending on if the stream cleanup has finished yet.
                await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
                    ignoreNonGoAwayFrames: false,
                    expectedLastStreamId: 1,
                    expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
                    expectedErrorMessage: new[] {
                            CoreStrings.FormatHttp2ErrorStreamClosed(Http2FrameType.HEADERS, streamId: 1),
                            CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(Http2FrameType.HEADERS, streamId: 1)
                    });
                AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
                break;
            default:
                throw new NotImplementedException(finalFrameType.ToString());
        }
    }

    [Theory]
    [InlineData((int)(Http2FrameType.DATA))]
    [InlineData((int)(Http2FrameType.HEADERS))]
    public async Task AbortedStream_ResetsAndDrainsRequest_RefusesFramesAfterClientReset(int intFinalFrameType)
    {
        var finalFrameType = (Http2FrameType)intFinalFrameType;

        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        };
        await InitializeConnectionAsync(_appAbort);

        await StartStreamAsync(1, headers, endStream: false);

        await WaitForStreamErrorAsync(1, Http2ErrorCode.INTERNAL_ERROR, "The connection was aborted by the application.");

        await SendRstStreamAsync(1);

        // Send an extra frame to make it fail
        switch (finalFrameType)
        {
            case Http2FrameType.DATA:
                await SendDataAsync(1, new byte[100], endStream: true);
                break;

            case Http2FrameType.HEADERS:
                await SendHeadersAsync(1, Http2HeadersFrameFlags.END_STREAM | Http2HeadersFrameFlags.END_HEADERS, _requestTrailers);
                break;

            default:
                throw new NotImplementedException(finalFrameType.ToString());
        }

        // There's a race where either of these messages could be logged, depending on if the stream cleanup has finished yet.
        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 1,
            expectedErrorCode: Http2ErrorCode.STREAM_CLOSED,
            expectedErrorMessage: new[] {
                    CoreStrings.FormatHttp2ErrorStreamClosed(finalFrameType, streamId: 1),
                    CoreStrings.FormatHttp2ErrorStreamAborted(finalFrameType, streamId: 1)
            });

        switch (finalFrameType)
        {
            case Http2FrameType.DATA:
                AssertConnectionEndReason(ConnectionEndReason.UnknownStream);
                break;

            case Http2FrameType.HEADERS:
                AssertConnectionEndReason(ConnectionEndReason.InvalidStreamId);
                break;

            default:
                throw new NotImplementedException(finalFrameType.ToString());
        }
    }

    [Fact]
    public async Task StartConnection_SendPreface_ReturnSettings()
    {
        InitializeConnectionWithoutPreface(_noopApplication);

        await SendAsync(Http2Connection.ClientPreface);

        await ExpectAsync(Http2FrameType.SETTINGS,
            withLength: 4 * Http2FrameReader.SettingSize,
            withFlags: 0,
            withStreamId: 0);

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: true);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task StartConnection_SendHttp1xRequest_ReturnHttp11Status400()
    {
        InitializeConnectionWithoutPreface(_noopApplication);

        await SendAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n"));

        var data = await ReadAllAsync();

        Assert.NotNull(Http2Connection.InvalidHttp1xErrorResponseBytes);
        Assert.Equal(Http2Connection.InvalidHttp1xErrorResponseBytes, data);
        AssertConnectionEndReason(ConnectionEndReason.InvalidHttpVersion);
    }

    [Fact]
    public async Task StartConnection_SendHttp1xRequest_ExceedsRequestLineLimit_ProtocolError()
    {
        InitializeConnectionWithoutPreface(_noopApplication);

        await SendAsync(Encoding.ASCII.GetBytes($"GET /{new string('a', _connection.Limits.MaxRequestLineSize)} HTTP/1.1\r\n"));

        await WaitForConnectionErrorAsync<Http2ConnectionErrorException>(
            ignoreNonGoAwayFrames: false,
            expectedLastStreamId: 0,
            expectedErrorCode: Http2ErrorCode.PROTOCOL_ERROR,
            expectedErrorMessage: CoreStrings.Http2ErrorInvalidPreface);
        AssertConnectionEndReason(ConnectionEndReason.InvalidHandshake);
    }

    [Fact]
    public async Task StartTlsConnection_SendHttp1xRequest_NoError()
    {
        CreateConnection();

        var tlsHandshakeMock = new Mock<ITlsHandshakeFeature>();
        tlsHandshakeMock.SetupGet(m => m.Protocol).Returns(SslProtocols.Tls12);
        _connection.ConnectionFeatures.Set<ITlsHandshakeFeature>(tlsHandshakeMock.Object);

        InitializeConnectionWithoutPreface(_noopApplication);

        await SendAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n"));

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task StartConnection_SendNothing_NoError()
    {
        InitializeConnectionWithoutPreface(_noopApplication);

        await StopConnectionAsync(expectedLastStreamId: 0, ignoreNonGoAwayFrames: false);
        AssertConnectionNoError();
    }

    [Fact]
    public async Task WriteBeforeFlushingHeadersTracksBytesCorrectly()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await InitializeConnectionAsync(async c =>
        {
            try
            {
                var length = 0;
                var memory = c.Response.BodyWriter.GetMemory();
                c.Response.BodyWriter.Advance(memory.Length);
                length += memory.Length;
                Assert.Equal(length, c.Response.BodyWriter.UnflushedBytes);

                memory = c.Response.BodyWriter.GetMemory();
                c.Response.BodyWriter.Advance(memory.Length);
                length += memory.Length;

                Assert.Equal(length, c.Response.BodyWriter.UnflushedBytes);

                await c.Response.BodyWriter.FlushAsync();

                Assert.Equal(0, c.Response.BodyWriter.UnflushedBytes);

                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var frame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);

        frame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 8192,
            withFlags: (byte)(Http2HeadersFrameFlags.NONE),
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)(Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        await tcs.Task;
    }

    [Fact]
    public async Task WriteAfterFlushingHeadersTracksBytesCorrectly()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await InitializeConnectionAsync(async c =>
        {
            try
            {
                await c.Response.StartAsync();

                var length = 0;
                var memory = c.Response.BodyWriter.GetMemory();
                c.Response.BodyWriter.Advance(memory.Length);
                length += memory.Length;
                Assert.Equal(length, c.Response.BodyWriter.UnflushedBytes);

                memory = c.Response.BodyWriter.GetMemory();
                c.Response.BodyWriter.Advance(memory.Length);
                length += memory.Length;

                Assert.Equal(length, c.Response.BodyWriter.UnflushedBytes);

                await c.Response.BodyWriter.FlushAsync();

                Assert.Equal(0, c.Response.BodyWriter.UnflushedBytes);

                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await StartStreamAsync(1, _browserRequestHeaders, endStream: true);

        var frame = await ExpectAsync(Http2FrameType.HEADERS,
            withLength: 32,
            withFlags: (byte)(Http2HeadersFrameFlags.END_HEADERS),
            withStreamId: 1);

        frame = await ExpectAsync(Http2FrameType.DATA,
            withLength: 8192,
            withFlags: (byte)(Http2HeadersFrameFlags.NONE),
            withStreamId: 1);

        await ExpectAsync(Http2FrameType.DATA,
            withLength: 0,
            withFlags: (byte)(Http2HeadersFrameFlags.END_STREAM),
            withStreamId: 1);

        await StopConnectionAsync(expectedLastStreamId: 1, ignoreNonGoAwayFrames: false);

        await tcs.Task;
    }

    public static TheoryData<byte[]> UpperCaseHeaderNameData
    {
        get
        {
            // We can't use HPackEncoder here because it will convert header names to lowercase
            var headerName = "abcdefghijklmnopqrstuvwxyz";

            var headerBlockStart = new byte[]
            {
                    0x82,                    // Indexed Header Field - :method: GET
                    0x84,                    // Indexed Header Field - :path: /
                    0x86,                    // Indexed Header Field - :scheme: http
                    0x00,                    // Literal Header Field without Indexing - New Name
                    (byte)headerName.Length, // Header name length
            };

            var headerBlockEnd = new byte[]
            {
                    0x01, // Header value length
                    0x30  // "0"
            };

            var data = new TheoryData<byte[]>();

            for (var i = 0; i < headerName.Length; i++)
            {
                var bytes = Encoding.ASCII.GetBytes(headerName);
                bytes[i] &= 0xdf;

                var headerBlock = headerBlockStart.Concat(bytes).Concat(headerBlockEnd).ToArray();
                data.Add(headerBlock);
            }

            return data;
        }
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

    public static TheoryData<byte[], string> IllegalTrailerData
    {
        get
        {
            // We can't use HPackEncoder here because it will convert header names to lowercase
            var data = new TheoryData<byte[], string>();

            // Indexed Header Field - :method: GET
            data.Add(new byte[] { 0x82 }, CoreStrings.HttpErrorTrailersContainPseudoHeaderField);

            // Indexed Header Field - :path: /
            data.Add(new byte[] { 0x84 }, CoreStrings.HttpErrorTrailersContainPseudoHeaderField);

            // Indexed Header Field - :scheme: http
            data.Add(new byte[] { 0x86 }, CoreStrings.HttpErrorTrailersContainPseudoHeaderField);

            // Literal Header Field without Indexing - Indexed Name - :authority: 127.0.0.1
            data.Add(new byte[] { 0x01, 0x09 }.Concat(Encoding.ASCII.GetBytes("127.0.0.1")).ToArray(), CoreStrings.HttpErrorTrailersContainPseudoHeaderField);

            // Literal Header Field without Indexing - New Name - contains-Uppercase: 0
            data.Add(new byte[] { 0x00, 0x12 }
                .Concat(Encoding.ASCII.GetBytes("contains-Uppercase"))
                .Concat(new byte[] { 0x01, (byte)'0' })
                .ToArray(), CoreStrings.HttpErrorTrailerNameUppercase);

            return data;
        }
    }
}
