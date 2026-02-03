// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Maps TempData values to a model.
/// </summary>
public interface ITempDataValueMapper
{
    /// <summary>
    /// Returns the TempData value with the specified key, deserialized to the specified type.
    /// </summary>
    /// <returns>The deserialized value, or null if not found.</returns>
    object? GetValue(string tempDataKey, Type targetType);

    /// <summary>
    /// Registers a callback to retrieve the current value of a TempData property.
    /// The callback will be invoked when the response starts to persist the value.
    /// </summary>
    void RegisterValueCallback(string tempDataKey, Func<object?> valueGetter);
}
