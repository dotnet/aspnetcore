// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class ReflectionComponentTypeInfoResolver : IComponentTypeInfoResolver, IDisposable
{
    private const BindingFlags InjectablePropertyBindingFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly ConcurrentDictionary<Type, ComponentTypeInfo> _typeInfoCache = new();
    private readonly ConcurrentDictionary<Assembly, IReadOnlyList<ComponentTypeInfo>> _assemblyCache = new();

    internal ReflectionComponentTypeInfoResolver()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCaches;
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method.",
        Justification = "This resolver is only added when component metadata reflection is enabled.")]
    public ComponentTypeInfo? GetTypeInfo(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!_typeInfoCache.TryGetValue(componentType, out var typeInfo))
        {
            typeInfo = CreateTypeInfo(componentType);
            _typeInfoCache.TryAdd(componentType, typeInfo);
        }

        return typeInfo;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code.",
        Justification = "This resolver is only added when component metadata reflection is enabled.")]
    public ComponentTypeInfo? GetTypeInfo(string assemblyName, string typeName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);
        ArgumentNullException.ThrowIfNull(typeName);

        Assembly? targetAssembly = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
            {
                targetAssembly = assembly;
                break;
            }
        }

        if (targetAssembly is null && OperatingSystem.IsBrowser())
        {
            try
            {
                targetAssembly = Assembly.Load(assemblyName);
            }
            catch
            {
            }
        }

        var componentType = targetAssembly?.GetType(typeName, throwOnError: false, ignoreCase: false);
        return componentType is null || !typeof(IComponent).IsAssignableFrom(componentType)
            ? null
            : GetTypeInfo(componentType);
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code.",
        Justification = "This resolver is only added when component metadata reflection is enabled.")]
    public IReadOnlyList<ComponentTypeInfo> GetTypeInfos(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!_assemblyCache.TryGetValue(assembly, out var typeInfos))
        {
            var results = new List<ComponentTypeInfo>();
            foreach (var exportedType in assembly.GetExportedTypes())
            {
                if (exportedType is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false } &&
                    typeof(IComponent).IsAssignableFrom(exportedType) &&
                    GetTypeInfo(exportedType) is { } typeInfo)
                {
                    results.Add(typeInfo);
                }
            }

            typeInfos = [.. results];
            _assemblyCache.TryAdd(assembly, typeInfos);
        }

        return typeInfos;
    }

    internal void ClearCaches()
    {
        _typeInfoCache.Clear();
        _assemblyCache.Clear();
    }

    public void Dispose()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied -= ClearCaches;
        }
    }

    private static ComponentTypeInfo CreateTypeInfo([DynamicallyAccessedMembers(Component)] Type componentType)
    {
        var parameters = CreateParameterDescriptors(
            componentType,
            out var unmatchedPropertyNames,
            out var publicReadableParameterNames,
            out var nonPublicParameterNames,
            out var missingSetterParameterNames);

        return new ComponentTypeInfo(
            componentType,
            CreateComponentFactory(componentType),
            parameters,
            [.. CreateInjectableDescriptors(componentType)],
            [.. componentType.GetCustomAttributes(inherit: true)],
            unmatchedPropertyNames,
            publicReadableParameterNames,
            nonPublicParameterNames,
            missingSetterParameterNames);
    }

    private static IReadOnlyList<ComponentParameterDescriptor> CreateParameterDescriptors(
        [DynamicallyAccessedMembers(Component)] Type componentType,
        out IReadOnlySet<string> unmatchedPropertyNames,
        out IReadOnlySet<string> publicReadableParameterNames,
        out IReadOnlySet<string> nonPublicParameterNames,
        out IReadOnlySet<string> missingSetterParameterNames)
    {
        List<ComponentParameterDescriptor>? descriptors = null;
        var unmatchedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var publicReadableNames = new HashSet<string>(StringComparer.Ordinal);
        var nonPublicNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var missingSetterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in ComponentProperties.GetCandidateBindableProperties(componentType))
        {
            unmatchedNames.Add(property.Name);
            ParameterAttribute? parameterAttribute = null;
            CascadingParameterAttributeBase? cascadingParameterAttribute = null;

            foreach (var attribute in property.GetCustomAttributes())
            {
                switch (attribute)
                {
                    case ParameterAttribute parameter:
                        parameterAttribute = parameter;
                        break;
                    case CascadingParameterAttributeBase cascadingParameter:
                        cascadingParameterAttribute = cascadingParameter;
                        break;
                }
            }

            Attribute? effectiveAttribute = cascadingParameterAttribute ?? (Attribute?)parameterAttribute;
            if (effectiveAttribute is null)
            {
                continue;
            }

            descriptors ??= [];
            if (parameterAttribute is not null &&
                (property.SetMethod is null || !property.SetMethod.IsPublic))
            {
                nonPublicNames.Add(property.Name);
            }
            if (property.SetMethod is null)
            {
                missingSetterNames.Add(property.Name);
            }

            if (property.GetMethod?.IsPublic == true)
            {
                publicReadableNames.Add(property.Name);
            }

            descriptors.Add(new ComponentParameterDescriptor
            {
                Name = property.Name,
                ParameterType = property.PropertyType,
                Attribute = effectiveAttribute,
                SetValue = CreateParameterSetter(
                    componentType,
                    property,
                    requiresPublicSetter: parameterAttribute is not null),
                GetValue = CreateParameterGetter(componentType, property),
                GetStateSerializer = effectiveAttribute is PersistentStateAttribute
                    ? serviceProvider => ResolvePersistentStateSerializer(serviceProvider, property.PropertyType)
                    : null,
            });
        }

        unmatchedPropertyNames = unmatchedNames;
        publicReadableParameterNames = publicReadableNames;
        nonPublicParameterNames = nonPublicNames;
        missingSetterParameterNames = missingSetterNames;
        return descriptors ?? [];
    }

    private static IReadOnlyList<ComponentInjectableDescriptor> CreateInjectableDescriptors(
        [DynamicallyAccessedMembers(Component)] Type componentType)
    {
        List<ComponentInjectableDescriptor>? descriptors = null;

        foreach (var property in MemberAssignment.GetPropertiesIncludingInherited(componentType, InjectablePropertyBindingFlags))
        {
            if (property.GetCustomAttribute<InjectAttribute>() is not { } injectAttribute)
            {
                continue;
            }

            descriptors ??= [];
            descriptors.Add(new ComponentInjectableDescriptor
            {
                Name = property.Name,
                ServiceType = property.PropertyType,
                Attribute = injectAttribute,
                HasSetter = property.SetMethod is not null,
                SetValue = CreateInjectableSetter(componentType, property),
            });
        }

        return descriptors ?? [];
    }

    private static Func<IServiceProvider, IComponent>? CreateComponentFactory(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        if (componentType.IsAbstract || !typeof(IComponent).IsAssignableFrom(componentType))
        {
            return null;
        }

        try
        {
            var factory = ActivatorUtilities.CreateFactory(componentType, Type.EmptyTypes);
            return serviceProvider => (IComponent)factory(serviceProvider, []);
        }
        catch
        {
            return null;
        }
    }

    private static object? ResolvePersistentStateSerializer(IServiceProvider serviceProvider, Type type)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            return null;
        }

        var serializerType = typeof(PersistentComponentStateSerializer<>).MakeGenericType(type);
        return serviceProvider.GetService(serializerType);
    }

    private static Action<object, object?> CreateParameterSetter(
        Type componentType,
        PropertyInfo property,
        bool requiresPublicSetter)
    {
        if (requiresPublicSetter && (property.SetMethod is null || !property.SetMethod.IsPublic))
        {
            return (_, _) => throw new InvalidOperationException(
                $"The type '{componentType.FullName}' declares a parameter matching the name '{property.Name}' that is not public. Parameters must be public.");
        }

        if (property.SetMethod is null)
        {
            return (_, _) => throw new InvalidOperationException(
                $"Cannot provide a value for property '{property.Name}' on type '{componentType.FullName}' because the property has no setter.");
        }

        return new PropertySetter(componentType, property).SetValue;
    }

    private static Func<object, object?> CreateParameterGetter(Type componentType, PropertyInfo property)
        => property.GetMethod is null
            ? _ => throw new InvalidOperationException(
                $"Cannot provide a value for property '{property.Name}' on type '{componentType.FullName}' because the property has no getter.")
            : new PropertyGetter(componentType, property).GetValue;

    private static Action<object, object?> CreateInjectableSetter(Type componentType, PropertyInfo property)
        => property.SetMethod is null
            ? (_, _) => throw new InvalidOperationException(
                $"Cannot provide a value for property '{property.Name}' on type '{componentType.FullName}' because the property has no setter.")
            : new PropertySetter(componentType, property).SetValue;
}
