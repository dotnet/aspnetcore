// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Http3SettingType = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.Http3SettingType;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http3ConnectionTests : Http3TestBase
{
    private static readonly KeyValuePair<string, string>[] Headers = new[]
    {
        new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
        new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
        new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
        new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
    };

    [Fact]
    public async Task CreateRequestStream_RequestCompleted_Disposed()
    {
        var appCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Http3Api.InitializeConnectionAsync(async context =>
        {
            var buffer = new byte[16 * 1024];
            var received = 0;

            while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await context.Response.Body.WriteAsync(buffer, 0, received);
            }

            await appCompletedTcs.Task;
        });

        await Http3Api.CreateControlStream();
        await Http3Api.GetInboundControlStream();

        var requestStream = await Http3Api.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        });

        await requestStream.SendDataAsync(Encoding.ASCII.GetBytes("Hello world"), endStream: true);

        Assert.False(requestStream.Disposed);

        appCompletedTcs.SetResult();
        await requestStream.ExpectHeadersAsync();
        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello world", Encoding.ASCII.GetString(responseData.ToArray()));

        await requestStream.OnDisposedTask.DefaultTimeout();
        Assert.True(requestStream.Disposed);
    }

    [Fact]
    public async Task HEADERS_Received_ContainsExpect100Continue_100ContinueSent()
    {
        await Http3Api.InitializeConnectionAsync(async context =>
        {
            var buffer = new byte[16 * 1024];
            var received = 0;

            while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await context.Response.Body.WriteAsync(buffer, 0, received);
            }
        });

        await Http3Api.CreateControlStream();
        await Http3Api.GetInboundControlStream();

        var requestStream = await Http3Api.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "127.0.0.1"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Expect, "100-continue"),
        });

        var frame = await requestStream.ReceiveFrameAsync();
        Assert.Equal(Http3FrameType.Headers, frame.Type);

        var continueBytesQpackEncoded = new byte[] { 0x00, 0x00, 0xff, 0x00 };
        Assert.Equal(continueBytesQpackEncoded, frame.PayloadSequence.ToArray());

        await requestStream.SendDataAsync(Encoding.ASCII.GetBytes("Hello world"), endStream: false);
        var headers = await requestStream.ExpectHeadersAsync();
        Assert.Equal("200", headers[InternalHeaderNames.Status]);

        var responseData = await requestStream.ExpectDataAsync();
        Assert.Equal("Hello world", Encoding.ASCII.GetString(responseData.ToArray()));

        Assert.False(requestStream.Disposed, "Request is in progress and shouldn't be disposed.");

        await requestStream.SendDataAsync(Encoding.ASCII.GetBytes($"End"), endStream: true);
        responseData = await requestStream.ExpectDataAsync();
        Assert.Equal($"End", Encoding.ASCII.GetString(responseData.ToArray()));

        await requestStream.ExpectReceiveEndOfStream();
    }

    [Fact]
    public async Task HEADERS_CookiesMergedIntoOne()
    {
        var requestHeaders = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "GET"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Cookie, "a=0"),
            new KeyValuePair<string, string>(HeaderNames.Cookie, "b=1"),
            new KeyValuePair<string, string>(HeaderNames.Cookie, "c=2"),
        };

        var receivedHeaders = "";

        await Http3Api.InitializeConnectionAsync(async context =>
        {
            var buffer = new byte[16 * 1024];
            var received = 0;

            // verify that the cookies are all merged into a single string
            receivedHeaders = context.Request.Headers[HeaderNames.Cookie];

            while ((received = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await context.Response.Body.WriteAsync(buffer, 0, received);
            }
        });

        await Http3Api.CreateControlStream();
        await Http3Api.GetInboundControlStream();
        var requestStream = await Http3Api.CreateRequestStream(requestHeaders, endStream: true);
        var responseHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();
        await requestStream.OnDisposedTask.DefaultTimeout();

        Assert.Equal("a=0; b=1; c=2", receivedHeaders);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 4)]
    [InlineData(111, 444)]
    [InlineData(512, 2048)]
    public async Task GOAWAY_GracefulServerShutdown_SendsGoAway(int connectionRequests, int expectedStreamId)
    {
        await Http3Api.InitializeConnectionAsync(_echoApplication);

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        for (var i = 0; i < connectionRequests; i++)
        {
            var request = await Http3Api.CreateRequestStream(Headers);
            await request.EndStreamAsync();
            await request.ExpectReceiveEndOfStream();

            await request.OnStreamCompletedTask.DefaultTimeout();
        }

        // Trigger server shutdown.
        Http3Api.CloseServerGracefully();

        Assert.Null(await Http3Api.MultiplexedConnectionContext.AcceptAsync().DefaultTimeout());

        await Http3Api.WaitForConnectionStopAsync(expectedStreamId, false, expectedErrorCode: Http3ErrorCode.NoError);
        MetricsAssert.NoError(Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task GOAWAY_GracefulServerShutdownWithActiveRequest_SendsMultipleGoAways()
    {
        await Http3Api.InitializeConnectionAsync(_echoApplication);

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        await inboundControlStream.ExpectSettingsAsync();

        var activeRequest = await Http3Api.CreateRequestStream(Headers);

        // Trigger server shutdown.
        Http3Api.CloseServerGracefully();

        await Http3Api.WaitForGoAwayAsync(false, VariableLengthIntegerHelper.EightByteLimit);

        // Request made while shutting down is rejected.
        var rejectedRequest = await Http3Api.CreateRequestStream(Headers);
        await rejectedRequest.WaitForStreamErrorAsync(Http3ErrorCode.RequestRejected);

        // End active request.
        await activeRequest.EndStreamAsync();
        await activeRequest.ExpectReceiveEndOfStream();

        // Client aborts the connection.
        Http3Api.MultiplexedConnectionContext.Abort();

        await Http3Api.WaitForConnectionStopAsync(4, false, expectedErrorCode: Http3ErrorCode.NoError);
        MetricsAssert.NoError(Http3Api.ConnectionTags);
    }

    [Theory]
    [InlineData(0x0)]
    [InlineData(0x2)]
    [InlineData(0x3)]
    [InlineData(0x4)]
    [InlineData(0x5)]
    public async Task SETTINGS_ReservedSettingSent_ConnectionError(long settingIdentifier)
    {
        await Http3Api.InitializeConnectionAsync(_echoApplication);

        var outboundcontrolStream = await Http3Api.CreateControlStream();
        await outboundcontrolStream.SendSettingsAsync(new List<Http3PeerSetting>
            {
                new Http3PeerSetting((Http3SettingType) settingIdentifier, 0) // reserved value
            });

        await Http3Api.GetInboundControlStream();

        await Http3Api.WaitForConnectionErrorAsync<Http3ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 0,
            expectedErrorCode: Http3ErrorCode.SettingsError,
            matchExpectedErrorMessage: AssertExpectedErrorMessages,
            expectedErrorMessage: CoreStrings.FormatHttp3ErrorControlStreamReservedSetting($"0x{settingIdentifier.ToString("X", CultureInfo.InvariantCulture)}"));
        MetricsAssert.Equal(ConnectionEndReason.InvalidSettings, Http3Api.ConnectionTags);
    }

    [Theory]
    [InlineData(0, "control")]
    [InlineData(2, "encoder")]
    [InlineData(3, "decoder")]
    public async Task InboundStreams_CreateMultiple_ConnectionError(int streamId, string name)
    {
        await Http3Api.InitializeConnectionAsync(_noopApplication);

        await Http3Api.CreateControlStream(streamId);
        await Http3Api.CreateControlStream(streamId);

        await Http3Api.WaitForConnectionErrorAsync<Http3ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 0,
            expectedErrorCode: Http3ErrorCode.StreamCreationError,
            matchExpectedErrorMessage: AssertExpectedErrorMessages,
            expectedErrorMessage: CoreStrings.FormatHttp3ControlStreamErrorMultipleInboundStreams(name));
        MetricsAssert.Equal(ConnectionEndReason.StreamCreationError, Http3Api.ConnectionTags);
    }

    [Theory]
    [InlineData(nameof(Http3FrameType.Data))]
    [InlineData(nameof(Http3FrameType.Headers))]
    [InlineData(nameof(Http3FrameType.PushPromise))]
    public async Task ControlStream_ClientToServer_UnexpectedFrameType_ConnectionError(string frameType)
    {
        await Http3Api.InitializeConnectionAsync(_noopApplication);

        var controlStream = await Http3Api.CreateControlStream();

        var f = Enum.Parse<Http3FrameType>(frameType);
        await controlStream.SendFrameAsync(f, Memory<byte>.Empty);

        await Http3Api.WaitForConnectionErrorAsync<Http3ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 0,
            expectedErrorCode: Http3ErrorCode.UnexpectedFrame,
            matchExpectedErrorMessage: AssertExpectedErrorMessages,
            expectedErrorMessage: CoreStrings.FormatHttp3ErrorUnsupportedFrameOnControlStream(Http3Formatting.ToFormattedType(f)));
        MetricsAssert.Equal(ConnectionEndReason.UnexpectedFrame, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task ControlStream_ClientToServer_Completes_ConnectionError()
    {
        await Http3Api.InitializeConnectionAsync(_noopApplication);

        var controlStream = await Http3Api.CreateControlStream(id: 0);
        await controlStream.SendSettingsAsync(new List<Http3PeerSetting>());

        await controlStream.EndStreamAsync().DefaultTimeout();

        // Wait for control stream to finish processing and exit.
        await controlStream.OnStreamCompletedTask.DefaultTimeout();

        Http3Api.TriggerTick();
        Http3Api.TriggerTick(TimeSpan.FromSeconds(1));

        await Http3Api.WaitForConnectionErrorAsync<Http3ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 0,
            expectedErrorCode: Http3ErrorCode.ClosedCriticalStream,
            matchExpectedErrorMessage: AssertExpectedErrorMessages,
            expectedErrorMessage: CoreStrings.Http3ErrorControlStreamClosed);
        MetricsAssert.Equal(ConnectionEndReason.ClosedCriticalStream, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task GOAWAY_TriggersLifetimeNotification_ConnectionClosedRequested()
    {
        var completionSource = new TaskCompletionSource();
        await Http3Api.InitializeConnectionAsync(_noopApplication);

        var controlStream = await Http3Api.CreateControlStream(id: 0);
        await controlStream.SendSettingsAsync(new List<Http3PeerSetting>());
        var lifetime = Http3Api.MultiplexedConnectionContext.Features.Get<IConnectionLifetimeNotificationFeature>();
        lifetime.ConnectionClosedRequested.Register(() => completionSource.TrySetResult());
        Assert.False(lifetime.ConnectionClosedRequested.IsCancellationRequested);
        
        await controlStream.SendGoAwayAsync(streamId: 0, false);

        await completionSource.Task.DefaultTimeout();
        Assert.True(lifetime.ConnectionClosedRequested.IsCancellationRequested);

        // Trigger server shutdown.
        Http3Api.CloseServerGracefully();

        await Http3Api.WaitForConnectionStopAsync(0, true, expectedErrorCode: Http3ErrorCode.NoError);
        MetricsAssert.NoError(Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task ControlStream_ServerToClient_ErrorInitializing_ConnectionError()
    {
        await Http3Api.InitializeConnectionAsync(_noopApplication);

        var controlStream = await Http3Api.GetInboundControlStream();

        controlStream.StreamContext.Close();

        Http3Api.TriggerTick();
        Http3Api.TriggerTick(TimeSpan.FromSeconds(1));

        await Http3Api.WaitForConnectionErrorAsync<Http3ConnectionErrorException>(
            ignoreNonGoAwayFrames: true,
            expectedLastStreamId: 0,
            expectedErrorCode: Http3ErrorCode.ClosedCriticalStream,
            matchExpectedErrorMessage: AssertExpectedErrorMessages,
            expectedErrorMessage: CoreStrings.Http3ErrorControlStreamClosed);
        MetricsAssert.Equal(ConnectionEndReason.ClosedCriticalStream, Http3Api.ConnectionTags);
    }

    [Fact]
    public async Task SETTINGS_MaxFieldSectionSizeSent_ServerReceivesValue()
    {
        await Http3Api.InitializeConnectionAsync(_echoApplication);

        var inboundControlStream = await Http3Api.GetInboundControlStream();
        var incomingSettings = await inboundControlStream.ExpectSettingsAsync();

        var defaultLimits = new KestrelServerLimits();
        Assert.Collection(incomingSettings,
            kvp =>
            {
                Assert.Equal((long)Http3SettingType.MaxFieldSectionSize, kvp.Key);
                Assert.Equal(defaultLimits.MaxRequestHeadersTotalSize, kvp.Value);
            });

        var outboundcontrolStream = await Http3Api.CreateControlStream();
        await outboundcontrolStream.SendSettingsAsync(new List<Http3PeerSetting>
            {
                new Http3PeerSetting(Http3SettingType.MaxFieldSectionSize, 100)
            });

        var maxFieldSetting = await Http3Api.ServerReceivedSettingsReader.ReadAsync().DefaultTimeout();

        Assert.Equal(Http3SettingType.MaxFieldSectionSize, maxFieldSetting.Key);
        Assert.Equal(100, maxFieldSetting.Value);
    }

    [Fact]
    public async Task StreamPool_MultipleStreamsInSequence_PooledStreamReused()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        await Http3Api.InitializeConnectionAsync(_echoApplication);

        var streamContext1 = await MakeRequestAsync(0, headers, sendData: true, waitForServerDispose: true);
        var streamContext2 = await MakeRequestAsync(1, headers, sendData: true, waitForServerDispose: true);

        Assert.Same(streamContext1, streamContext2);
    }

    [Fact]
    public async Task StreamPool_MultipleStreamsInSequence_KnownHeaderReused()
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
            new KeyValuePair<string, string>(HeaderNames.ContentType, "application/json"),
        };

        string contentType = null;
        string authority = null;
        await Http3Api.InitializeConnectionAsync(async context =>
        {
            contentType = context.Request.ContentType;
            authority = context.Request.Host.Value;
            await _echoApplication(context);
        });

        var streamContext1 = await MakeRequestAsync(0, headers, sendData: true, waitForServerDispose: true);
        var contentType1 = contentType;
        var authority1 = authority;

        var streamContext2 = await MakeRequestAsync(1, headers, sendData: true, waitForServerDispose: true);
        var contentType2 = contentType;
        var authority2 = authority;

        Assert.NotNull(contentType1);
        Assert.NotNull(authority1);

        Assert.Same(contentType1, contentType2);
        Assert.Same(authority1, authority2);
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

        await Http3Api.InitializeConnectionAsync(_echoApplication);

        var streamContext1 = await MakeRequestAsync(0, requestHeaders1, sendData: true, waitForServerDispose: true);
        var http3Stream1 = (Http3Stream)streamContext1.Features.Get<IPersistentStateFeature>().State[Http3Connection.StreamPersistentStateKey];

        // Hacky but required because header references is private.
        var headerReferences1 = typeof(HttpRequestHeaders).GetField("_headers", privateFlags).GetValue(http3Stream1.RequestHeaders);
        var contentTypeValue1 = (StringValues)headerReferences1.GetType().GetField("_ContentType").GetValue(headerReferences1);

        var streamContext2 = await MakeRequestAsync(1, requestHeaders2, sendData: true, waitForServerDispose: true);
        var http3Stream2 = (Http3Stream)streamContext2.Features.Get<IPersistentStateFeature>().State[Http3Connection.StreamPersistentStateKey];

        // Hacky but required because header references is private.
        var headerReferences2 = typeof(HttpRequestHeaders).GetField("_headers", privateFlags).GetValue(http3Stream2.RequestHeaders);
        var contentTypeValue2 = (StringValues)headerReferences1.GetType().GetField("_ContentType").GetValue(headerReferences2);

        Assert.Equal("application/json", contentTypeValue1);
        Assert.Equal(StringValues.Empty, contentTypeValue2);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(500)]
    public async Task StreamPool_VariableMultipleStreamsInSequence_PooledStreamReused(int count)
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        await Http3Api.InitializeConnectionAsync(_echoApplication);

        ConnectionContext first = null;
        ConnectionContext last = null;
        for (var i = 0; i < count; i++)
        {
            Logger.LogInformation($"Iteration {i}");

            var streamContext = await MakeRequestAsync(i, headers, sendData: true, waitForServerDispose: true);

            first ??= streamContext;
            last = streamContext;

            Assert.Same(first, last);
        }
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(10, true)]
    [InlineData(100, false)]
    [InlineData(100, true)]
    [InlineData(500, false)]
    [InlineData(500, true)]
    public async Task VariableMultipleStreamsInSequence_Success(int count, bool sendData)
    {
        var headers = new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "Custom"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "localhost:80"),
        };

        var requestDelegate = sendData ? _echoApplication : _noopApplication;

        await Http3Api.InitializeConnectionAsync(requestDelegate);

        for (var i = 0; i < count; i++)
        {
            Logger.LogInformation($"Iteration {i}");

            await MakeRequestAsync(i, headers, sendData, waitForServerDispose: false);
        }
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
        await Http3Api.InitializeConnectionAsync(context =>
        {
            var trailersFeature = context.Features.Get<IHttpResponseTrailersFeature>();
            if (requestCount == 0)
            {
                trailersFirst = new ResponseTrailersWrapper(trailersFeature.Trailers);
                trailersFeature.Trailers = trailersFirst;
            }
            else
            {
                trailersLast = trailersFeature.Trailers;
            }
            trailersFeature.Trailers[$"trailer-{requestCount++}"] = "true";
            return Task.CompletedTask;
        });

        for (int i = 0; i < 3; i++)
        {
            var requestStream = await Http3Api.CreateRequestStream(requestHeaders, endStream: true);
            var responseHeaders = await requestStream.ExpectHeadersAsync();

            var data = await requestStream.ExpectTrailersAsync();
            Assert.Single(data);
            Assert.True(data.TryGetValue($"trailer-{i}", out var trailerValue) && bool.Parse(trailerValue));

            await requestStream.ExpectReceiveEndOfStream();
            await requestStream.OnDisposedTask.DefaultTimeout();
        }

        Assert.NotNull(trailersFirst);
        Assert.NotNull(trailersLast);
        Assert.NotSame(trailersFirst, trailersLast);
    }

    [Fact]
    public async Task WriteBeforeFlushingHeadersTracksBytesCorrectly()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Http3Api.InitializeConnectionAsync(async c =>
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

        var requestStream = await Http3Api.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "127.0.0.1"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Expect, "100-continue"),
        });

        await requestStream.SendDataAsync(Memory<byte>.Empty, endStream: true);

        await requestStream.ExpectHeadersAsync();
        await requestStream.ExpectDataAsync();

        await requestStream.OnDisposedTask.DefaultTimeout();
        Assert.True(requestStream.Disposed);

        await tcs.Task;
    }

    [Fact]
    public async Task WriteAfterFlushingHeadersTracksBytesCorrectly()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Http3Api.InitializeConnectionAsync(async c =>
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

        var requestStream = await Http3Api.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "POST"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "127.0.0.1"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Expect, "100-continue"),
        });

        await requestStream.SendDataAsync(Memory<byte>.Empty, endStream: true);

        await requestStream.ExpectHeadersAsync();
        await requestStream.ExpectDataAsync();

        await requestStream.OnDisposedTask.DefaultTimeout();
        Assert.True(requestStream.Disposed);

        await tcs.Task;
    }

    [Fact]
    public async Task ErrorCodeIsValidOnConnectionTimeout()
    {
        // This test loosely repros the scenario in https://github.com/dotnet/aspnetcore/issues/57933.
        // In particular, there's a request from the server and, once a response has been sent,
        // the (simulated) transport throws a QuicException that surfaces through AcceptAsync.
        // This test confirms that Http3Connection.ProcessRequestsAsync doesn't (indirectly) cause
        // IProtocolErrorCodeFeature.Error to be set to (or left at) -1, which System.Net.Quic will
        // not accept.

        // Used to signal that a request has been sent and a response has been received
        var requestTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // Used to signal that the connection context has been aborted
        var abortTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // InitializeConnectionAsync consumes the connection context, so set it first
        Http3Api.MultiplexedConnectionContext = new ThrowingMultiplexedConnectionContext(Http3Api, skipCount: 2, requestTcs, abortTcs);
        await Http3Api.InitializeConnectionAsync(_echoApplication);

        await Http3Api.CreateControlStream();
        await Http3Api.GetInboundControlStream();
        var requestStream = await Http3Api.CreateRequestStream(Headers, endStream: true);
        var responseHeaders = await requestStream.ExpectHeadersAsync();

        await requestStream.ExpectReceiveEndOfStream();
        await requestStream.OnDisposedTask.DefaultTimeout();

        requestTcs.SetResult();

        // By the time the connection context is aborted, the error code feature has been updated
        await abortTcs.Task.DefaultTimeout();

        Http3Api.CloseServerGracefully();

        var errorCodeFeature = Http3Api.MultiplexedConnectionContext.Features.Get<IProtocolErrorCodeFeature>();
        Assert.InRange(errorCodeFeature.Error, 0, (1L << 62) - 1); // Valid range for HTTP/3 error codes
    }

    private sealed class ThrowingMultiplexedConnectionContext : TestMultiplexedConnectionContext
    {
        private int _skipCount;
        private readonly TaskCompletionSource _requestTcs;
        private readonly TaskCompletionSource _abortTcs;

        /// <summary>
        /// After <paramref name="skipCount"/> calls to <see cref="AcceptAsync"/>, the next call will throw a <see cref="QuicException"/>
        /// (after waiting for <see cref="_requestTcs"/> to be set).
        ///
        /// <paramref name="abortTcs"/> lets this type signal that <see cref="Abort"/> has been called.
        /// </summary>
        public ThrowingMultiplexedConnectionContext(Http3InMemory testBase, int skipCount, TaskCompletionSource requestTcs, TaskCompletionSource abortTcs)
            : base(testBase)
        {
            _skipCount = skipCount;
            _requestTcs = requestTcs;
            _abortTcs = abortTcs;
        }

        public override async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (_skipCount-- <= 0)
            {
                await _requestTcs.Task.DefaultTimeout();
                throw new System.Net.Quic.QuicException(
                    System.Net.Quic.QuicError.ConnectionTimeout,
                    applicationErrorCode: null,
                    "Connection timed out waiting for a response from the peer.");
            }
            return await base.AcceptAsync(cancellationToken);
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            _abortTcs.SetResult();
            base.Abort(abortReason);
        }
    }

    private async Task<ConnectionContext> MakeRequestAsync(int index, KeyValuePair<string, string>[] headers, bool sendData, bool waitForServerDispose)
    {
        var requestStream = await Http3Api.CreateRequestStream(headers, endStream: !sendData);
        var streamContext = requestStream.StreamContext;

        if (sendData)
        {
            await requestStream.SendDataAsync(Encoding.ASCII.GetBytes($"Hello world {index}"));
        }

        await requestStream.ExpectHeadersAsync();

        if (sendData)
        {
            var responseData = await requestStream.ExpectDataAsync();
            Assert.Equal($"Hello world {index}", Encoding.ASCII.GetString(responseData.ToArray()));

            Assert.False(requestStream.Disposed, "Request is in progress and shouldn't be disposed.");

            await requestStream.SendDataAsync(Encoding.ASCII.GetBytes($"End {index}"), endStream: true);
            responseData = await requestStream.ExpectDataAsync();
            Assert.Equal($"End {index}", Encoding.ASCII.GetString(responseData.ToArray()));
        }

        await requestStream.ExpectReceiveEndOfStream();

        if (waitForServerDispose)
        {
            await requestStream.OnDisposedTask.DefaultTimeout();
            Assert.True(requestStream.Disposed, "Request is complete and should be disposed.");

            Logger.LogInformation($"Received notification that stream {index} disposed.");
        }

        return streamContext;
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
}
