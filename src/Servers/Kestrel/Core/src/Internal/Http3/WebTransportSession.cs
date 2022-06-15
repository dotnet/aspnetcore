// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class WebTransportSession
{
    private bool _aborted;
    private readonly Dictionary<long, IHttp3Stream> _openStreams = new(); // todo should this maybe just be a list as I am not actually indexing based on id?
    private static bool _webtransportEnabled;
    private Http3BidirectionalStream _controlStream = default!;

    public static string VersionHeaderPrefix => "sec-webtransport-http3-draft";

    // Order is important for both of these arrays as we choose the first
    // supported version (index 0 is the most prefered option)
    public static readonly byte[][] suppportedWebTransportVersionsBytes =
    {
         new byte[] { 0x64, 0x72, 0x61, 0x66, 0x74, 0x30, 0x32 } // "draft02" 
    };

    public static readonly string[] suppportedWebTransportVersions =
    {
        "draft02"
    };

    public long SessionId => _controlStream.StreamId;

    public bool Initialize(Http3BidirectionalStream controlStream, string version)
    {
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", out _webtransportEnabled);

        if (_webtransportEnabled)
        {
            _controlStream = controlStream;
            _aborted = false;
        }

        // todo once we support multiple versions, actually use the version

        return _webtransportEnabled;
    }

    /// <summary>
    /// Adds a new stream to the internal list of open streams.
    /// </summary>
    /// <param name="stream">A reference to the new stream that is being added</param>
    public void AddStream(IHttp3Stream stream)
    {
        ThrowIfInvalidSession();

        _openStreams.Add(stream.StreamId, stream);
    }

    /// <summary>
    /// Tries to remove a stream from the internal list of open streams.
    /// </summary>
    /// <param name="streamId">A reference to the new stream that is being added</param>
    /// <returns>True is the process succeeded. False otherwise</returns>
    public bool TryRemoveStream(long streamId)
    {
        // Don't check for if the session was aborted because
        // removing streams is part of the process
        ThrowIfInvalidSessionCore();

        var t = _openStreams.Remove(streamId);
        var f = t ? "Sucessfully" : "Failed to";
        Console.WriteLine($"{f} Removed stream with id: {streamId}. {_openStreams.Count} stream left.");

        return t;
    }

    // TODO add a graceful close
    // TODO handle the client closing (abort and graceful)
    // TODO handle a stream ending/completing (I think I did this but I need to make sure)
    public void Abort()
    {
        ThrowIfInvalidSession();

        _aborted = true;

        Console.WriteLine("Aborting the WebTransport session");

        _controlStream.Abort(new Connections.ConnectionAbortedException(), System.Net.Http.Http3ErrorCode.NoError);
        foreach (var stream in _openStreams)
        {
            stream.Value.Abort(new Connections.ConnectionAbortedException(), System.Net.Http.Http3ErrorCode.NoError);
            Console.WriteLine($"Aborted stream with id: {stream.Value.StreamId}. {_openStreams.Count} stream left.");
        }

        _openStreams.Clear();

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

    private void ThrowIfInvalidSessionCore()
    {
        if (!_webtransportEnabled)
        {
            throw new Exception("WebTransport is currently disabled.");
        }
    }
}
