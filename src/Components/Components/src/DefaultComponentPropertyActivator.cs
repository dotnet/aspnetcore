// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class DefaultComponentPropertyActivator : IComponentPropertyActivator
{
    private readonly IComponentTypeInfoResolver _typeInfoResolver;
    private readonly ConditionalWeakTable<ComponentTypeInfo, CachedActivator> _propertyActivators = new();

    public DefaultComponentPropertyActivator()
        : this(ComponentTypeInfoResolverFactory.Default)
    {
    }

    internal DefaultComponentPropertyActivator(IComponentTypeInfoResolver typeInfoResolver)
    {
        _typeInfoResolver = typeInfoResolver;
    }

    /// <inheritdoc />
    public Action<IServiceProvider, IComponent> GetActivator(
        [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        return GetActivator(_typeInfoResolver.GetRequiredTypeInfo(componentType));
    }

    internal Action<IServiceProvider, IComponent> GetActivator(ComponentTypeInfo typeInfo)
    {
        return _propertyActivators.GetValue(
            typeInfo,
            static info => new CachedActivator(CreatePropertyActivator(info.Type, info.Injectables))).Value;
    }

    private static Action<IServiceProvider, IComponent> CreatePropertyActivator(
        Type type,
        IReadOnlyList<ComponentInjectableDescriptor> descriptors)
    {
        if (descriptors.Count == 0)
        {
            return static (_, _) => { };
        }

        var injectables = new List<Injectable>(descriptors.Count);
        foreach (var descriptor in descriptors)
        {
            injectables.Add(new Injectable(
                descriptor.Name,
                descriptor.ServiceType,
                descriptor.Attribute.Key,
                descriptor.HasSetter,
                descriptor.SetValue));
        }

        return CreatePropertyActivator(type, injectables);
    }

    private static Action<IServiceProvider, IComponent> CreatePropertyActivator(Type type, List<Injectable> injectables)
    {
        if (injectables.Count == 0)
        {
            return static (_, _) => { };
        }

        return Initialize;

        // Return an action whose closure can write all the injected properties
        // without any further reflection calls (just typecasts)
        void Initialize(IServiceProvider serviceProvider, IComponent component)
        {
            foreach (var (propertyName, _, _, hasSetter, _) in injectables)
            {
                if (!hasSetter)
                {
                    throw new InvalidOperationException(
                        $"Cannot provide a value for property '{propertyName}' on type '{type.FullName}' because the property has no setter.");
                }
            }

            foreach (var (propertyName, propertyType, serviceKey, _, setValue) in injectables)
            {
                object? serviceInstance;

                if (serviceKey is not null)
                {
                    if (serviceProvider is not IKeyedServiceProvider keyedServiceProvider)
                    {
                        throw new InvalidOperationException($"Cannot provide a value for property " +
                            $"'{propertyName}' on type '{type.FullName}'. The service provider " +
                            $"does not implement '{nameof(IKeyedServiceProvider)}' and therefore " +
                            $"cannot provide keyed services.");
                    }

                    serviceInstance = keyedServiceProvider.GetKeyedService(propertyType, serviceKey)
                        ?? throw new InvalidOperationException($"Cannot provide a value for property " +
                        $"'{propertyName}' on type '{type.FullName}'. There is no " +
                        $"registered keyed service of type '{propertyType}' with key '{serviceKey}'.");
                }
                else
                {
                    serviceInstance = serviceProvider.GetService(propertyType)
                        ?? throw new InvalidOperationException($"Cannot provide a value for property " +
                        $"'{propertyName}' on type '{type.FullName}'. There is no " +
                        $"registered service of type '{propertyType}'.");
                }

                setValue(component, serviceInstance);
            }
        }
    }

    private readonly record struct Injectable(
        string PropertyName,
        Type PropertyType,
        object? ServiceKey,
        bool HasSetter,
        Action<object, object?> SetValue);

    private sealed record CachedActivator(Action<IServiceProvider, IComponent> Value);
}