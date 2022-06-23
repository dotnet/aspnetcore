// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using System.Net;
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
internal class WebTransportSession : IWebTransportSession, IHttpWebTransportSessionFeature
{
    private static bool _webtransportEnabled;
    private readonly Dictionary<long, WebTransportStream> _openStreams = new();
    private readonly ConcurrentQueue<WebTransportStream> _pendingStreams = new();
    private readonly SemaphoreSlim _pendingAcceptStreamRequests = new(0);
    private readonly Http3Connection _connection;

    private string _version = "";
    private Http3Stream _controlStream = default!;
    private bool _initialized;
    private bool _isClosing;

    internal static string WebTransportProtocolValue => "webtransport";
    private static string SecPrefix => "sec-webtransport-http3-";
    internal static string VersionHeaderPrefix => $"{SecPrefix}draft";

    // Order is important as we choose the first supported
    // version (preferenc dereases as index increases)
    private static readonly IEnumerable<string> suppportedWebTransportVersions = new string[]
    {
        $"{VersionHeaderPrefix}02"
    };

    long IWebTransportSession.SessionId => _controlStream.StreamId;

    bool IHttpWebTransportSessionFeature.IsWebTransportRequest => _initialized;

    internal WebTransportSession(Http3Connection connection)
    {
        _connection = connection;
    }

#pragma warning disable CA2252 // WebTransport is a preview feature. Suppress this warning
    async ValueTask<IWebTransportSession> IHttpWebTransportSessionFeature.AcceptAsync(CancellationToken token)
    {
        ThrowIfInvalidSession();

        // build and flush the 200 ACK to accept the connection from the client
        _controlStream.ResponseHeaders.TryAdd(VersionHeaderPrefix, _version);
        _controlStream.Output.WriteResponseHeaders((int)HttpStatusCode.OK, null, (Http.HttpResponseHeaders)_controlStream.ResponseHeaders, false, false);
        await _controlStream.Output.FlushAsync(token);

        // add the close stream listener as we must abort the session once this stream dies
        _controlStream.OnCompleted(_ =>
        {
            Abort(new(), Http3ErrorCode.NoError);

            return Task.CompletedTask;
        }, this);

        return this;
    }
#pragma warning restore CA2252

    void IWebTransportSession.Abort()
    {
        Abort(new(), Http3ErrorCode.NoError);
    }

    public void Close()
    {
        if (_isClosing)
        {
            return;
        }

        ThrowIfInvalidSession();

        _isClosing = true;
        _initialized = false;

        _controlStream.CompleteAsync();

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

        ThrowIfInvalidSession();

        _isClosing = true;
        _initialized = false;

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
        ThrowIfInvalidSession();

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
    /// Initialize the WebTransport session and prepare it to make a connection
    /// </summary>
    /// <param name="controlStream">The stream overwhich the ENHANCED CONNECT request was established</param>
    /// <param name="supportedVersions">A list of supported versions from the client. Kestrel will pick the
    /// most recent that both client and server support.</param>
    /// <returns>True if the initialization completed successfully. False otherwise.</returns>'
    internal bool Initialize(Http3Stream controlStream, IEnumerable<string> supportedVersions)
    {
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", out _webtransportEnabled);

        if (!_webtransportEnabled)
        {
            return false;
        }

        var matches = supportedVersions.Intersect(suppportedWebTransportVersions);

        if (!matches.Any())
        {
            return false;
        }

        _controlStream = controlStream;
        _version = matches.First()[SecPrefix.Length..];
        _initialized = true;
        _isClosing = false;
        return true;
    }

    /// <summary>
    /// Adds a new stream to the internal list of open streams.
    /// </summary>
    /// <param name="stream">A reference to the new stream that is being added</param>
    internal void AddStream(WebTransportStream stream)
    {
        ThrowIfInvalidSession();

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
        ThrowIfInvalidSession();

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
        // Don't check for if the session was aborted because
        // removing streams is part of the process
        ThrowIfInvalidSessionCore();

        lock (_openStreams)
        {
            return _openStreams.Remove(streamId);
        }
    }

    private void ThrowIfInvalidSession()
    {
        ThrowIfInvalidSessionCore();

        if (!_initialized)
        {
            throw new Exception("Session has already been aborted or has never been initialized.");
        }
    }

    private static void ThrowIfInvalidSessionCore()
    {
        if (!_webtransportEnabled)
        {
            throw new Exception("WebTransport is currently disabled.");
        }
    }
}
