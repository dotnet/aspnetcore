// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

internal sealed class DefaultComponentActivator : IComponentActivator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IComponentTypeInfoResolver _typeInfoResolver;
    private ComponentTypeInfo? _componentTypeInfoForRenderMode;

    internal DefaultComponentActivator(IServiceProvider serviceProvider)
        : this(
            serviceProvider,
            serviceProvider.GetService<IComponentTypeInfoResolver>() ?? ComponentTypeInfoResolverFactory.Create(serviceProvider))
    {
    }

    internal DefaultComponentActivator(
        IServiceProvider serviceProvider,
        IComponentTypeInfoResolver typeInfoResolver)
    {
        _serviceProvider = serviceProvider;
        _typeInfoResolver = typeInfoResolver;
    }

    /// <inheritdoc />
    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
        }

        var typeInfo = _componentTypeInfoForRenderMode is { } renderModeTypeInfo &&
            renderModeTypeInfo.Type == componentType
                ? renderModeTypeInfo
                : _typeInfoResolver.GetRequiredTypeInfo(componentType);

        return CreateInstance(typeInfo);
    }

    internal IComponent CreateInstance(ComponentTypeInfo typeInfo)
    {
        var componentType = typeInfo.Type;
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(typeInfo));
        }

        if (typeInfo.CreateInstance is { } createInstance)
        {
            return createInstance(_serviceProvider);
        }

        var factory = ActivatorUtilities.CreateFactory(componentType, Type.EmptyTypes);
        return (IComponent)factory(_serviceProvider, []);
    }

    internal ComponentTypeInfo? SetComponentTypeInfoForRenderMode(ComponentTypeInfo? typeInfo)
    {
        var previous = _componentTypeInfoForRenderMode;
        _componentTypeInfoForRenderMode = typeInfo;
        return previous;
    }
}
