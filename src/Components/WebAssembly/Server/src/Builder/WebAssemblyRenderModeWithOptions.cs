// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Server;

namespace Microsoft.AspNetCore.Builder;

internal class WebAssemblyRenderModeWithOptions(WebAssemblyComponentsEndpointOptions? options) : InteractiveWebAssemblyRenderMode
{
    public WebAssemblyComponentsEndpointOptions? EndpointOptions => options;
}
