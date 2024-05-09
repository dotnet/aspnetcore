// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.StaticAssets;

// Represents a property of an endpoint.
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class EndpointProperty(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; } = value;

    private string GetDebuggerDisplay() => $"Name: {Name} Value:{Value}";
}
