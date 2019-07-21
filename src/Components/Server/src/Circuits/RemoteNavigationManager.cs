// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// A Server-Side Components implementation of <see cref="NavigationManager"/>.
    /// </summary>
    public class RemoteNavigationManager : NavigationManager
    {
        private readonly ILogger<RemoteNavigationManager> _logger;
        private IJSRuntime _jsRuntime;

        /// <summary>
        /// Creates a new <see cref="RemoteNavigationManager"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
        public RemoteNavigationManager(ILogger<RemoteNavigationManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets or sets whether the circuit has an attached <see cref="IJSRuntime"/>.
        /// </summary>
        public bool HasAttachedJSRuntime => _jsRuntime != null;

        /// <summary>
        /// Initializes the <see cref="RemoteNavigationManager"/>.
        /// </summary>
        /// <param name="uriAbsolute">The absolute URI of the current page.</param>
        /// <param name="baseUriAbsolute">The absolute base URI of the current page.</param>
        public override void InitializeState(string uriAbsolute, string baseUriAbsolute)
        {
            base.InitializeState(uriAbsolute, baseUriAbsolute);
            TriggerOnLocationChanged(isinterceptedLink: false);
        }

        /// <summary>
        /// Initializes the <see cref="RemoteNavigationManager"/>.
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
                Interop.ListenForNavigationEvents,
                typeof(RemoteNavigationManager).Assembly.GetName().Name,
                nameof(NotifyLocationChanged));
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string uriAbsolute, bool isInterceptedLink)
        {
            var circuit = CircuitHost.Current;
            if (circuit == null)
            {
                var message = $"{nameof(NotifyLocationChanged)} called without a circuit.";
                throw new InvalidOperationException(message);
            }

            var navigationManager = (RemoteNavigationManager)circuit.Services.GetRequiredService<NavigationManager>();
            Log.ReceivedLocationChangedNotification(navigationManager._logger, uriAbsolute, isInterceptedLink);

            navigationManager.SetAbsoluteUri(uriAbsolute);
            navigationManager.TriggerOnLocationChanged(isInterceptedLink);
        }

        /// <inheritdoc />
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Log.RequestingNavigation(_logger, uri, forceLoad);

            if (_jsRuntime == null)
            {
                var absoluteUriString = ToAbsoluteUri(uri).ToString();
                throw new NavigationException(absoluteUriString);
            }

            _jsRuntime.InvokeAsync<object>(Interop.NavigateTo, uri, forceLoad);
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, bool, Exception> _requestingNavigation =
                LoggerMessage.Define<string, bool>(LogLevel.Debug, new EventId(1, "RequestingNavigation"), "Requesting navigation to URI {Uri} with forceLoad={ForceLoad}");

            private static readonly Action<ILogger, string, bool, Exception> _receivedLocationChangedNotification =
                LoggerMessage.Define<string, bool>(LogLevel.Debug, new EventId(2, "ReceivedLocationChangedNotification"), "Received notification that the URI has changed to {Uri} with isIntercepted={IsIntercepted}");

            public static void RequestingNavigation(ILogger logger, string uri, bool forceLoad)
            {
                _requestingNavigation(logger, uri, forceLoad, null);
            }

            public static void ReceivedLocationChangedNotification(ILogger logger, string uri, bool isIntercepted)
            {
                _receivedLocationChangedNotification(logger, uri, isIntercepted, null);
            }
        }
    }
}
