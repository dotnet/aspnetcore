// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class DynamicPageRouteValueTransformerMetadata : IDynamicEndpointMetadata
{
    public DynamicPageRouteValueTransformerMetadata(Type selectorType, object? state)
    {
        ArgumentNullException.ThrowIfNull(selectorType);

        if (!typeof(DynamicRouteValueTransformer).IsAssignableFrom(selectorType))
        {
            throw new ArgumentException(
                $"The provided type must be a subclass of {typeof(DynamicRouteValueTransformer)}",
                nameof(selectorType));
        }

        SelectorType = selectorType;
        State = state;
    }

    public bool IsDynamic => true;

    public object? State { get; }

    public Type SelectorType { get; }
}
