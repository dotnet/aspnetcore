// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Reflection
{
    internal class MemberAssignment
    {
        public static IEnumerable<PropertyInfo> GetPropertiesIncludingInherited(
            [DynamicallyAccessedMembers(Component)] Type type,
            BindingFlags bindingFlags)
        {
            var dictionary = new Dictionary<string, List<PropertyInfo>>();

            Type? currentType = type;

            while (currentType != null)
            {
                var properties = currentType.GetProperties(bindingFlags  | BindingFlags.DeclaredOnly);
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
