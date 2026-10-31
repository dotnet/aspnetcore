// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics.RazorViews;

internal sealed class EndpointModel
{
    public string? DisplayName { get; set; }
    public string? RoutePattern { get; set; }
    public int? Order { get; set; }
    public string? HttpMethods { get; set; }
    public EndpointMetadataCollection? Metadata { get; set; }
}
