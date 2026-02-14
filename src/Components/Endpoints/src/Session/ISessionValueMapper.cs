// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Maps session data values to a model.
/// </summary>
public interface ISessionValueMapper
{
    /// <summary>
    /// Returns the session value with the specified name, deserialized to the specified type.
    /// </summary>
    object? GetValue(string sessionKey, Type targetType);

    /// <summary>
    /// Registers a callback to retrieve the current value of a session property.
    /// The callback will be invoked when the response starts to persist the value.
    /// </summary>
    void RegisterValueCallback(string sessionKey, Func<object?> valueGetter);

    /// <summary>
    /// Unregisters a previously registered callback for the specified session key.
    /// </summary>
    void DeleteValueCallback(string sessionKey);
}
