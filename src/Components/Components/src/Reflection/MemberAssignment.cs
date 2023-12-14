// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Reflection;

internal sealed class MemberAssignment
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
                continue;
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
