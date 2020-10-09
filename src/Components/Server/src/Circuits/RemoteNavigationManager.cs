// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Components.Web.BrowserNavigationManagerInterop;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    /// <summary>
    /// A Server-Side Blazor implementation of <see cref="NavigationManager"/>.
    /// </summary>
    internal class RemoteNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
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
        /// Initializes the <see cref="NavigationManager" />.
        /// </summary>
        /// <param name="baseUri">The base URI.</param>
        /// <param name="uri">The absolute URI.</param>
        public new void Initialize(string baseUri, string uri)
        {
            base.Initialize(baseUri, uri);
            NotifyLocationChanged(isInterceptedLink: false);
        }

        /// <summary>
        /// Initializes the <see cref="RemoteNavigationManager"/>.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/> to use for interoperability.</param>
        public void AttachJsRuntime(IJSRuntime jsRuntime)
        {
            if (_jsRuntime != null)
            {
                throw new InvalidOperationException("JavaScript runtime already initialized.");
            }

            _jsRuntime = jsRuntime;
        }

        public void NotifyLocationChanged(string uri, bool intercepted)
        {
            Log.ReceivedLocationChangedNotification(_logger, uri, intercepted);

            Uri = uri;
            NotifyLocationChanged(intercepted);
        }

        public bool HandleLocationChanging(string uri, bool intercepted)
        {
            return NotifyLocationChanging(uri, intercepted);
        }

        /// <inheritdoc />
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            Log.RequestingNavigation(_logger, uri, forceLoad);

            if (_jsRuntime == null)
            {
                var absoluteUriString = ToAbsoluteUri(uri).ToString();
                throw new NavigationException(absoluteUriString);
            }

            //In serverside blazor we call the locationChanging event here to avoid an extra browser / server roundtrip
            //When forceload is set we bypass the LocationChanging event and force the browser to load the new page from the server
            if (forceLoad || !NotifyLocationChanging(uri, false))
            {
                _jsRuntime.InvokeAsync<object>(Interop.NavigateTo, uri, forceLoad);
            }
            else
            {
                Log.NavigationCanceled(_logger, uri);
            }
        }

        /// <inheritdoc />
        protected override void SetHasLocationChangingListeners(bool value)
        {
            _jsRuntime?.InvokeAsync<object>(Interop.SetHasLocationChangingListeners, value);
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, bool, Exception> _requestingNavigation =
                LoggerMessage.Define<string, bool>(LogLevel.Debug, new EventId(1, "RequestingNavigation"), "Requesting navigation to URI {Uri} with forceLoad={ForceLoad}");

            private static readonly Action<ILogger, string, bool, Exception> _receivedLocationChangedNotification =
                LoggerMessage.Define<string, bool>(LogLevel.Debug, new EventId(2, "ReceivedLocationChangedNotification"), "Received notification that the URI has changed to {Uri} with isIntercepted={IsIntercepted}");

            private static readonly Action<ILogger, string, Exception> _navigationCanceled =
                LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "NavigationCanceled"), "Navigation to URI {Uri} canceled");

            public static void RequestingNavigation(ILogger logger, string uri, bool forceLoad)
            {
                _requestingNavigation(logger, uri, forceLoad, null);
            }

            public static void ReceivedLocationChangedNotification(ILogger logger, string uri, bool isIntercepted)
            {
                _receivedLocationChangedNotification(logger, uri, isIntercepted, null);
            }

            public static void NavigationCanceled(ILogger logger, string uri)
            {
                _navigationCanceled(logger, uri, null);
            }
        }
    }
}
