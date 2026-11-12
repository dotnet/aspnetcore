// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents activity or progress content that can change while a response streams.
/// </summary>
public class ActivityContentBlock : ContentBlock
{
    /// <summary>
    /// Gets or sets the application-defined activity type.
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current activity payload.
    /// </summary>
    public JsonElement Content { get; set; }
}
