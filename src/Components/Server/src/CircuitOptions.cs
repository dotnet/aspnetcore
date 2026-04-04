// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.AspNetCore.Components.Server;

/// <summary>
/// Options to configure circuit handler for server-side Blazor
/// </summary>
public sealed class CircuitOptions
{
    /// <summary>
    /// Gets or sets a value that determines the maximum number of disconnected circuit state details
    /// are retained by the server.
    /// <para>
    /// When a client disconnects, ASP.NET Core Components attempts to retain state on the server for an
    /// interval. This allows the client to re-establish a connection to the existing circuit on the server
    /// without losing any state in the event of transient connection issues.
    /// </para>
    /// <para>
    /// This value determines the maximum number of circuit states retained by the server.
    /// <seealso cref="DisconnectedCircuitRetentionPeriod"/>
    /// </para>
    /// </summary>
    /// <value>
    /// Defaults to <c>100</c>.
    /// </value>
    public int DisconnectedCircuitMaxRetained { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value that determines the maximum duration state for a disconnected circuit is
    /// retained on the server.
    /// <para>
    /// When a client disconnects, ASP.NET Core Components attempts to retain state on the server for an
    /// interval. This allows the client to re-establish a connection to the existing circuit on the server
    /// without losing any state in the event of transient connection issues.
    /// </para>
    /// <para>
    /// This value determines the maximum duration circuit state is retained by the server before being evicted.
    /// <seealso cref="DisconnectedCircuitMaxRetained"/>
    /// </para>
    /// </summary>
    /// <value>
    /// Defaults to <c>3 minutes</c>.
    /// </value>
    public TimeSpan DisconnectedCircuitRetentionPeriod { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Gets or sets a value that determines the maximum number of persisted circuits state that
    /// are retained in memory by the server when no distributed cache is configured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When using a distributed cache like <see cref="HybridCache"/> this value is ignored
    /// and the configuration from <see cref="Extensions.DependencyInjection.MemoryCacheServiceCollectionExtensions.AddMemoryCache(Extensions.DependencyInjection.IServiceCollection)"/>
    /// is used instead.
    /// </para>
    /// <para>
    /// To explicitly control the in memory cache limits when using a distributed cache. Setup a separate instance in <see cref="HybridPersistenceCache"/> with
    /// the desired configuration.
    /// </para>
    /// </remarks>
    public int PersistedCircuitInMemoryMaxRetained { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the duration for which a persisted circuit is retained in memory.
    /// </summary>
    /// <remarks>
    /// When using a <see cref="HybridCache"/> based implementation this value
    /// is used for the local cache retention period.
    /// </remarks>
    public TimeSpan PersistedCircuitInMemoryRetentionPeriod { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Gets or sets the duration for which a persisted circuit is retained in the distributed cache.
    /// </summary>
    /// <remarks>
    /// This setting is ignored when using an in-memory cache implementation.
    /// </remarks>
    public TimeSpan? PersistedCircuitDistributedRetentionPeriod { get; set; } = TimeSpan.FromHours(8);

    /// <summary>
    /// Gets or sets the <see cref="HybridCache"/> instance to use for persisting circuit state across servers.
    /// </summary>
    public HybridCache? HybridPersistenceCache { get; set; }

    /// <summary>
    /// Gets or sets a value that determines whether or not to send detailed exception messages to JavaScript when an unhandled exception
    /// happens on the circuit or when a .NET method invocation through JS interop results in an exception.
    /// </summary>
    /// <remarks>
    /// This value should only be turned on in development scenarios as turning it on in production might result in the leak of
    /// sensitive information to untrusted parties.
    /// </remarks>
    /// <value>Defaults to <c>false</c>.</value>
    public bool DetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates how long the server will wait before timing out an asynchronous JavaScript function invocation.
    /// </summary>
    /// <value>
    /// Defaults to <c>1 minute</c>.
    /// </value>
    public TimeSpan JSInteropDefaultCallTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the maximum number of render batches that a circuit will buffer until an acknowledgement for the batch is
    /// received.
    /// </summary>
    /// <remarks>
    /// When the limit of buffered render batches is reached components will stop rendering and will wait until either the
    /// circuit is disconnected and disposed or at least one batch gets acknowledged.
    /// </remarks>
    /// <value>
    /// Defaults to <c>10</c>.</value>
    public int MaxBufferedUnacknowledgedRenderBatches { get; set; } = 10;

    /// <summary>
    /// Gets options for root components within the circuit.
    /// </summary>
    public CircuitRootComponentOptions RootComponents { get; } = new CircuitRootComponentOptions();

    internal IList<string> JavaScriptInitializers { get; } = new List<string>();
}
