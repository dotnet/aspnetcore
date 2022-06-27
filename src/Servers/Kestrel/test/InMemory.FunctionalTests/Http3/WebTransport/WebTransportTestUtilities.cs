// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal class WebTransportTestUtilities
{
    private static int streamCounter;

    public static async ValueTask<WebTransportSession> GenerateSession(Http3InMemory inMemory)
    {
        var appCompletedTcs = new TaskCompletionSource<IWebTransportSession>(TaskCreationOptions.RunContinuationsAsynchronously);

        await inMemory.InitializeConnectionAsync(async context =>
        {
            var webTransportFeature = context.Features.GetRequiredFeature<IHttpWebTransportFeature>();

#pragma warning disable CA2252 // This API requires opting into preview features
            try
            {
                var session = await webTransportFeature.AcceptAsync(CancellationToken.None).DefaultTimeout();
                appCompletedTcs.SetResult(session);
            }
            catch (TimeoutException exception)
            {
                appCompletedTcs.SetException(exception);
            }
#pragma warning restore CA2252

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

        var requestStream = await inMemory.CreateRequestStream();
        var headersConnectFrame = new[]
        {
            new KeyValuePair<string, string>(HeaderNames.Method, "CONNECT"),
            new KeyValuePair<string, string>(HeaderNames.Protocol, "webtransport"),
            new KeyValuePair<string, string>(HeaderNames.Scheme, "http"),
            new KeyValuePair<string, string>(HeaderNames.Path, "/"),
            new KeyValuePair<string, string>(HeaderNames.Authority, "server.example.com"),
            new KeyValuePair<string, string>(HeaderNames.Origin, "server.example.com"),
            new KeyValuePair<string, string>(WebTransportSession.SuppportedWebTransportVersions.First(), "1")
        };

        await requestStream.SendHeadersAsync(headersConnectFrame);
        var response2 = await requestStream.ExpectHeadersAsync();

        return (WebTransportSession)await appCompletedTcs.Task;
    }

    public static async ValueTask<WebTransportStream> CreateStream(WebTransportStreamType type, Memory<byte>? memory = null)
    {
        var features = new FeatureCollection();
        features[typeof(IStreamIdFeature)] = new StreamId(streamCounter++);
        features[typeof(IProtocolErrorCodeFeature)] = new ErrorCode();

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

    class ErrorCode : IProtocolErrorCodeFeature
    {
        long IProtocolErrorCodeFeature.Error { get; set; }
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

        public ValueTask<FlushResult> ProduceContinueAsync()
        {
            // no-op
            return new ValueTask<FlushResult>();
        }

        public Task CompleteAsync(Exception? exception)
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
