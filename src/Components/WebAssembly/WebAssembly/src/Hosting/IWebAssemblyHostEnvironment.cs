// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

/// <summary>
/// Provides information about the hosting environment an application is running in.
/// </summary>
public interface IWebAssemblyHostEnvironment
{
    /// <summary>
    /// Gets the name of the environment. This is configured to use the environment of the application hosting the Blazor WebAssembly application.
    /// Configured to "Production" when not specified by the host.
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the base address for the application. This is typically derived from the <c>&gt;base href&lt;</c> value in the host page.
    /// </summary>
    string BaseAddress { get; }
}
