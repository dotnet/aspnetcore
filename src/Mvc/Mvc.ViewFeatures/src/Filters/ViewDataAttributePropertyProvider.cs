// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

internal static class ViewDataAttributePropertyProvider
{
    public static IReadOnlyList<LifecycleProperty> GetViewDataProperties(Type type)
    {
        List<LifecycleProperty> results = null;

        var propertyHelpers = PropertyHelper.GetVisibleProperties(type: type);

        for (var i = 0; i < propertyHelpers.Length; i++)
        {
            var propertyHelper = propertyHelpers[i];
            var property = propertyHelper.Property;
            var tempDataAttribute = property.GetCustomAttribute<ViewDataAttribute>();
            if (tempDataAttribute != null)
            {
                if (results == null)
                {
                    results = new List<LifecycleProperty>();
                }

                var key = tempDataAttribute.Key;
                if (string.IsNullOrEmpty(key))
                {
                    key = property.Name;
                }

                results.Add(new LifecycleProperty(property, key));
            }
        }

        return results;
    }
}
