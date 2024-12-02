// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Routing.Tree;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class RouteTable(TreeRouter treeRouter)
{
    private readonly TreeRouter _router = treeRouter;
    private static readonly ConcurrentDictionary<(Type, string), InboundRouteEntry> _routeEntryCache = new();

    public TreeRouter? TreeRouter => _router;

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2077:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The source field does not have matching annotations.",
        Justification = "We don't trim the user assemblies and this code is only used on the server.")]
    internal static RouteData ProcessParameters(RouteData endpointRouteData)
    {
        if (endpointRouteData.Template != null)
        {
            var entry = _routeEntryCache.GetOrAdd(
                (endpointRouteData.PageType, endpointRouteData.Template),
                ((Type page, string template) key) => RouteTableFactory.CreateEntry(key.page, key.template));

            var routeValueDictionary = new RouteValueDictionary(endpointRouteData.RouteValues);
            foreach (var kvp in endpointRouteData.RouteValues)
            {
                if (kvp.Value is string value)
                {
                    // At this point the values have already been URL decoded, but we might not have decoded '/' characters.
                    // as that can cause issues when routing the request (You wouldn't be able to accept parameters that contained '/').
                    // To be consistent with existing Blazor quirks that used Uri.UnescapeDataString, we'll replace %2F with /.
                    // We don't want to call Uri.UnescapeDataString here as that would decode other characters that we don't want to decode,
                    // for example, any value that was "double" encoded (for whatever reason) within the original URL.
                    routeValueDictionary[kvp.Key] = value.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
                }
            }
            ProcessParameters(entry, routeValueDictionary);
            return new RouteData(endpointRouteData.PageType, routeValueDictionary)
            {
                Template = endpointRouteData.Template
            };
        }
        else
        {
            return endpointRouteData;
        }
    }

    public void Route(RouteContext routeContext)
    {
        _router.Route(routeContext);
        if (routeContext.Entry != null)
        {
            ProcessParameters(routeContext.Entry, routeContext.RouteValues);
        }

        if (routeContext.RouteValues != null && routeContext.RouteValues.Count == 0)
        {
            routeContext.RouteValues = null!;
        }
        return;
    }

    private static void ProcessParameters(InboundRouteEntry entry, RouteValueDictionary routeValues)
    {
        // Add null values for unused route parameters.
        if (entry.UnusedRouteParameterNames != null)
        {
            foreach (var parameter in entry.UnusedRouteParameterNames)
            {
                routeValues[parameter] = null;
            }
        }

        foreach (var kvp in routeValues)
        {
            if (kvp.Value is string value)
            {
                // At this point the values have already been URL decoded, but we might not have decoded '/' characters.
                // as that can cause issues when routing the request (You wouldn't be able to accept parameters that contained '/').
                // To be consistent with existing Blazor quirks that used Uri.UnescapeDataString, we'll replace %2F with /.
                // We don't want to call Uri.UnescapeDataString here as that would decode other characters that we don't want to decode,
                // for example, any value that was "double" encoded (for whatever reason) within the original URL.
                routeValues[kvp.Key] = value.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
            }
        }

        foreach (var parameter in entry.RoutePattern.Parameters)
        {
            // Add null values for optional route parameters that weren't provided.
            if (!routeValues.TryGetValue(parameter.Name, out var parameterValue))
            {
                routeValues.Add(parameter.Name, null);
            }
            else if (parameter.ParameterPolicies.Count > 0 && !parameter.IsCatchAll)
            {
                // If the parameter has some well-known set of route constraints, then we need to convert the value
                // to the target type.
                for (var i = 0; i < parameter.ParameterPolicies.Count; i++)
                {
                    var policy = parameter.ParameterPolicies[i];
                    switch (policy.Content)
                    {
                        case "bool":
                            routeValues[parameter.Name] = bool.Parse((string)parameterValue!);
                            break;
                        case "datetime":
                            routeValues[parameter.Name] = DateTime.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "decimal":
                            routeValues[parameter.Name] = decimal.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "double":
                            routeValues[parameter.Name] = double.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "float":
                            routeValues[parameter.Name] = float.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "guid":
                            routeValues[parameter.Name] = Guid.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "int":
                            routeValues[parameter.Name] = int.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        case "long":
                            routeValues[parameter.Name] = long.Parse((string)parameterValue!, CultureInfo.InvariantCulture);
                            break;
                        default:
                            continue;
                    }
                }
            }
        }
    }
}
