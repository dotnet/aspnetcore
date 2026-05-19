// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Static class that adds standard view component convention methods. This class cannot be inherited.
/// </summary>
public static class ViewComponentConventions
{
    /// <summary>
    /// The suffix for a view component name.
    /// </summary>
    public static readonly string ViewComponentSuffix = "ViewComponent";

    /// <summary>
    /// Gets the name of a component.
    /// </summary>
    /// <param name="componentType"></param>
    /// <returns></returns>
    public static string GetComponentName(TypeInfo componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var attribute = componentType.GetCustomAttribute<ViewComponentAttribute>();
        if (attribute != null && !string.IsNullOrEmpty(attribute.Name))
        {
            var separatorIndex = attribute.Name.LastIndexOf('.');
            if (separatorIndex >= 0)
            {
                return attribute.Name.Substring(separatorIndex + 1);
            }
            else
            {
                return attribute.Name;
            }
        }

        return GetShortNameByConvention(componentType);
    }

    /// <summary>
    /// Get the component's full name from a type from the <see cref="ViewComponentAttribute.Name"/> first.
    /// If not defined, the full name is the Namespace with the <see cref="GetShortNameByConvention(TypeInfo)"/>.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <returns>The full name of the component.</returns>
    public static string GetComponentFullName(TypeInfo componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var attribute = componentType.GetCustomAttribute<ViewComponentAttribute>();
        if (!string.IsNullOrEmpty(attribute?.Name))
        {
            return attribute.Name;
        }

        // If the view component didn't define a name explicitly then use the namespace + the
        // 'short name'.
        var shortName = GetShortNameByConvention(componentType);
        if (string.IsNullOrEmpty(componentType.Namespace))
        {
            return shortName;
        }
        else
        {
            return componentType.Namespace + "." + shortName;
        }
    }

    private static string GetShortNameByConvention(TypeInfo componentType)
    {
        if (componentType.Name.EndsWith(ViewComponentSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return componentType.Name.Substring(0, componentType.Name.Length - ViewComponentSuffix.Length);
        }
        else
        {
            return componentType.Name;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> if the type is a public, non abstract, non generic class which
    /// defines <see cref="ViewComponentAttribute"/>, but not the <see cref="NonViewComponentAttribute"/>
    /// and has a name that ends in ViewComponent.
    /// </summary>
    /// <param name="typeInfo">The <see cref="TypeInfo"/> to inspect.</param>
    /// <returns>If the type is a component.</returns>
    public static bool IsComponent(TypeInfo typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        if (!typeInfo.IsClass ||
            !typeInfo.IsPublic ||
            typeInfo.IsAbstract ||
            typeInfo.ContainsGenericParameters ||
            typeInfo.IsDefined(typeof(NonViewComponentAttribute)))
        {
            return false;
        }

        return
            typeInfo.Name.EndsWith(ViewComponentSuffix, StringComparison.OrdinalIgnoreCase) ||
            typeInfo.IsDefined(typeof(ViewComponentAttribute));
    }
}
