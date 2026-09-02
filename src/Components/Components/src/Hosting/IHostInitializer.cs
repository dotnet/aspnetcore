// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

/// <summary>
/// Initializes services for a component host.
/// </summary>
/// <remarks>
/// <para>Implementations must be registered as singleton services.</para>
/// <para>Initializers execute once for each host activation.</para>
/// </remarks>
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
    /// <remarks>
    /// Static server-side rendering skips initializers that require JavaScript interop. Interactive server-side
    /// rendering defers the first such initializer and the remaining ordered initializers until JavaScript interop
    /// is available.
    /// </remarks>
    bool RequiresJSInterop => false;

    /// <summary>
    /// Initializes services for the component host.
    /// </summary>
    /// <param name="services">
    /// The service provider for the active request scope, circuit scope, or WebAssembly application scope.
    /// </param>
    /// <param name="cancellationToken">A token that can cancel initialization.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous initialization operation.</returns>
    /// <remarks>
    /// The <paramref name="services"/> provider and scoped services resolved from it must not be retained after this method completes.
    /// </remarks>
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
}
