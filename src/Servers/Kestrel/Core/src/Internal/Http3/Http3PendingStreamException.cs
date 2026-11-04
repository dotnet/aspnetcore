// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3PendingStreamException : Exception
{
    public Http3PendingStreamException(string message, long streamId, Exception? innerException = null)
        : base($"HTTP/3 stream error while trying to identify stream {streamId}: {message}", innerException)
    {
        StreamId = streamId;
    }

    public long StreamId { get; }
}
