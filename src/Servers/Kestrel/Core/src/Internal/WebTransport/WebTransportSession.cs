// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Runtime.Versioning;
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
[RequiresPreviewFeatures("WebTransport is a preview feature")]
internal class WebTransportSession : IWebTransportSession, IHttpWebTransportSessionFeature
{
    private static bool _webtransportEnabled;

    private readonly Dictionary<long, WebTransportStream> _openStreams = new();
    private readonly ConcurrentQueue<WebTransportStream> _pendingStreams = new();
    private readonly SemaphoreSlim _pendingAcceptStreamRequests = new(0);
    private readonly Http3Connection _connection;

    // these should all be effectively readonly after
    // the initialization method
    private string _version = "";
    private Http3Stream _controlStream = default!;

    private bool _initialized;

    internal static string VersionHeaderPrefix => "sec-webtransport-http3-draft";

    // Order is important for both of these arrays as we choose the first
    // supported version (index 0 is the most prefered option)
    internal static readonly byte[][] suppportedWebTransportVersionsBytes =
    {
        new byte[] { 0x64, 0x72, 0x61, 0x66, 0x74, 0x30, 0x32 } // "draft02" 
    };

    internal static readonly string[] suppportedWebTransportVersions =
    {
        "draft02"
    };

    long IWebTransportSession.SessionId => _controlStream.StreamId;

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

    void IWebTransportSession.Abort()
    {
        Abort(new(), Http3ErrorCode.NoError);
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

    internal WebTransportSession(Http3Connection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Initialize the WebTransport session and prepare it to make a connection
    /// </summary>
    /// <param name="controlStream">The stream overwhich the ENHANCED CONNECT request was established</param>
    /// <param name="version">The version of the WebTransport spec to use</param>
    /// <returns>True if the initialization completed successfully. False otherwise.</returns>
    internal bool Initialize(Http3Stream controlStream, string version) // todo merge with constructor
    {
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", out _webtransportEnabled);

        if (_webtransportEnabled)
        {
            _controlStream = controlStream;
            _initialized = true;
        }

        _version = version;

        return _webtransportEnabled;
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

    internal void Abort(ConnectionAbortedException exception, Http3ErrorCode error)
    {
        ThrowIfInvalidSessionCore();

        if (!_initialized)
        {
            return;
        }
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

        // Eventual todo: implement http datagrams and send the close webtransport session capsule
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
