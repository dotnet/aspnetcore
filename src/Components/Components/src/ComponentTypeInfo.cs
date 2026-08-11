// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

internal sealed class ComponentTypeInfo
{
    private readonly ComponentDescriptor _descriptor;
    private readonly ReadOnlyCollection<ComponentParameterDescriptor> _parameters;
    private readonly ReadOnlyCollection<ComponentInjectableDescriptor> _injectables;
    private readonly ReadOnlyCollection<object> _metadata;
    private readonly IReadOnlySet<string>? _unmatchedPropertyNames;
    private readonly IReadOnlySet<string>? _publicReadableParameterNames;
    private readonly IReadOnlySet<string>? _nonPublicParameterNames;
    private readonly IReadOnlySet<string>? _missingSetterParameterNames;

    internal ComponentTypeInfo(
        ComponentDescriptor descriptor,
        IReadOnlySet<string>? unmatchedPropertyNames = null,
        IReadOnlySet<string>? publicReadableParameterNames = null,
        IReadOnlySet<string>? nonPublicParameterNames = null,
        IReadOnlySet<string>? missingSetterParameterNames = null)
        : this(
            descriptor.Type,
            descriptor.CreateInstance,
            descriptor.Parameters,
            descriptor.Injectables,
            descriptor.Metadata,
            unmatchedPropertyNames,
            publicReadableParameterNames,
            nonPublicParameterNames,
            missingSetterParameterNames)
    {
    }

    internal ComponentTypeInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        Func<IServiceProvider, IComponent>? createInstance,
        IReadOnlyList<ComponentParameterDescriptor> parameters,
        IReadOnlyList<ComponentInjectableDescriptor> injectables,
        IReadOnlyList<object> metadata,
        IReadOnlySet<string>? unmatchedPropertyNames,
        IReadOnlySet<string>? publicReadableParameterNames = null,
        IReadOnlySet<string>? nonPublicParameterNames = null,
        IReadOnlySet<string>? missingSetterParameterNames = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(injectables);
        ArgumentNullException.ThrowIfNull(metadata);

        Type = type;
        CreateInstance = createInstance;
        _parameters = Array.AsReadOnly([.. parameters]);
        _injectables = Array.AsReadOnly([.. injectables]);
        _metadata = Array.AsReadOnly([.. metadata]);
        _unmatchedPropertyNames = unmatchedPropertyNames?.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _publicReadableParameterNames = publicReadableParameterNames?.ToFrozenSet(StringComparer.Ordinal);
        _nonPublicParameterNames = nonPublicParameterNames?.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _missingSetterParameterNames = missingSetterParameterNames?.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _descriptor = new ComponentDescriptor
        {
            Type = type,
            CreateInstance = createInstance,
            Parameters = _parameters,
            Injectables = _injectables,
            Metadata = _metadata,
        };
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    internal Type Type { get; }

    internal Func<IServiceProvider, IComponent>? CreateInstance { get; }

    internal IReadOnlyList<ComponentParameterDescriptor> Parameters => _parameters;

    internal IReadOnlyList<ComponentInjectableDescriptor> Injectables => _injectables;

    internal IReadOnlyList<object> Metadata => _metadata;

    internal ComponentDescriptor Descriptor => _descriptor;

    internal IReadOnlySet<string>? UnmatchedPropertyNames => _unmatchedPropertyNames;

    internal bool IsParameterPubliclyReadable(string parameterName)
        => _publicReadableParameterNames?.Contains(parameterName) ?? true;

    internal bool IsParameterPubliclyWritable(string parameterName)
        => !(_nonPublicParameterNames?.Contains(parameterName) ?? false);

    internal bool HasParameterSetter(string parameterName)
        => !(_missingSetterParameterNames?.Contains(parameterName) ?? false);

    internal ComponentTypeInfo WithCreateInstance(Func<IServiceProvider, IComponent> createInstance)
    {
        ArgumentNullException.ThrowIfNull(createInstance);

        return new ComponentTypeInfo(
            new ComponentDescriptor
            {
                Type = Type,
                CreateInstance = createInstance,
                Parameters = _parameters,
                Injectables = _injectables,
                Metadata = _metadata,
            },
            _unmatchedPropertyNames,
            _publicReadableParameterNames,
            _nonPublicParameterNames,
            _missingSetterParameterNames);
    }
}
