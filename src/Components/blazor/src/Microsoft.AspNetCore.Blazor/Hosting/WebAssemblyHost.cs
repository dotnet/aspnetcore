// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Http;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Components.Hosting;
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
            Services = services ?? throw new ArgumentNullException(nameof(services));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public IServiceProvider Services { get; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            // We need to do this as early as possible, it eliminates a bunch of problems. Note that what we do
            // is a bit fragile. If you see things breaking because JSRuntime.Current isn't set, then it's likely
            // that something on the startup path went wrong.
            //
            // We want to the JSRuntime created here to be the 'ambient' runtime when JS calls back into .NET. When
            // this happens in the browser it will be a direct call from Mono. We effectively needs to set the
            // JSRuntime in the 'root' execution context which implies that we want to do as part of a direct
            // call from Program.Main, and before any 'awaits'.
            JSRuntime.SetCurrentJSRuntime(_runtime);
            SetBrowserHttpMessageHandlerAsDefault();

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

                _renderer = builder.CreateRenderer();
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


            return Task.CompletedTask;
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

        private static void SetBrowserHttpMessageHandlerAsDefault()
        {
            // Within the Mono WebAssembly BCL, this is a special private static field
            // that can be assigned to override the default handler
            const string getHttpMessageHandlerFieldName = "GetHttpMessageHandler";
            var getHttpMessageHandlerField = typeof(HttpClient).GetField(
                getHttpMessageHandlerFieldName,
                BindingFlags.Static | BindingFlags.NonPublic);

            // getHttpMessageHandlerField will be null in tests, but nonnull when actually
            // running under Mono WebAssembly
            if (getHttpMessageHandlerField != null)
            {
                // Just in case you're not actually using HttpClient, defer the construction
                // of the WebAssemblyHttpMessageHandler
                var handlerSingleton = new Lazy<HttpMessageHandler>(
                    () => new WebAssemblyHttpMessageHandler());
                Func<HttpMessageHandler> handlerFactory = () => handlerSingleton.Value;
                getHttpMessageHandlerField.SetValue(null, handlerFactory);
            }
            else
            {
                // We log a warning in case this ever happens at runtime (even though there's
                // no obvious way it could be possible), but don't actually throw because that
                // would break unit tests
                Console.WriteLine("WARNING: Could not set default HttpMessageHandler because " +
                    $"'{getHttpMessageHandlerFieldName}' was not found on '{typeof(HttpClient).FullName}'.");
            }
        }
    }
}
