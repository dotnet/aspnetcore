// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;

/// <summary>
/// Controls the session and streams of a WebTransport session
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
internal class WebTransportSession : IWebTransportSession, IHttpWebTransportSessionFeature
{
    #region Public API
    long IWebTransportSession.SessionId => _controlStream.StreamId;

    async ValueTask<IWebTransportSession> IHttpWebTransportSessionFeature.AcceptAsync(CancellationToken token)
    {
        ThrowIfInvalidSession();

        // build and flush the 200 ACK
        _controlStream.ResponseHeaders.TryAdd(VersionHeaderPrefix, _version);
        _controlStream.Output.WriteResponseHeaders(200, null, (Http.HttpResponseHeaders)_controlStream.ResponseHeaders, false, false);
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

    ValueTask<Stream?> IWebTransportSession.OpenUnidirectionalStreamAsync()
    {
        ThrowIfInvalidSession();

        return default!; // TODO Implement
    }
    #endregion

    private bool _initialized;
    private string _version = "";
    private readonly Dictionary<long, WebTransportBaseStream> _openStreams = new(); // todo should this maybe just be a list as I am not actually indexing based on id?
    private readonly ConcurrentQueue<WebTransportBaseStream> _pendingStreams = new();
    private static bool _webtransportEnabled;
    private Http3Stream _controlStream = default!;

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

    /// <summary>
    /// Initialize the WebTransport session and prepare it to make a connection
    /// </summary>
    /// <param name="controlStream">The stream overwhich the ENHANCED CONNECT request was established</param>
    /// <param name="version">The version of the WebTransport spec to use</param>
    /// <returns>True if the initialization completed successfully. False otherwise.</returns>
    internal bool Initialize(Http3Stream controlStream, string version)
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
    internal void AddStream(WebTransportBaseStream stream)
    {
        ThrowIfInvalidSession();

        lock (_pendingStreams)
        {
            _pendingStreams.Enqueue(stream);
        }
    }

    /// <summary>
    /// Pops the first stream from the list of pending streams.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to abort waiting for a stream</param>
    /// <returns>An instance of WebTransportStream that corresponds to the new stream to accept</returns>
    public ValueTask<WebTransportBaseStream?> AcceptStreamAsync(CancellationToken cancellationToken) // todo use the cancellation token
    {
        ThrowIfInvalidSession();

        _pendingStreams.TryDequeue(out var stream);
        return ValueTask.FromResult(stream);

        //var success = _pendingStreams.TryDequeue(out var stream);
        //if (success)
        //{
        //    if (stream!.Type == WebTransportStreamType.Bidirectional)
        //    {
        //        KestrelEventSource.Log.RequestQueuedStart(stream!._stream, AspNetCore.Http.HttpProtocol.Http3);
        //    }
        //    ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);

        //    return ValueTask.FromResult(stream!);
        //}
        //else
        //{
        //    // TODO implement
        //}
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
                stream.Value.Abort();// todo (exception, error);
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
