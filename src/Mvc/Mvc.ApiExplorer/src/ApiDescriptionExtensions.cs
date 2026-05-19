// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Extension methods for <see cref="ApiDescription"/>.
/// </summary>
public static class ApiDescriptionExtensions
{
    /// <summary>
    /// Gets the value of a property from the <see cref="ApiDescription.Properties"/> collection
    /// using the provided value of <typeparamref name="T"/> as the key.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="apiDescription">The <see cref="ApiDescription"/>.</param>
    /// <returns>The property or the default value of <typeparamref name="T"/>.</returns>
    public static T? GetProperty<T>(this ApiDescription apiDescription)
    {
        ArgumentNullException.ThrowIfNull(apiDescription);

        if (apiDescription.Properties.TryGetValue(typeof(T), out var value))
        {
            return (T)value;
        }
        else
        {
            return default(T);
        }
    }

    /// <summary>
    /// Sets the value of an property in the <see cref="ApiDescription.Properties"/> collection using
    /// the provided value of <typeparamref name="T"/> as the key.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="apiDescription">The <see cref="ApiDescription"/>.</param>
    /// <param name="value">The value of the property.</param>
    public static void SetProperty<T>(this ApiDescription apiDescription, T value)
    {
        ArgumentNullException.ThrowIfNull(apiDescription);

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        apiDescription.Properties[typeof(T)] = value;
    }
}
