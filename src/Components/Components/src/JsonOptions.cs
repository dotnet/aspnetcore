// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Options to configure JSON serialization settings for components.
/// </summary>
public sealed class JsonOptions
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions(DefaultJsonSerializerOptions.Instance);
}
