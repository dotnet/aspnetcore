// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// The stream id for a given stream in an HTTP/2 connection.
/// </summary>
public interface IHttp2StreamIdFeature
{
    /// <summary>
    /// Gets the id for the HTTP/2 stream.
    /// </summary>
    int StreamId { get; }
}
