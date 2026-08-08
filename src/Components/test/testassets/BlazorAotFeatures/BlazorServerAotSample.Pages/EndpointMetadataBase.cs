// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace BlazorServerAotSample.Pages;

[Layout(typeof(Layout.MetadataLayout))]
[InteractiveServer]
[AotEndpointMarker("base")]
public abstract class EndpointMetadataBase : ComponentBase
{
}
