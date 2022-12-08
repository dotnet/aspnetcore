// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

// This wraps any other registered component activator (or the default), adding the behavior
// that a particular component instance can be pre-registered as the instance we'll use the
// first time that component type is required.
//
// This is a hack to allow endpoints to return pre-constructed component instances and have
// them inserted into the render tree inside their layouts, etc. For a real implementation,
// this technique might not be too bad, or we might choose to change the renderer and diffing
// system to have a concept of some whole new frame type describing pre-instantiated components.

internal sealed class PassiveComponentInstanceActivator : IComponentActivator
{
    private readonly IComponentActivator _underlyingActivator;
    private readonly IComponent? _componentInstance;
    private Type? _componentInstanceType;

    public PassiveComponentInstanceActivator(IServiceProvider requestServices, IComponent componentInstance)
    {
        _underlyingActivator = requestServices.GetService<IComponentActivator>()
            ?? DefaultComponentActivator.Instance;
        _componentInstanceType = componentInstance.GetType();
        _componentInstance = componentInstance;
    }

    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        // If it's the first time we're asked for the pre-supplied instance, supply it
        if (componentType == _componentInstanceType)
        {
            _componentInstanceType = null;
            return _componentInstance!;
        }

        // Otherwise fall back on the underlying implementation
        return _underlyingActivator.CreateInstance(componentType);
    }

    // TODO: Instead of duplicating this code, put it somewhere we can share the type
    internal sealed class DefaultComponentActivator : IComponentActivator
    {
        public static IComponentActivator Instance { get; } = new DefaultComponentActivator();

        public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
        {
            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
            }

            return (IComponent)Activator.CreateInstance(componentType)!;
        }
    }
}
