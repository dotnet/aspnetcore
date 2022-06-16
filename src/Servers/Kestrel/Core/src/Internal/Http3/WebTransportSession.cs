// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

/// <summary>
/// Controls the session and streams of a WebTransport session
/// </summary>
internal class WebTransportSession : IWebTransportSession, IHttpWebTransportSessionFeature
{
    #region Public API
    long IWebTransportSession.SessionId => _controlStream.StreamId;

    async ValueTask<IWebTransportSession> IHttpWebTransportSessionFeature.AcceptAsync()
    {
        // build and flush the 200 ACK
        _controlStream.ResponseHeaders.TryAdd(VersionHeaderPrefix, _version);
        _controlStream.Output.WriteResponseHeaders(200, null, (Http.HttpResponseHeaders)_controlStream.ResponseHeaders, false, false);
        await _controlStream.Output.FlushAsync(CancellationToken.None);

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

    ValueTask<IHttp3Stream?> IWebTransportSession.OpenBidirectionalStream()
    {
        ThrowIfInvalidSession();

        return default!; // TODO Implement
    }

    ValueTask<IHttp3Stream?> IWebTransportSession.OpenUnidirectionalStream()
    {
        ThrowIfInvalidSession();

        return default!; // TODO Implement
    }
    #endregion

    private bool _aborted;
    private string _version = "";
    private readonly Dictionary<long, IHttp3Stream> _openStreams = new(); // todo should this maybe just be a list as I am not actually indexing based on id?
    private static bool _webtransportEnabled;
    private Http3BidirectionalStream _controlStream = default!;

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

    internal bool Initialize(Http3BidirectionalStream controlStream, string version)
    {
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", out _webtransportEnabled);

        if (_webtransportEnabled)
        {
            _controlStream = controlStream;
            _aborted = false;
        }

        _version = version;

        return _webtransportEnabled;
    }

    /// <summary>
    /// Adds a new stream to the internal list of open streams.
    /// </summary>
    /// <param name="stream">A reference to the new stream that is being added</param>
    internal void AddStream(IHttp3Stream stream)
    {
        ThrowIfInvalidSession();

        lock (_openStreams)
        {
            _openStreams.Add(stream.StreamId, stream);
        }
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

    // TODO add a graceful close
    // TODO handle the client closing (abort and graceful)
    // TODO handle a stream ending/completing (I think I did this but I need to make sure)
    internal void Abort(ConnectionAbortedException exception, Http3ErrorCode error)
    {
        Console.WriteLine("Aborting WebTransport session");

        ThrowIfInvalidSessionCore();

        if (_aborted)
        {
            return;
        }
        _aborted = true;

        lock (_openStreams)
        {
            _controlStream.Abort(exception, error);
            foreach (var stream in _openStreams)
            {
                stream.Value.Abort(exception, error);
            }

            _openStreams.Clear();
        }

        // Eventual todo: implement http datagrams and send the close webtransport session capsule
    }

    private void ThrowIfInvalidSession()
    {
        ThrowIfInvalidSessionCore();

        if (_aborted)
        {
            throw new Exception("Session has already been aborted.");
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
