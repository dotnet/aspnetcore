// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Provides an abstraction for a provider that stores and retrieves temporary data.
/// </summary>
internal interface ITempDataProvider
{
    /// <summary>
    /// Loads temporary data from the given <see cref="HttpContext"/>.
    /// </summary>
    IDictionary<string, (object? Value, Type? Type)> LoadTempData(HttpContext context);

    /// <summary>
    /// Saves temporary data to the given <see cref="HttpContext"/>.
    /// </summary>
    void SaveTempData(HttpContext context, IDictionary<string, (object? Value, Type? Type)> values);
}
