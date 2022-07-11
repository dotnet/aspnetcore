// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;

internal sealed class WebTransportSession : IWebTransportSession
{
    private static readonly IStreamDirectionFeature _outputStreamDirectionFeature = new DefaultStreamDirectionFeature(canRead: false, canWrite: true);

    private readonly CancellationTokenRegistration _connectionClosedRegistration;

    // stores all created streams (pending or accepted)
    private readonly ConcurrentDictionary<long, WebTransportStream> _openStreams = new();
    // stores all pending streams that have not been accepted yet
    private readonly Channel<WebTransportStream> _pendingStreams;

    private readonly Http3Connection _connection;
    private readonly Http3Stream _connectStream = default!;
    private bool _isClosing;

    private static readonly ReadOnlyMemory<byte> OutputStreamHeader = new(new byte[] {
            0x40 /*quic variable-length integer length*/,
            (byte)Http3StreamType.WebTransportUnidirectional,
            0x00 /*body*/});

    internal static string WebTransportProtocolValue => "webtransport";
    internal static string SecPrefix => "sec-webtransport-http3-";
    internal static string VersionHeaderPrefix => $"{SecPrefix}draft";
    internal static string CurrentSuppportedVersion => $"{VersionHeaderPrefix}02";

    long IWebTransportSession.SessionId => _connectStream.StreamId;

    internal WebTransportSession(Http3Connection connection, Http3Stream connectStream)
    {
        _connection = connection;
        _connectStream = connectStream;
        _isClosing = false;
        // unbounded as limits to number of streams is enforced elsewhere
        _pendingStreams = Channel.CreateUnbounded<WebTransportStream>();

        // listener to abort if this connection is closed
        _connectionClosedRegistration = connection._multiplexedContext.ConnectionClosed.Register(static state =>
        {
            var session = (WebTransportSession)state!;
            session.OnClientConnectionClosed();
        }, this);
    }

    void IWebTransportSession.Abort(int errorCode)
    {
        Abort(new(), (Http3ErrorCode)errorCode);
    }

    internal void OnClientConnectionClosed()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;

        _connectionClosedRegistration.Dispose();

        lock (_openStreams)
        {
            foreach (var stream in _openStreams)
            {
                _ = stream.Value.DisposeAsync().AsTask();
            }

            _openStreams.Clear();
        }

        _pendingStreams.Writer.Complete();
    }

    internal void Abort(ConnectionAbortedException exception, Http3ErrorCode error)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;

        _connectionClosedRegistration.Dispose();

        lock (_openStreams)
        {
            _connectStream.Abort(exception, error);
            foreach (var stream in _openStreams)
            {
                if (exception.InnerException is not null)
                {
                    stream.Value.Abort(new ConnectionAbortedException(exception.Message, exception.InnerException));
                }
                else
                {
                    stream.Value.Abort(new ConnectionAbortedException(exception.Message));
                }
            }
            _openStreams.Clear();
        }

        _pendingStreams.Writer.Complete();
    }

    public async ValueTask<ConnectionContext?> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken)
    {
        if (_isClosing)
        {
            return null;
        }
        // create the stream
        var features = new FeatureCollection();
        features.Set(_outputStreamDirectionFeature);
        var connectionContext = await _connection._multiplexedContext.ConnectAsync(features, cancellationToken);
        var streamContext = _connection.CreateHttpStreamContext(connectionContext);
        var stream = new WebTransportStream(streamContext, WebTransportStreamType.Output);

        var success = _openStreams.TryAdd(stream.StreamId, stream);
        Debug.Assert(success);

        // send the stream header
        // https://ietf-wg-webtrans.github.io/draft-ietf-webtrans-http3/draft-ietf-webtrans-http3.html#name-unidirectional-streams
        await stream.Transport.Output.WriteAsync(OutputStreamHeader, cancellationToken);

        return stream;
    }

    internal void AddStream(WebTransportStream stream)
    {
        if (_isClosing)
        {
            throw new ObjectDisposedException(CoreStrings.WebTransportIsClosing);
        }

        if (!_pendingStreams.Writer.TryWrite(stream) || !_openStreams.TryAdd(stream.StreamId, stream))
        {
            throw new Exception(CoreStrings.WebTransportFailedToAddStreamToPendingQueue);
        }
    }

    public async ValueTask<ConnectionContext?> AcceptStreamAsync(CancellationToken cancellationToken)
    {
        if (_isClosing)
        {
            return null;
        }

        try
        {
            return await _pendingStreams.Reader.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }

    internal bool TryRemoveStream(long streamId)
    {
        var success = _openStreams.Remove(streamId, out var stream);

        if (success && stream is not null)
        {
            _ = stream.DisposeAsync().AsTask();
        }

        return success;
    }
}
