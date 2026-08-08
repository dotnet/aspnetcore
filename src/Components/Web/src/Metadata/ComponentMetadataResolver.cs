// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Builds the lookup indexes over the flattened metadata and answers the framework's internal
/// resolution contracts. This is the single place lookup semantics live, which is what keeps
/// <see cref="RazorComponentsMetadataContext"/> data only.
/// </summary>
internal sealed class ComponentMetadataResolver
    : IComponentMetadataResolver, IBindableTypeResolver, IComponentJsonMetadataResolver
{
    private readonly Dictionary<Type, ComponentDescriptor> _componentsByType;
    private readonly Dictionary<Type, BindableTypeDescriptor> _bindableTypesByType;

    public ComponentMetadataResolver(IOptions<ComponentMetadataOptions> options)
    {
        var value = options.Value;

        Components = [.. value.Components];

        // A later descriptor for the same type wins, so a framework-supplied description can be
        // overridden by an application that describes the same component.
        _componentsByType = [];
        foreach (var descriptor in value.Components)
        {
            _componentsByType[descriptor.Type] = descriptor;
        }

        _bindableTypesByType = [];
        foreach (var descriptor in value.BindableTypes)
        {
            _bindableTypesByType[descriptor.Type] = descriptor;
        }

        JsonTypeInfoResolver = value.JsonTypeInfoResolvers.Count switch
        {
            0 => null,
            1 => value.JsonTypeInfoResolvers[0],
            _ => System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver.Combine([.. value.JsonTypeInfoResolvers]),
        };
    }

    public IReadOnlyList<ComponentDescriptor> Components { get; }

    public IJsonTypeInfoResolver? JsonTypeInfoResolver { get; }

    public bool TryGetComponentDescriptor(Type type, [NotNullWhen(true)] out ComponentDescriptor? descriptor)
        => _componentsByType.TryGetValue(type, out descriptor);

    public bool TryGetBindableTypeDescriptor(Type type, [NotNullWhen(true)] out BindableTypeDescriptor? descriptor)
        => _bindableTypesByType.TryGetValue(type, out descriptor);
}
