// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;

/// <summary>
/// Controls the WebTransport session and keeps track of all the streams.
/// </summary>
internal class WebTransportSession : IWebTransportSession
{
    private readonly Dictionary<long, WebTransportStream> _openStreams = new();
    private readonly ConcurrentQueue<WebTransportStream> _pendingStreams = new();
    private readonly SemaphoreSlim _pendingAcceptStreamRequests = new(0);
    private readonly Http3Connection _connection;
    private readonly Http3Stream _controlStream = default!;
    private bool _isClosing;

    internal static string WebTransportProtocolValue => "webtransport";
    internal static string SecPrefix => "sec-webtransport-http3-";
    internal static string VersionHeaderPrefix => $"{SecPrefix}draft";

    // Order is important as we choose the first supported
    // version (preferenc dereases as index increases)
    internal static readonly IEnumerable<string> SuppportedWebTransportVersions = new string[]
    {
        $"{VersionHeaderPrefix}02"
    };

    long IWebTransportSession.SessionId => _controlStream.StreamId;


    internal WebTransportSession(Http3Connection connection, Http3Stream controlStream)
    {
        _connection = connection;
        _controlStream = controlStream;
        _isClosing = false;
    }


    void IWebTransportSession.Abort()
    {
        Abort(new(), Http3ErrorCode.NoError);
    }

    internal void OnClientConnectionClosed()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;

        lock (_openStreams)
        {
            foreach (var stream in _openStreams)
            {
                stream.Value.Close();
            }

            _openStreams.Clear();
        }

        _pendingStreams.Clear();
    }

    internal void Abort(ConnectionAbortedException exception, Http3ErrorCode error)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        lock (_openStreams)
        {
            _controlStream.Abort(exception, error);
            foreach (var stream in _openStreams)
            {
                stream.Value.Abort(exception);
            }

            _openStreams.Clear();
        }

        _pendingStreams.Clear();
    }

    async ValueTask<Stream> IWebTransportSession.OpenUnidirectionalStreamAsync(CancellationToken cancellationToken)
    {
        if (_isClosing)
        {
            throw new Exception("WebTransport is closing the session");
        }
        // create the stream
        var features = new FeatureCollection();
        features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(canRead: false, canWrite: true));
        var connectionContext = await _connection!._multiplexedContext.ConnectAsync(features, cancellationToken);
        var streamContext = _connection.CreateHttpStreamContext(connectionContext);
        var stream = await WebTransportStream.CreateWebTransportStream(streamContext, WebTransportStreamType.Output);

        // send the stream header
        // https://ietf-wg-webtrans.github.io/draft-ietf-webtrans-http3/draft-ietf-webtrans-http3.html#name-unidirectional-streams
        await stream.WriteAsync(new ReadOnlyMemory<byte>(new byte[] {
            0x40 /*quic variable-length integer length*/,
            (byte)Http3StreamType.WebTransportUnidirectional,
            0x00 /*body*/}), cancellationToken);
        await stream.FlushAsync(cancellationToken);

        return stream;
    }

    /// <summary>
    /// Adds a new stream to the internal list of open streams.
    /// </summary>
    /// <param name="stream">A reference to the new stream that is being added</param>
    internal void AddStream(WebTransportStream stream)
    {
        if (_isClosing)
        {
            throw new Exception("WebTransport is closing the session");
        }

        lock (_pendingStreams)
        {
            _pendingStreams.Enqueue(stream);
            _pendingAcceptStreamRequests.Release();
        }
    }

    /// <summary>
    /// Pops the first stream from the list of pending streams.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to abort waiting for a stream</param>
    /// <returns>An instance of WebTransportStream that corresponds to the new stream to accept</returns>
    public async ValueTask<Stream> AcceptStreamAsync(CancellationToken cancellationToken)
    {
        if (_isClosing)
        {
            throw new Exception("WebTransport is closing the session");
        }

        await _pendingAcceptStreamRequests.WaitAsync(cancellationToken);

        var success = _pendingStreams.TryDequeue(out var stream);
        if (!success)
        {
            throw new Exception("Failed to accept the next stream in the queue");
        }
        _openStreams.Add(stream!.StreamId, stream);

        return stream!;
    }

    /// <summary>
    /// Tries to remove a stream from the internal list of open streams.
    /// </summary>
    /// <param name="streamId">A reference to the new stream that is being added</param>
    /// <returns>True is the process succeeded. False otherwise</returns>
    internal bool TryRemoveStream(long streamId)
    {
        lock (_openStreams)
        {
            return _openStreams.Remove(streamId);
        }
    }
}
