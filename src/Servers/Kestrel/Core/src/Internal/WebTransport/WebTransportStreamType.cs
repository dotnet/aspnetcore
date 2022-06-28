// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

/// <summary>
/// Represents the different types of WebTransport streams.
/// </summary>
internal enum WebTransportStreamType
{
    /// <summary>
    /// Represents a bidirectional WebTransport stream.
    /// </summary>
    Bidirectional,
    /// <summary>
    /// Represents a unidirectional inbound WebTransport stream.
    /// </summary>
    Input,
    /// <summary>
    /// Represents a unidirectional outbound WebTransport stream.
    /// </summary>
    Output,
}
