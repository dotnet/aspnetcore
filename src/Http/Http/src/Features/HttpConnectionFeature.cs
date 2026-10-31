// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Default implementation for <see cref="IHttpConnectionFeature"/>.
/// </summary>
public class HttpConnectionFeature : IHttpConnectionFeature
{
    /// <inheritdoc />
    public string ConnectionId { get; set; } = default!;

    /// <inheritdoc />
    public IPAddress? LocalIpAddress { get; set; }

    /// <inheritdoc />
    public int LocalPort { get; set; }

    /// <inheritdoc />
    public IPAddress? RemoteIpAddress { get; set; }

    /// <inheritdoc />
    public int RemotePort { get; set; }
}
