// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.StaticAssets;

// Represents a selector for a static resource.
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class StaticAssetSelector(string name, string value, string quality)
{
    public string Name { get; } = name;
    public string Value { get; } = value;
    public string Quality { get; } = quality;

    private string GetDebuggerDisplay() => $"Name: {Name} Value:{Value} Quality:{Quality}";
}
