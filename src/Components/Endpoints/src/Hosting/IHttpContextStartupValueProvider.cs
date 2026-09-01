// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Hosting;

/// <summary>
/// Provides startup values from an <see cref="HttpContext"/>.
/// </summary>
/// <remarks>
/// Values are available during server rendering and are not emitted to the browser.
/// Duplicate keys across providers are rejected.
/// </remarks>
public interface IHttpContextStartupValueProvider
{
    /// <summary>
    /// Gets startup values from the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The startup values.</returns>
    IReadOnlyDictionary<string, string> GetValues(HttpContext httpContext);
}
