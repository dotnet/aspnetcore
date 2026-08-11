// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web;

internal sealed class ComponentBindableTypeResolver : IBindableTypeResolver
{
    private readonly Dictionary<Type, BindableTypeDescriptor> _bindableTypesByType = [];

    public ComponentBindableTypeResolver(IEnumerable<RazorComponentsMetadataContext> contexts)
    {
        foreach (var context in contexts)
        {
            foreach (var descriptor in context.BindableTypes)
            {
                _bindableTypesByType[descriptor.Type] = descriptor;
            }
        }
    }

    public bool TryGetBindableTypeDescriptor(
        Type type,
        [NotNullWhen(true)] out BindableTypeDescriptor? descriptor)
        => _bindableTypesByType.TryGetValue(type, out descriptor);
}
