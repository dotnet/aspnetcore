// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

/// <summary>
/// Initializes services for a component host.
/// </summary>
/// <remarks>
/// <para>Implementations must be registered as singleton services. Initializers execute once for each host activation.</para>
/// <para>
/// The service provider passed to each method represents the active request, circuit, or WebAssembly application scope.
/// The provider and services resolved from it must not be retained after the method completes.
/// </para>
/// </remarks>
public interface IHostInitializer
{
    /// <summary>
    /// Gets the order in which the initializer executes.
    /// </summary>
    /// <remarks>
    /// Initializers with lower values execute first. Order values must be unique within a host.
    /// </remarks>
    int Order => 0;

    /// <summary>
    /// Initializes services during the host phase.
    /// </summary>
    /// <param name="services">
    /// The service provider for the active request scope, circuit scope, or WebAssembly application scope.
    /// </param>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous initialization operation.</returns>
    /// <remarks>
    /// The host phase runs before browser initialization and does not require an interactive browser.
    /// </remarks>
    Task InitializeHostAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <summary>
    /// Initializes services during the browser phase.
    /// </summary>
    /// <param name="services">
    /// The service provider for the active request scope, circuit scope, or WebAssembly application scope.
    /// </param>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous initialization operation.</returns>
    /// <remarks>
    /// The browser phase runs after host initialization when an interactive browser is available.
    /// </remarks>
    Task InitializeBrowserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
