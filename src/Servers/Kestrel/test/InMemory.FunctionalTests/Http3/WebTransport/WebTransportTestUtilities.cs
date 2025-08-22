// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal class WebTransportTestUtilities
{
    private static int streamCounter;

    public static async ValueTask<WebTransportSession> GenerateSession(Http3InMemory inMemory, TaskCompletionSource exitSessionTcs)
    {
#pragma warning disable CA2252 // WebTransport is a preview feature
        var appCompletedTcs = new TaskCompletionSource<IWebTransportSession>(TaskCreationOptions.RunContinuationsAsynchronously);

        await inMemory.InitializeConnectionAsync(async context =>
        {
            var webTransportFeature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();

            try
            {
                var session = await webTransportFeature.AcceptAsync(CancellationToken.None).DefaultTimeout();
                appCompletedTcs.SetResult(session);
            }
            catch (TimeoutException exception)
            {
                appCompletedTcs.SetException(exception);
            }
#pragma warning restore CA2252 // WebTransport is a preview feature

            // wait for the test to tell us to kill the application
            await exitSessionTcs.Task;
        });
        var controlStream = await inMemory.CreateControlStream();
        var controlStream2 = await inMemory.GetInboundControlStream();

        var settings = new Http3PeerSettings()
        {
            EnableWebTransport = 1,
            H3Datagram = 1,
        };

        await controlStream.SendSettingsAsync(settings.GetNonProtocolDefaults());
        var response1 = await controlStream2.ExpectSettingsAsync();

        await inMemory.ServerReceivedSettingsReader.ReadAsync().DefaultTimeout();

        var requestStream = await inMemory.CreateRequestStream(new[]
        {
            new KeyValuePair<string, string>(InternalHeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(InternalHeaderNames.Protocol, "webtransport"),
            new KeyValuePair<string, string>(InternalHeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(InternalHeaderNames.Path, "/"),
            new KeyValuePair<string, string>(InternalHeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "server.example.com"),
            new KeyValuePair<string, string>(WebTransportSession.CurrentSupportedVersion, "1")
        });

        return (WebTransportSession)await appCompletedTcs.Task;
    }

    public static WebTransportStream CreateStream(WebTransportStreamType type, Memory<byte>? memory = null)
    {
        var features = new FeatureCollection();
        features.Set<IStreamIdFeature>(new StreamId(streamCounter++));
        features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(type != WebTransportStreamType.Output, type != WebTransportStreamType.Input));
        features.Set(Mock.Of<IConnectionItemsFeature>());
        features.Set(Mock.Of<IProtocolErrorCodeFeature>());
        features.Set(Mock.Of<IConnectionMetricsContextFeature>());

        var writer = new HttpResponsePipeWriter(new StreamWriterControl(memory));
        writer.StartAcceptingWrites();
        var transport = new DuplexPipe(new StreamReader(memory), writer);
        return new WebTransportStream(TestContextFactory.CreateHttp3StreamContext("id", null, new TestServiceContext(), features, null, null, null, transport), type);
    }

    class StreamId : IStreamIdFeature
    {
        private readonly int _id;
        long IStreamIdFeature.StreamId => _id;

        public StreamId(int id = 1)
        {
            _id = id;
        }
    }

    class StreamWriterControl : IHttpResponseControl
    {
        readonly Memory<byte>? _sharedMemory;

        public StreamWriterControl(Memory<byte>? sharedMemory = null)
        {
            _sharedMemory = sharedMemory;
        }

        public void Advance(int bytes)
        {
            // no-op
        }

        public void CancelPendingFlush()
        {
            // no-op
        }

        public ValueTask<FlushResult> FlushPipeAsync(CancellationToken cancellationToken)
        {
            // no-op
            return new ValueTask<FlushResult>();
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (_sharedMemory is null)
            {
                throw new NullReferenceException();
            }
            return (Memory<byte>)_sharedMemory;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return GetMemory(sizeHint).Span;
        }

        public long UnflushedBytes => 0;

        public ValueTask<FlushResult> ProduceContinueAsync()
        {
            // no-op
            return new ValueTask<FlushResult>();
        }

        public Task CompleteAsync(Exception exception)
        {
            // no-op
            return Task.CompletedTask;
        }

        public ValueTask<FlushResult> WritePipeAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            source.CopyTo(GetMemory());
            return new ValueTask<FlushResult>();
        }
    }

    class StreamReader : PipeReader
    {
        readonly Memory<byte>? _sharedMemory;

        public StreamReader(Memory<byte>? sharedMemory = null)
        {
            _sharedMemory = sharedMemory;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            // no-op
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            // no-op
        }

        public override void CancelPendingRead()
        {
            throw new NotImplementedException();
        }

        public override void Complete(Exception exception = null)
        {
            // no-op
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            // just return the whole memory as a readResult
            return new ValueTask<ReadResult>(new ReadResult(
                new ReadOnlySequence<byte>((ReadOnlyMemory<byte>)_sharedMemory), false, true));
        }

        public override bool TryRead(out ReadResult result)
        {
            result = new ReadResult(new ReadOnlySequence<byte>((ReadOnlyMemory<byte>)_sharedMemory), false, true);
            return true;
        }
    }
}
