// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing.Tree;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class RouteContext
{
    public RouteContext(string path)
    {
        Path = Uri.UnescapeDataString(path);
    }

    public string Path { get; set; }

    public RouteValueDictionary RouteValues { get; set; } = new();

    public InboundRouteEntry? Entry { get; set; }

    [DynamicallyAccessedMembers(Component)]
    public Type? Handler => Entry?.Handler;

    public IReadOnlyDictionary<string, object?>? Parameters => RouteValues;
}
