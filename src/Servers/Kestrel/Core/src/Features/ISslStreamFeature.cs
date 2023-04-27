// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features;

/// <summary>
/// Feature to get access to the connection's <see cref="SslStream" />.
/// This feature will not be available for non-TLS connections or HTTP/3.
/// </summary>
public interface ISslStreamFeature
{
    /// <summary>
    /// Gets the <see cref="SslStream"/>.
    /// Note that <see cref="ISslStreamFeature"/> will not be available for non-TLS connections or HTTP/3.
    /// </summary>
    SslStream SslStream { get; }
}
