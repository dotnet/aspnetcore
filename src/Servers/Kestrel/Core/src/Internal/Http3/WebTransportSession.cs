// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class WebTransportSession
{
    private readonly Dictionary<long, IHttp3Stream> _openStreams = new();
    private static readonly int maxSimultaneousStreams = 25; // arbitrary number 

    public static readonly string supportedVersionHeader = "sec-webtransport-http3-draft";

    // TODO use instead of the hardcoded values in Startup.cs in the sample app
    public static readonly string[] suppportedWebTransportVersions =
    {
        "draft02"
    };

    public readonly int sessionId;
    public long numOpenStreams => _openStreams.Count;

    public WebTransportSession(int id)
    {
        sessionId = id;
    }

    /// <summary>
    /// Tries to add a new stream to the internal list of open streams.
    /// </summary>
    /// <param name="stream">A reference to the new stream that is being added</param>
    /// <returns>True is the process succeeded. False otherwise</returns>
    public bool TryAddStream(ref IHttp3Stream stream)
    {
        if (numOpenStreams >= maxSimultaneousStreams)
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
        // todo implement: https://ietf-wg-webtrans.github.io/draft-ietf-webtrans-http3/draft-ietf-webtrans-http3.html#name-session-termination
    }
}
