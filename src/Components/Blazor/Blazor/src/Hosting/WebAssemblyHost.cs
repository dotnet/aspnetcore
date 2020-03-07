// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    internal class WebAssemblyHost : IWebAssemblyHost
    {
        private readonly IJSRuntime _runtime;

        private IServiceScope _scope;
        private WebAssemblyRenderer _renderer;

        public WebAssemblyHost(IServiceProvider services, IJSRuntime runtime)
        {
            // To ensure JS-invoked methods don't get linked out, have a reference to their enclosing types
            GC.KeepAlive(typeof(EntrypointInvoker));
            GC.KeepAlive(typeof(JSInteropMethods));
            GC.KeepAlive(typeof(WebAssemblyEventDispatcher));

            Services = services ?? throw new ArgumentNullException(nameof(services));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public IServiceProvider Services { get; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return StartAsyncAwaited();
        }

        private async Task StartAsyncAwaited()
        {
            var scopeFactory = Services.GetRequiredService<IServiceScopeFactory>();
            _scope = scopeFactory.CreateScope();

            try
            {
                var startup = _scope.ServiceProvider.GetService<IBlazorStartup>();
                if (startup == null)
                {
                    var message =
                        $"Could not find a registered Blazor Startup class. " +
                        $"Using {nameof(IWebAssemblyHost)} requires a call to {nameof(IWebAssemblyHostBuilder)}.UseBlazorStartup.";
                    throw new InvalidOperationException(message);
                }

                // Note that we differ from the WebHost startup path here by using a 'scope' for the app builder
                // as well as the Configure method.
                var builder = new WebAssemblyBlazorApplicationBuilder(_scope.ServiceProvider);
                startup.Configure(builder, _scope.ServiceProvider);

                _renderer = await builder.CreateRendererAsync();
            }
            catch
            {
                _scope.Dispose();
                _scope = null;

                if (_renderer != null)
                {
                    _renderer.Dispose();
                    _renderer = null;
                }

                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_scope != null)
            {
                _scope.Dispose();
                _scope = null;
            }

            if (_renderer != null)
            {
                _renderer.Dispose();
                _renderer = null;
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            (Services as IDisposable)?.Dispose();
        }
    }
}
