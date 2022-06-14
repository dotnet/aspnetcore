// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class WebTransportSession
{
    private readonly Dictionary<long, IHttp3Stream> _openStreams = new();
    private const int _maxSimultaneousStreams = 25; // arbitrary number 
    private static bool _webtransportEnabled;

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

    public int SessionId { get; private set; }
    public long NumOpenStreams => _openStreams.Count;

    public bool Initialize(int id, string version)
    {
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.WebTransportAndH3Datagrams", out _webtransportEnabled);

        if (_webtransportEnabled)
        {
            SessionId = id;
        }

        // todo once we support multiple versions, actually use the version

        return _webtransportEnabled;
    }

    /// <summary>
    /// Tries to add a new stream to the internal list of open streams.
    /// </summary>
    /// <param name="stream">A reference to the new stream that is being added</param>
    /// <returns>True is the process succeeded. False otherwise</returns>
    public bool TryAddStream(IHttp3Stream stream)
    {
        CheckIfValidSession();

        if (NumOpenStreams >= _maxSimultaneousStreams)
        {
            return false;
        }

        _openStreams.Add(stream.StreamId, stream);
        return true;
    }

    /// <summary>
    /// Tries to remove a stream from the internal list of open streams.
    /// </summary>
    /// <param name="streamId">A reference to the new stream that is being added</param>
    /// <returns>True is the process succeeded. False otherwise</returns>
    public bool TryRemoveStream(long streamId)
    {
        return _openStreams.Remove(streamId);
    }

    public void TerminateSession()
    {
        CheckIfValidSession();

        // todo implement: https://ietf-wg-webtrans.github.io/draft-ietf-webtrans-http3/draft-ietf-webtrans-http3.html#name-session-termination
    }

    private void CheckIfValidSession()
    {
        if (!_webtransportEnabled)
        {
            throw new Exception("WebTransport is currently disabled.");
        }
    }
}
