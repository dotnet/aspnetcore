// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Tree;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class RouteTable(TreeRouter treeRouter)
{
    private readonly TreeRouter _router = treeRouter;

    public TreeRouter? TreeRouter => _router;

    public void Route(RouteContext routeContext)
    {
        _router.Route(routeContext);
        return;
    }
}
