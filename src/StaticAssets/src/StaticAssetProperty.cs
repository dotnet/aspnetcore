// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.StaticAssets;

/// <summary>
/// A property associated with a static asset.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class StaticAssetProperty(string name, string value)
{
    /// <summary>
    /// The name of the property.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The value of the property.
    /// </summary>
    public string Value { get; } = value;

    private string GetDebuggerDisplay() => $"Name: {Name} Value:{Value}";
}
