// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing;

/// <summary>
/// A <see cref="IRouteConstraint"/> that represents a known route value.
/// </summary>
public class KnownRouteValueConstraint : IRouteConstraint
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private RouteValuesCollection? _cachedValuesCollection;

    /// <summary>
    /// Initializes an instance of <see cref="KnownRouteValueConstraint"/>.
    /// </summary>
    /// <param name="actionDescriptorCollectionProvider">The <see cref="IActionDescriptorCollectionProvider"/>.</param>
    public KnownRouteValueConstraint(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
    {
        ArgumentNullException.ThrowIfNull(actionDescriptorCollectionProvider);

        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
    }

    /// <inheritdoc/>
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        ArgumentNullException.ThrowIfNull(routeKey);
        ArgumentNullException.ThrowIfNull(values);

        if (values.TryGetValue(routeKey, out var obj))
        {
            var value = Convert.ToString(obj, CultureInfo.InvariantCulture);
            if (value != null)
            {
                var actionDescriptors = GetAndValidateActionDescriptors(httpContext);

                var allValues = GetAndCacheAllMatchingValues(routeKey, actionDescriptors);
                foreach (var existingValue in allValues)
                {
                    if (string.Equals(value, existingValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private ActionDescriptorCollection GetAndValidateActionDescriptors(HttpContext? httpContext)
    {
        var actionDescriptorsProvider = _actionDescriptorCollectionProvider;

        if (actionDescriptorsProvider == null)
        {
            // Only validate that HttpContext was passed to constraint if it is needed
            ArgumentNullException.ThrowIfNull(httpContext);

            var services = httpContext.RequestServices;
            actionDescriptorsProvider = services.GetRequiredService<IActionDescriptorCollectionProvider>();
        }

        var actionDescriptors = actionDescriptorsProvider.ActionDescriptors;
        if (actionDescriptors == null)
        {
            throw new InvalidOperationException(
                Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(IActionDescriptorCollectionProvider.ActionDescriptors),
                    actionDescriptorsProvider.GetType()));
        }

        return actionDescriptors;
    }

    private string[] GetAndCacheAllMatchingValues(string routeKey, ActionDescriptorCollection actionDescriptors)
    {
        var version = actionDescriptors.Version;
        var valuesCollection = _cachedValuesCollection;

        if (valuesCollection == null ||
            version != valuesCollection.Version)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < actionDescriptors.Items.Count; i++)
            {
                var action = actionDescriptors.Items[i];

                if (action.RouteValues.TryGetValue(routeKey, out var value) &&
                    !string.IsNullOrEmpty(value))
                {
                    values.Add(value);
                }
            }

            valuesCollection = new RouteValuesCollection(version, values.ToArray());
            _cachedValuesCollection = valuesCollection;
        }

        return valuesCollection.Items;
    }

    private sealed class RouteValuesCollection
    {
        public RouteValuesCollection(int version, string[] items)
        {
            Version = version;
            Items = items;
        }

        public int Version { get; }

        public string[] Items { get; }
    }
}
