// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Routing.Tree;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class RouteTable(TreeRouter treeRouter)
{
    private readonly TreeRouter _router = treeRouter;

    public TreeRouter? TreeRouter => _router;

    public void Route(RouteContext routeContext)
    {
        _router.Route(routeContext);
        if (routeContext.Entry != null)
        {
            if (routeContext.Entry.UnusedRouteParameterNames != null)
            {
                foreach (var parameter in routeContext.Entry.UnusedRouteParameterNames)
                {
                    routeContext.RouteValues[parameter] = null;
                }
            }

            foreach (var parameter in routeContext.Entry.RoutePattern.Parameters)
            {
                if (!routeContext.RouteValues.TryGetValue(parameter.Name, out var parameterValue))
                {
                    routeContext.RouteValues.Add(parameter.Name, null);
                }
                else if (parameter.ParameterPolicies.Count > 0 && !parameter.IsCatchAll)
                {
                    for (var i = 0; i < parameter.ParameterPolicies.Count; i++)
                    {
                        var policy = parameter.ParameterPolicies[i];
                        switch (policy.Content)
                        {
                            case "bool":
                                routeContext.RouteValues[parameter.Name] = bool.Parse((string)parameterValue!);
                                break;
                            case "datetime":
                                routeContext.RouteValues[parameter.Name] = DateTime.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "decimal":
                                routeContext.RouteValues[parameter.Name] = decimal.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "double":
                                routeContext.RouteValues[parameter.Name] = double.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "float":
                                routeContext.RouteValues[parameter.Name] = float.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "guid":
                                routeContext.RouteValues[parameter.Name] = Guid.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "int":
                                routeContext.RouteValues[parameter.Name] = int.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            case "long":
                                routeContext.RouteValues[parameter.Name] = long.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                                break;
                            default:
                                continue;
                        }
                    }
                }
            }
        }
        if (routeContext.RouteValues != null && routeContext.RouteValues.Count == 0)
        {
            routeContext.RouteValues = null!;
        }
        return;
    }
}
