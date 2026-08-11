// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BlazorServerAotSample.Pages;

/// <summary>
/// A value stored in protected browser storage that deliberately has no JSON contract. It can only
/// round-trip through the <see cref="ThemeSerializer"/> registered for it, which is what makes it a
/// test of the custom serializer path rather than of the generated JSON contracts.
/// </summary>
public sealed class Theme(string name)
{
    public string Name { get; } = name;
}

/// <summary>
/// Serializes <see cref="Theme"/> without JSON, so the type needs no generated contract and the
/// stored payload stays readable under Native AOT.
/// </summary>
public sealed class ThemeSerializer : ProtectedBrowserStorageSerializer<Theme>
{
    public override string Serialize(Theme value) => value.Name;

    public override Theme Deserialize(string data) => new(data);
}
