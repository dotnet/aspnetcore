// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class ServerAddressesFeature : IServerAddressesFeature
{
    public ICollection<string> Addresses { get; set; } = Array.Empty<string>();
    public bool PreferHostingUrls { get; set; }
}
