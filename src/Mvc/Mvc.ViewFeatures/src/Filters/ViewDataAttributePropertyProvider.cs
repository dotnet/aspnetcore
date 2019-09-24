// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
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
}
