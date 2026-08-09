// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Web;

internal sealed class ComponentMetadataResolver : IComponentMetadataResolver
{
    private readonly Dictionary<Type, ComponentDescriptor> _componentsByType;

    public ComponentMetadataResolver(IOptions<ComponentDescriptorOptions> options)
    {
        Components = [.. options.Value.Components];
        _componentsByType = [];
        foreach (var descriptor in Components)
        {
            _componentsByType[descriptor.Type] = descriptor;
        }
    }

    public IReadOnlyList<ComponentDescriptor> Components { get; }

    public bool TryGetComponentDescriptor(
        Type type,
        [NotNullWhen(true)] out ComponentDescriptor? descriptor)
        => _componentsByType.TryGetValue(type, out descriptor);
}
