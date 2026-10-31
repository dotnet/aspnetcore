// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Hosting.Server.Features;

/// <summary>
/// Specifies the address used by the server.
/// </summary>
public class ServerAddressesFeature : IServerAddressesFeature
{
    /// <inheritdoc />
    public ICollection<string> Addresses { get; } = new List<string>();

    /// <inheritdoc />
    public bool PreferHostingUrls { get; set; }
}
