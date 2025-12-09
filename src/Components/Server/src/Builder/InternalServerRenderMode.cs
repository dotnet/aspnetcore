// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Server;

internal class InternalServerRenderMode(ServerComponentsEndpointOptions options) : InteractiveServerRenderMode
{
    public ServerComponentsEndpointOptions? Options { get; } = options;
}
