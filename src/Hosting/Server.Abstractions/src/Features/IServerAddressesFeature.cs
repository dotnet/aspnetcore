// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting.Server.Features;

/// <summary>
/// Specifies the address used by the server.
/// </summary>
public interface IServerAddressesFeature
{
    /// <summary>
    /// An <see cref="ICollection{T}" /> of addresses used by the server.
    /// </summary>
    ICollection<string> Addresses { get; }

    /// <summary>
    /// <see langword="true" /> to prefer URLs configured by the host rather than the server.
    /// </summary>
    bool PreferHostingUrls { get; set; }
}
