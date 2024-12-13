// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.StaticAssets;

/// <summary>
/// A static asset selector. Selectors are used to discriminate between two or more assets with the same route.
/// </summary>
/// <param name="name">The name associated to the selector.</param>
/// <param name="value">The value associated to the selector and used to match against incoming requests.</param>
/// <param name="quality">The static server quality associated to this selector.</param>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class StaticAssetSelector(string name, string value, string quality)
{
    /// <summary>
    /// The name associated to the selector.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The value associated to the selector and used to match against incoming requests.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// The static asset server quality associated to this selector. Used to break ties when a request matches multiple values
    /// with the same degree of specificity.
    /// </summary>
    public string Quality { get; } = quality;

    private string GetDebuggerDisplay() => $"Name: {Name} Value: {Value} Quality: {Quality}";
}
