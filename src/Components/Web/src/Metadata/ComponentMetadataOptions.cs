// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Holds the compile-time metadata contributed by every registered
/// <see cref="RazorComponentsMetadataContext"/>, flattened into one set of lists.
/// </summary>
/// <remarks>
/// Flattened descriptors rather than a list of contexts, so the indexes are built once at configuration
/// time instead of on every lookup, and so framework-supplied descriptors can be contributed without
/// inventing a context to hold them.
/// </remarks>
internal sealed class ComponentMetadataOptions
{
    public IList<ComponentDescriptor> Components { get; } = [];

    public IList<BindableTypeDescriptor> BindableTypes { get; } = [];

    public IList<JSInvokableMethodDescriptor> JSInvokableMethods { get; } = [];

    public IList<IJsonTypeInfoResolver> JsonTypeInfoResolvers { get; } = [];
}
