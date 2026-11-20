// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a snapshot of a message received by a message port or broadcast channel.
/// </summary>
public sealed class MessageEvent
{
    /// <summary>
    /// Gets the received JSON value, or JSON null for a native message deserialization failure.
    /// </summary>
    /// <remarks>The value remains usable after the listener returns. Non-JSON messages are rejected and logged without calling the listener.</remarks>
    public JsonElement Data { get; init; }

    /// <summary>
    /// Gets the origin of the sender, or an empty string if the browser supplies no origin.
    /// </summary>
    public string Origin { get; init; } = "";
}
