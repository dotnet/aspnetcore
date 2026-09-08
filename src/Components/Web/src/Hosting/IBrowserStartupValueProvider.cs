// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

/// <summary>
/// Declares startup values to collect from the browser.
/// </summary>
/// <remarks>
/// Keys are dot-separated JavaScript property paths resolved from <c>globalThis</c>.
/// Each path must resolve to a string value. Duplicate keys across providers are rejected.
/// </remarks>
public interface IBrowserStartupValueProvider
{
    /// <summary>
    /// Gets the JavaScript property paths to collect from the browser.
    /// </summary>
    IReadOnlyList<string> Keys { get; }
}
