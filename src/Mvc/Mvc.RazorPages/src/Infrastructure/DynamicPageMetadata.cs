// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class DynamicPageMetadata : IDynamicEndpointMetadata
{
    public DynamicPageMetadata(RouteValueDictionary values)
    {
        ArgumentNullException.ThrowIfNull(values);

        Values = values;
    }

    public bool IsDynamic => true;

    public RouteValueDictionary Values { get; }
}
