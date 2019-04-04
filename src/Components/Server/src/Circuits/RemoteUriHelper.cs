// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Browser.BrowserUriHelperInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// A Server-Side Components implementation of <see cref="IUriHelper"/>.
    /// </summary>
    public class RemoteUriHelper : UriHelperBase
    {
        private IJSRuntime _jsRuntime;
        private readonly ILogger<RemoteUriHelper> _logger;

        /// <summary>
        /// Creates a new <see cref="RemoteUriHelper"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
        public RemoteUriHelper(ILogger<RemoteUriHelper> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets or sets whether the circuit has an attached <see cref="IJSRuntime"/>.
        /// </summary>
        public bool HasAttachedJSRuntime => _jsRuntime != null;

        /// <summary>
        /// Initializes the <see cref="RemoteUriHelper"/>.
        /// </summary>
        /// <param name="uriAbsolute">The absolute URI of the current page.</param>
        /// <param name="baseUriAbsolute">The absolute base URI of the current page.</param>
        public override void InitializeState(string uriAbsolute, string baseUriAbsolute)
        {
            base.InitializeState(uriAbsolute, baseUriAbsolute);
            TriggerOnLocationChanged();
        }

        /// <summary>
        /// Initializes the <see cref="RemoteUriHelper"/>.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/> to use for interoperability.</param>
        internal void AttachJsRuntime(IJSRuntime jsRuntime)
        {
            if (_jsRuntime != null)
            {
                throw new InvalidOperationException("JavaScript runtime already initialized.");
            }
            _jsRuntime = jsRuntime;
            _jsRuntime.InvokeAsync<object>(
                    Interop.EnableNavigationInterception,
                    typeof(RemoteUriHelper).Assembly.GetName().Name,
                    nameof(NotifyLocationChanged));

            _logger.LogInformation($"{nameof(RemoteUriHelper)} initialized.");
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string uriAbsolute)
        {
            var circuit = CircuitHost.Current;
            if (circuit == null)
            {
                var message = $"{nameof(NotifyLocationChanged)} called without a circuit.";
                throw new InvalidOperationException(message);
            }

            var uriHelper = (RemoteUriHelper)circuit.Services.GetRequiredService<IUriHelper>();

            uriHelper.SetAbsoluteUri(uriAbsolute);

            uriHelper._logger.LogDebug($"Location changed to '{uriAbsolute}'.");
            uriHelper.TriggerOnLocationChanged();
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            _logger.LogDebug($"Log debug {uri} force load {forceLoad}.");

            if (_jsRuntime == null)
            {
                throw new InvalidOperationException("Navigation commands can not be issued at this time. This is because the component is being " +
                    "prerendered and the page has not yet loaded in the browser or because the circuit is currently disconnected. " +
                    "Components must wrap any navigation calls in conditional logic to ensure those navigation calls are not " +
                    "attempted during prerendering or while the client is disconnected.");
            }
            _jsRuntime.InvokeAsync<object>(Interop.NavigateTo, uri, forceLoad);
        }
    }
}
