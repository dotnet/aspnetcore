// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class BlazorWebEndpointMetadata(RequestDelegate requestDelegate)
{
    public RequestDelegate RequestDelegate { get; } = requestDelegate;

    internal IRouteDiagnosticsMetadata? GeneratedDiagnosticsMetadata { get; set; }

    internal HashSet<object> GeneratedResourceMetadata { get; } = new(ReferenceEqualityComparer.Instance);
}
