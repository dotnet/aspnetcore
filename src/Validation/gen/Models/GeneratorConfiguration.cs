// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Represents the configuration options for the validation source generator.
/// </summary>
internal readonly record struct GeneratorConfiguration(AccessibilityFilter AccessibilityFilter)
{
    public static readonly GeneratorConfiguration Default = new(AccessibilityFilter.PublicOnly);
    public static GeneratorConfiguration IncludeInternalTypes() => new(AccessibilityFilter.InternalOrPublic);
}

// Filter to check if validatable type should be extracted based on it's accessibility
internal sealed record AccessibilityFilter(ImmutableHashSet<Accessibility> Hash)
{
    internal AccessibilityFilter(params Accessibility[] values) : this(ImmutableHashSet.Create(values)) {}

    internal bool Match(Accessibility value) => Hash.Contains(value);
    internal bool Match(params Accessibility[] values) => values.All(Hash.Contains);

    internal AccessibilityFilter Extend(params Accessibility[] values) => new(Hash.Union(values));
    internal AccessibilityFilter Except(params Accessibility[] values) => new(Hash.Except(values));

    internal static AccessibilityFilter PublicOnly { get; } = new(ImmutableHashSet.Create(Accessibility.Public));
    internal static AccessibilityFilter InternalOrPublic { get; } = new(ImmutableHashSet.Create(Accessibility.Public, Accessibility.Internal));
}
