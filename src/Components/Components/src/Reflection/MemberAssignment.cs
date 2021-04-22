// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Reflection
{
    internal class MemberAssignment
    {
        public static IEnumerable<PropertyInfo> GetPropertiesIncludingInherited(
            [DynamicallyAccessedMembers(Component)] Type type,
            BindingFlags bindingFlags)
        {
            var dictionary = new Dictionary<string, object>(StringComparer.Ordinal);

            Type? currentType = type;

            while (currentType != null)
            {
                var properties = currentType.GetProperties(bindingFlags | BindingFlags.DeclaredOnly);
                foreach (var property in properties)
                {
                    if (!dictionary.TryGetValue(property.Name, out var others))
                    {
                        dictionary.Add(property.Name, property);
                    }
                    else if (!IsInheritedProperty(property, others))
                    {
                        List<PropertyInfo> many;
                        if (others is PropertyInfo single)
                        {
                            many = new List<PropertyInfo> { single };
                            dictionary[property.Name] = many;
                        }
                        else
                        {
                            many = (List<PropertyInfo>)others;
                        }
                        many.Add(property);
                    }
                }

                currentType = currentType.BaseType;
            }

            foreach (var item in dictionary)
            {
                if (item.Value is PropertyInfo property)
                {
                    yield return property;
                }
                var list = (List<PropertyInfo>)item.Value;

                var count = list.Count;
                for (var i = 0; i < count; i++)
                {
                    yield return list[i];
                }
            }
        }

        private static bool IsInheritedProperty(PropertyInfo property, object others)
        {
            if (others is PropertyInfo single)
            {
                return single.GetMethod?.GetBaseDefinition() == property.GetMethod?.GetBaseDefinition();
            }

            var many = (List<PropertyInfo>)others;
            foreach (var other in CollectionsMarshal.AsSpan(many))
            {
                if (other.GetMethod?.GetBaseDefinition() == property.GetMethod?.GetBaseDefinition())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
