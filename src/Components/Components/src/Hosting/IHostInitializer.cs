// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

/// <summary>
/// Initializes services for a component host.
/// </summary>
public interface IHostInitializer
{
    /// <summary>
    /// Gets the order in which the initializer executes.
    /// </summary>
    /// <remarks>
    /// Initializers with lower values execute first. Initializers with the same value execute in registration order.
    /// </remarks>
    int Order => 0;

    /// <summary>
    /// Gets a value that indicates whether the initializer requires JavaScript interop.
    /// </summary>
    bool RequiresJSInterop => false;

    /// <summary>
    /// Initializes services for the component host.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
