// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides a dictionary for storing data that is needed for subsequent requests.
/// Data stored in TempData is automatically removed after it is read unless
/// <see cref="Keep()"/> or <see cref="Keep(string)"/> is called, or it is accessed via <see cref="Peek(string)"/>.
/// </summary>
public interface ITempData
{
    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    object? this[string key] { get; set; }

    /// <summary>
    /// Gets the value associated with the specified key and then schedules it for deletion.
    /// </summary>
    object? Get(string key);

    /// <summary>
    /// Gets the value associated with the specified key without scheduling it for deletion.
    /// </summary>
    object? Peek(string key);

    /// <summary>
    /// Makes all of the keys currently in TempData persist for another request.
    /// </summary>
    void Keep();

    /// <summary>
    /// Makes the element with the <paramref name="key"/> persist for another request.
    /// </summary>
    void Keep(string key);
}
