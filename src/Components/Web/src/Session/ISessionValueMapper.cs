// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Maps session data values to a model.
/// </summary>
public interface ISessionValueMapper
{
    /// <summary>
    /// Returns the session value with the specified name, deserialized to the specified type.
    /// </summary>
    /// <param name="sessionKey">The session key.</param>
    /// <param name="targetType">The type to deserialize to.</param>
    /// <returns>The deserialized value, or null if not found.</returns>
    object? GetValue(string sessionKey, Type targetType);

    /// <summary>
    /// Registers a callback to retrieve the current value of a session property.
    /// The callback will be invoked when the response starts to persist the value.
    /// </summary>
    /// <param name="sessionKey">The session key.</param>
    /// <param name="valueGetter">A function that returns the current value.</param>
    void RegisterValueCallback(string sessionKey, Func<object?> valueGetter);
}
