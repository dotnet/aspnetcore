// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides the compile-time component metadata the framework consults before falling back to
/// reflection. Implemented in <c>Microsoft.AspNetCore.Components.Web</c>, which is the only assembly
/// that can name every kind of descriptor.
/// </summary>
internal interface IComponentMetadataResolver
{
    /// <summary>
    /// Gets every described component. Discovery and routing need to visit all of them without
    /// knowing any of their types in advance.
    /// </summary>
    IReadOnlyList<ComponentDescriptor> Components { get; }

    /// <summary>
    /// Looks up the descriptor for a component type. Activation and parameter binding always start
    /// from a type they already have.
    /// </summary>
    bool TryGetComponentDescriptor(Type type, [NotNullWhen(true)] out ComponentDescriptor? descriptor);
}
