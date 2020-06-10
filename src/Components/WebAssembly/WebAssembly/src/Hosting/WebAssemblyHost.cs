// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Rendering;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    /// <summary>
    /// A host object for Blazor running under WebAssembly. Use <see cref="WebAssemblyHostBuilder"/>
    /// to initialize a <see cref="WebAssemblyHost"/>.
    /// </summary>
    public sealed class WebAssemblyHost : IAsyncDisposable
    {
        private readonly IServiceScope _scope;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly RootComponentMapping[] _rootComponents;

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
        private WebAssemblyRenderer _renderer;

        internal WebAssemblyHost(IServiceProvider services, IServiceScope scope, IConfiguration configuration, RootComponentMapping[] rootComponents)
        {
            // To ensure JS-invoked methods don't get linked out, have a reference to their enclosing types
            GC.KeepAlive(typeof(EntrypointInvoker));
            GC.KeepAlive(typeof(JSInteropMethods));

            _services = services;
            _scope = scope;
            _configuration = configuration;
            _rootComponents = rootComponents;
        }

        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        public IConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the service provider associated with the application.
        /// </summary>
        public IServiceProvider Services => _scope.ServiceProvider;

        internal SatelliteResourcesLoader SatelliteResourcesLoader { get; set; } = new SatelliteResourcesLoader(WebAssemblyJSRuntimeInvoker.Instance);

        /// <summary>
        /// Disposes the host asynchronously.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> which respresents the completion of disposal.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _renderer?.Dispose();

            if (_scope is IAsyncDisposable asyncDisposableScope)
            {
                await asyncDisposableScope.DisposeAsync();
            }
            else
            {
                _scope?.Dispose();
            }

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
        internal async Task RunAsyncCore(CancellationToken cancellationToken)
        {
            if (_started)
            {
                throw new InvalidOperationException("The host has already started.");
            }

            _started = true;

            // EntryPointInvoker loads satellite assemblies for the application default culture.
            // Application developers might have configured the culture based on some ambient state
            // such as local storage, url etc as part of their Program.Main(Async).
            // This is the earliest opportunity to fetch satellite assemblies for this selection.
            await SatelliteResourcesLoader.LoadCurrentCultureResourcesAsync();

            var tcs = new TaskCompletionSource<object>();

            using (cancellationToken.Register(() => { tcs.TrySetResult(null); }))
            {
                var loggerFactory = Services.GetRequiredService<ILoggerFactory>();
                _renderer = new WebAssemblyRenderer(Services, loggerFactory);

                var rootComponents = _rootComponents;
                for (var i = 0; i < rootComponents.Length; i++)
                {
                    var rootComponent = rootComponents[i];
                    await _renderer.AddComponentAsync(rootComponent.ComponentType, rootComponent.Selector);
                }

                await tcs.Task;
            }
        }
    }
}
