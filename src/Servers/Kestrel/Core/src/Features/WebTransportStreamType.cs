// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Represents the different types fo WebTransport streams
/// </summary>
[RequiresPreviewFeatures("WebTransport is a preview feature")]
public enum WebTransportStreamType
{
    /// <summary>
    /// Represents a bidirectional WebTransport stream
    /// </summary>
    Bidirectional,
    /// <summary>
    /// Represents a unidirectional inbound WebTransport stream
    /// </summary>
    Input,
    /// <summary>
    /// Represents a unidirectional outbound WebTransport stream
    /// </summary>
    Output,
}
