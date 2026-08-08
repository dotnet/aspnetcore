// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class ComponentParameterValueGetter
{
    internal static Func<object?> Create(ComponentState componentState, string propertyName)
        => Create(componentState.Component, componentState.ComponentTypeInfo, propertyName);

    internal static Func<object?> Create(object component, ComponentTypeInfo typeInfo, string propertyName)
    {
        var descriptor = FindParameter(typeInfo, propertyName);

        if (descriptor is null && ComponentMetadataFeature.IsReflectionEnabledByDefault)
        {
            descriptor = FindParameter(
                ComponentTypeInfoResolverFactory.Default.GetRequiredTypeInfo(component.GetType()),
                propertyName);
        }

        if (descriptor is null)
        {
            throw new InvalidOperationException(
                $"A property '{propertyName}' on component type '{component.GetType().FullName}' wasn't found.");
        }

        return () => descriptor.GetValue(component);
    }

    private static ComponentParameterDescriptor? FindParameter(ComponentTypeInfo typeInfo, string propertyName)
    {
        foreach (var parameter in typeInfo.Parameters)
        {
            if (string.Equals(parameter.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return null;
    }
}
