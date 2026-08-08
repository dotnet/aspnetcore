// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components.RenderTree;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentFactory
{
    // This switch is unsupported and will be removed in a future version.
    private static readonly bool _propertyInjectionDisabled =
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Components.Unsupported.DisablePropertyInjection", out var isDisabled) &&
        isDisabled;

    private readonly IComponentActivator _componentActivator;
    private readonly IComponentPropertyActivator _propertyActivator;
    private readonly Renderer _renderer;
    private readonly IComponentTypeInfoResolver _typeInfoResolver;

    public ComponentFactory(IComponentActivator componentActivator, IComponentPropertyActivator propertyActivator, Renderer renderer)
    {
        _componentActivator = componentActivator ?? throw new ArgumentNullException(nameof(componentActivator));
        _propertyActivator = propertyActivator ?? throw new ArgumentNullException(nameof(propertyActivator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _typeInfoResolver = renderer.ComponentTypeInfoResolver;
    }

    private static IComponentRenderMode? GetComponentTypeRenderMode(ComponentTypeInfo typeInfo)
    {
        RenderModeAttribute? result = null;
        foreach (var candidate in typeInfo.Metadata.OfType<RenderModeAttribute>())
        {
            if (result is not null)
            {
                throw new AmbiguousMatchException(
                    $"Multiple custom attributes of the same type '{typeof(RenderModeAttribute)}' found.");
            }

            result = candidate;
        }

        return result?.Mode;
    }

    public IComponent InstantiateComponent(IServiceProvider serviceProvider, [DynamicallyAccessedMembers(Component)] Type componentType, IComponentRenderMode? callerSpecifiedRenderMode, int? parentComponentId)
        => InstantiateComponent(
            serviceProvider,
            _typeInfoResolver.GetRequiredTypeInfo(componentType),
            callerSpecifiedRenderMode,
            parentComponentId);

    internal IComponent InstantiateComponent(
        IServiceProvider serviceProvider,
        ComponentTypeInfo typeInfo,
        IComponentRenderMode? callerSpecifiedRenderMode,
        int? parentComponentId)
    {
        var componentType = typeInfo.Type;
        if (!ComponentMetadataFeature.IsReflectionEnabledByDefault &&
            _componentActivator is not DefaultComponentActivator)
        {
            throw new NotSupportedException(
                $"The custom {nameof(IComponentActivator)} contract cannot be used when component metadata reflection is disabled.");
        }

        var componentTypeRenderMode = GetComponentTypeRenderMode(typeInfo);
        IComponent component;

        if (componentTypeRenderMode is null && callerSpecifiedRenderMode is null)
        {
            // Typical case where no rendermode is specified in either location. We don't call ResolveComponentForRenderMode in this case.
            component = CreateInstance(typeInfo);
        }
        else
        {
            // At least one rendermode is specified. We require that it's exactly one, and use ResolveComponentForRenderMode with it.
            var effectiveRenderMode = callerSpecifiedRenderMode is null
                ? componentTypeRenderMode!
                : componentTypeRenderMode is null
                    ? callerSpecifiedRenderMode
                    : throw new InvalidOperationException($"The component type '{componentType}' has a fixed rendermode of '{componentTypeRenderMode}', so it is not valid to specify any rendermode when using this component.");
            component = ResolveComponentForRenderMode(typeInfo, parentComponentId, effectiveRenderMode);
        }

        if (component is null)
        {
            // The default activator/resolver will never do this, but an externally-supplied one might
            throw new InvalidOperationException($"The component activator returned a null value for a component of type {componentType.FullName}.");
        }

        if (!_propertyInjectionDisabled)
        {
            if (!ComponentMetadataFeature.IsReflectionEnabledByDefault &&
                _propertyActivator is not DefaultComponentPropertyActivator)
            {
                throw new NotSupportedException(
                    $"The custom {nameof(IComponentPropertyActivator)} contract cannot be used when component metadata reflection is disabled.");
            }

            PerformPropertyInjection(serviceProvider, component, typeInfo);
        }

        return component;
    }

    private IComponent CreateInstance(ComponentTypeInfo typeInfo)
        => _componentActivator is DefaultComponentActivator defaultActivator
            ? defaultActivator.CreateInstance(typeInfo)
            : CreateInstanceWithCustomActivator(typeInfo);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.",
        Justification = "Custom Type-based activators are only used when component metadata reflection is enabled.")]
    private IComponent CreateInstanceWithCustomActivator(ComponentTypeInfo typeInfo)
        => _componentActivator.CreateInstance(typeInfo.Type);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.",
        Justification = "The default activator receives the supplied ComponentTypeInfo; custom Type-based activators are only used when reflection is enabled.")]
    private IComponent ResolveComponentForRenderMode(
        ComponentTypeInfo typeInfo,
        int? parentComponentId,
        IComponentRenderMode effectiveRenderMode)
    {
        if (_componentActivator is not DefaultComponentActivator defaultActivator)
        {
            return _renderer.ResolveComponentForRenderMode(
                typeInfo.Type,
                parentComponentId,
                _componentActivator,
                effectiveRenderMode);
        }

        var previousTypeInfo = defaultActivator.SetComponentTypeInfoForRenderMode(typeInfo);
        try
        {
            return _renderer.ResolveComponentForRenderMode(
                typeInfo.Type,
                parentComponentId,
                defaultActivator,
                effectiveRenderMode);
        }
        finally
        {
            defaultActivator.SetComponentTypeInfoForRenderMode(previousTypeInfo);
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.",
        Justification = "The custom Type-based property activator contract is only used when reflection metadata is enabled.")]
    private void PerformPropertyInjection(
        IServiceProvider serviceProvider,
        IComponent instance,
        ComponentTypeInfo requestedTypeInfo)
    {
        var instanceType = instance.GetType();
        var propertyActivator = _propertyActivator is DefaultComponentPropertyActivator defaultPropertyActivator &&
            instanceType == requestedTypeInfo.Type
                ? defaultPropertyActivator.GetActivator(requestedTypeInfo)
                : _propertyActivator.GetActivator(instanceType);

        propertyActivator(serviceProvider, instance);
    }
}
