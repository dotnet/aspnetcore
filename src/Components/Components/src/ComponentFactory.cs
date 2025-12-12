// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.RenderTree;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentFactory
{
    // This switch is unsupported and will be removed in a future version.
    private static readonly bool _propertyInjectionDisabled =
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Components.Unsupported.DisablePropertyInjection", out var isDisabled) &&
        isDisabled;

    private static readonly ConcurrentDictionary<Type, IComponentRenderMode?> _cachedComponentTypeRenderModes = new();

    static ComponentFactory()
    {
        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCache;
        }
    }

    private readonly IComponentActivator _componentActivator;
    private readonly IComponentPropertyActivator _propertyActivator;
    private readonly Renderer _renderer;

    public ComponentFactory(IComponentActivator componentActivator, IComponentPropertyActivator propertyActivator, Renderer renderer)
    {
        _componentActivator = componentActivator ?? throw new ArgumentNullException(nameof(componentActivator));
        _propertyActivator = propertyActivator ?? throw new ArgumentNullException(nameof(propertyActivator));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public static void ClearCache() => _cachedComponentTypeRenderModes.Clear();

    private static IComponentRenderMode? GetComponentTypeRenderMode([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        // Unfortunately we can't use 'GetOrAdd' here because the DynamicallyAccessedMembers annotation doesn't flow through to the
        // callback, so it becomes an IL2111 warning. The following is equivalent and thread-safe because it's a ConcurrentDictionary
        // and it doesn't matter if we build a cache entry more than once.
        if (!_cachedComponentTypeRenderModes.TryGetValue(componentType, out var renderMode))
        {
            renderMode = componentType.GetCustomAttribute<RenderModeAttribute>()?.Mode;
            _cachedComponentTypeRenderModes.TryAdd(componentType, renderMode);
        }

        return renderMode;
    }

    public IComponent InstantiateComponent(IServiceProvider serviceProvider, [DynamicallyAccessedMembers(Component)] Type componentType, IComponentRenderMode? callerSpecifiedRenderMode, int? parentComponentId)
    {
        var componentTypeRenderMode = GetComponentTypeRenderMode(componentType);
        IComponent component;

        if (componentTypeRenderMode is null && callerSpecifiedRenderMode is null)
        {
            // Typical case where no rendermode is specified in either location. We don't call ResolveComponentForRenderMode in this case.
            component = _componentActivator.CreateInstance(componentType);
        }
        else
        {
            // At least one rendermode is specified. We require that it's exactly one, and use ResolveComponentForRenderMode with it.
            var effectiveRenderMode = callerSpecifiedRenderMode is null
                ? componentTypeRenderMode!
                : componentTypeRenderMode is null
                    ? callerSpecifiedRenderMode
                    : throw new InvalidOperationException($"The component type '{componentType}' has a fixed rendermode of '{componentTypeRenderMode}', so it is not valid to specify any rendermode when using this component.");
            component = _renderer.ResolveComponentForRenderMode(componentType, parentComponentId, _componentActivator, effectiveRenderMode);
        }

        if (component is null)
        {
            // The default activator/resolver will never do this, but an externally-supplied one might
            throw new InvalidOperationException($"The component activator returned a null value for a component of type {componentType.FullName}.");
        }

        if (!_propertyInjectionDisabled)
        {
            PerformPropertyInjection(serviceProvider, component);
        }

        return component;
    }

    private void PerformPropertyInjection(IServiceProvider serviceProvider, IComponent instance)
    {
        // Suppressed with "pragma warning disable" so ILLink Roslyn Anayzer doesn't report the warning.
#pragma warning disable IL2072 // 'componentType' argument does not satisfy 'DynamicallyAccessedMemberTypes.All' in call to 'IComponentPropertyActivator.GetActivator(Type)'.
        var propertyActivator = _propertyActivator.GetActivator(instance.GetType());
#pragma warning restore IL2072

        propertyActivator(serviceProvider, instance);
    }
}
