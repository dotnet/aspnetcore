// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace DojoAgent;

/// <summary>
/// Supplies the current UI-owned state and thread identity to a direct dojo model request.
/// </summary>
public sealed class DojoRequestContext
{
    /// <summary>
    /// The key for request metadata in chat options' additional properties.
    /// </summary>
    public const string PropertyName = "dojo-request";

    /// <summary>
    /// Gets or initializes the stable identity of the UI conversation.
    /// </summary>
    public required string ThreadId { get; init; }

    /// <summary>
    /// Gets or initializes the current state supplied by the UI.
    /// </summary>
    public JsonElement? State { get; init; }
}
