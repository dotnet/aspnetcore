// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.HotReload;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

/// <summary>
/// A host object for Blazor running under WebAssembly. Use <see cref="WebAssemblyHostBuilder"/>
/// to initialize a <see cref="WebAssemblyHost"/>.
/// </summary>
public sealed class WebAssemblyHost : IAsyncDisposable
{
    private readonly AsyncServiceScope _scope;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly RootComponentMappingCollection _rootComponents;
    private readonly string? _persistedState;

    // NOTE: the host is disposable because it OWNs references to disposable things.
    //
    // The twist is that in general dispose is not going to run even if the user puts it in a using.
    // When a user refreshes or navigates away that terminates the app, like a process.exit. So the
    // dispose functionality here is basically so that it can be used in unit tests.
    //
    // Based on the APIs that exist in Blazor today it's not possible for the
    // app to get disposed, however if we add something like that in the future, most of the work is
    // already done.
    private bool _disposed;
    private bool _started;
    private WebAssemblyRenderer? _renderer;

    internal WebAssemblyHost(
        WebAssemblyHostBuilder builder,
        IServiceProvider services,
        AsyncServiceScope scope,
        string? persistedState)
    {
        // To ensure JS-invoked methods don't get linked out, have a reference to their enclosing types
        GC.KeepAlive(typeof(JSInteropMethods));

        _services = services;
        _scope = scope;
        _configuration = builder.Configuration;
        _rootComponents = builder.RootComponents;
        _persistedState = persistedState;
    }

    /// <summary>
    /// Gets the application configuration.
    /// </summary>
    public IConfiguration Configuration => _configuration;

    /// <summary>
    /// Gets the service provider associated with the application.
    /// </summary>
    public IServiceProvider Services => _scope.ServiceProvider;

    /// <summary>
    /// Disposes the host asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> which represents the completion of disposal.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_renderer != null)
        {
            await _renderer.DisposeAsync();
        }

        await _scope.DisposeAsync();

        if (_services is IAsyncDisposable asyncDisposableServices)
        {
            await asyncDisposableServices.DisposeAsync();
        }
        else if (_services is IDisposable disposableServices)
        {
            disposableServices.Dispose();
        }
    }

    /// <summary>
    /// Runs the application associated with this host.
    /// </summary>
    /// <returns>A <see cref="Task"/> which represents exit of the application.</returns>
    /// <remarks>
    /// At this time, it's not possible to shut down a Blazor WebAssembly application using imperative code.
    /// The application only stops when the hosting page is reloaded or navigated to another page. As a result
    /// the task returned from this method does not complete. This method is not suitable for use in unit-testing.
    /// </remarks>
    public Task RunAsync()
    {
        // RunAsyncCore will await until the CancellationToken fires. However, we don't fire it
        // currently, so the app will "run" forever.
        return RunAsyncCore(CancellationToken.None);
    }

    // Internal for testing.
    internal async Task RunAsyncCore(CancellationToken cancellationToken, WebAssemblyCultureProvider? cultureProvider = null)
    {
        if (_started)
        {
            throw new InvalidOperationException("The host has already started.");
        }

        _started = true;

        cultureProvider ??= WebAssemblyCultureProvider.Instance!;
        cultureProvider.ThrowIfCultureChangeIsUnsupported();

        // Application developers might have configured the culture based on some ambient state
        // such as local storage, url etc as part of their Program.Main(Async).
        // This is the earliest opportunity to fetch satellite assemblies for this selection.
        await cultureProvider.LoadCurrentCultureResourcesAsync();

        var manager = Services.GetRequiredService<ComponentStatePersistenceManager>();
        var store = !string.IsNullOrEmpty(_persistedState) ?
            new PrerenderComponentApplicationStore(_persistedState) :
            new PrerenderComponentApplicationStore();

        await manager.RestoreStateAsync(store);

        if (MetadataUpdater.IsSupported)
        {
            await WebAssemblyHotReload.InitializeAsync();
        }

        var tcs = new TaskCompletionSource();
        using (cancellationToken.Register(() => tcs.TrySetResult()))
        {
            var loggerFactory = Services.GetRequiredService<ILoggerFactory>();
            var jsComponentInterop = new JSComponentInterop(_rootComponents.JSComponents);
            var collectionProvider = Services.GetRequiredService<ResourceCollectionProvider>();
            var collection = await collectionProvider.GetResourceCollection();
            _renderer = new WebAssemblyRenderer(Services, collection, loggerFactory, jsComponentInterop);

            WebAssemblyNavigationManager.Instance.CreateLogger(loggerFactory);

            RootComponentOperationBatch? initialOperationBatch = null;
            if (Environment.GetEnvironmentVariable("__BLAZOR_WEBASSEMBLY_WAIT_FOR_ROOT_COMPONENTS") == "true")
            {
                // In Blazor web, we wait for the JS side to tell us about the components available
                // before we render the initial set of components. Any additional update goes through
                // UpdateRootComponents.
                // We do it this way to ensure that the persistent component state is only used the first time
                // the wasm runtime is initialized and is done in the same way for both webassembly and blazor
                // web.
                initialOperationBatch = await InternalJSImportMethods.GetInitialComponentUpdate();
            }

            var initializationTcs = new TaskCompletionSource();
            WebAssemblyCallQueue.Schedule((_rootComponents, _renderer, initializationTcs), async state =>
            {
                var (rootComponents, renderer, initializationTcs) = state;
                try
                {
                    // Here, we add each root component but don't await the returned tasks so that the
                    // components can be processed in parallel.
                    var count = rootComponents.Count;
                    var initialOperationCount = initialOperationBatch?.Operations.Length ?? 0;
                    var pendingRenders = new List<Task>(count + initialOperationCount);
                    for (var i = 0; i < count; i++)
                    {
                        var rootComponent = rootComponents[i];
                        pendingRenders.Add(renderer.AddComponentAsync(
                            rootComponent.ComponentType,
                            rootComponent.Parameters,
                            rootComponent.Selector));
                    }

                    if (initialOperationBatch is not null)
                    {
                        AddWebRootComponents(renderer, initialOperationBatch, pendingRenders);
                    }

                    // Now we wait for all components to finish rendering.
                    await Task.WhenAll(pendingRenders);

                    initializationTcs.SetResult();
                }
                catch (Exception ex)
                {
                    initializationTcs.SetException(ex);
                }
            });

            await initializationTcs.Task;
            store.ExistingState.Clear();

            await tcs.Task;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "These are root components which belong to the user and are in assemblies that don't get trimmed.")]
    private static void AddWebRootComponents(WebAssemblyRenderer renderer, RootComponentOperationBatch operationBatch, List<Task> pendingRenders)
    {
        var webRootComponentManager = renderer.GetOrCreateWebRootComponentManager();
        var operations = operationBatch.Operations;
        for (var i = 0; i < operations.Length; i++)
        {
            var operation = operations[i];
            if (operation.Type != RootComponentOperationType.Add)
            {
                throw new InvalidOperationException("All initial operations must be additions.");
            }

            pendingRenders.Add(webRootComponentManager.AddRootComponentAsync(
                operation.SsrComponentId,
                operation.Descriptor!.ComponentType,
                operation.Marker?.Key,
                operation.Descriptor!.Parameters));
        }

        renderer.NotifyEndUpdateRootComponents(operationBatch.BatchId);
    }
}
