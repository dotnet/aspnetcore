// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Reflection
{
    internal class MemberAssignment
    {
        public static IEnumerable<PropertyInfo> GetPropertiesIncludingInherited(
            Type type, BindingFlags bindingFlags)
        {
            var dictionary = new Dictionary<string, List<PropertyInfo>>();

            Type? currentType = type;

            while (currentType != null)
            {
                var properties = currentType.GetProperties(bindingFlags)
                    .Where(prop => prop.DeclaringType == currentType);
                foreach (var property in properties)
                {
                    if (!dictionary.TryGetValue(property.Name, out var others))
                    {
                        others = new List<PropertyInfo>();
                        dictionary.Add(property.Name, others);
                    }

                    if (others.Any(other => other.GetMethod?.GetBaseDefinition() == property.GetMethod?.GetBaseDefinition()))
                    {
                        // This is an inheritance case. We can safely ignore the value of property since
                        // we have seen a more derived value.
                        continue;
                    }

                    others.Add(property);
                }

                currentType = currentType.BaseType;
            }

            return dictionary.Values.SelectMany(p => p);
        }
    }
}
