// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.StaticAssets;

/// <summary>
/// A response header to apply to the response when a static asset is served.
/// </summary>
/// <param name="name">The name of the header.</param>
/// <param name="value">The value of the header.</param>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public sealed class StaticAssetResponseHeader(string name, string value)
{
    /// <summary>
    /// The name of the header.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The value of the header.
    /// </summary>
    public string Value { get; } = value;

    private string GetDebuggerDisplay() => $"Name: {Name} Value: {Value}";
}
