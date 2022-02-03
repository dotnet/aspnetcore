// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Feature to set access the TLS application protocol
/// </summary>
public interface ITlsApplicationProtocolFeature
{
    /// <summary>
    /// Gets the <see cref="ReadOnlyMemory{T}"/> represeting the application protocol.
    /// </summary>
    ReadOnlyMemory<byte> ApplicationProtocol { get; }
}
