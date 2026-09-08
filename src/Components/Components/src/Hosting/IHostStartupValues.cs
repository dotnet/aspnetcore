// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

/// <summary>
/// Provides startup values collected from the host environment.
/// </summary>
/// <remarks>Keys are compared using ordinal comparison.</remarks>
public interface IHostStartupValues
{
    /// <summary>
    /// Gets the startup value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the startup value.</param>
    /// <returns>The startup value, or <see langword="null"/> if the key was not provided.</returns>
    string? GetValue(string key);

    /// <summary>
    /// Gets the startup value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the startup value.</param>
    /// <returns>The startup value.</returns>
    /// <exception cref="InvalidOperationException">The key was not provided.</exception>
    string GetRequired(string key);
}
