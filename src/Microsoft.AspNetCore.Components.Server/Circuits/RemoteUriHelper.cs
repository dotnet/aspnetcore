// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Interop = Microsoft.AspNetCore.Blazor.Browser.Services.BrowserUriHelperInterop;

namespace Microsoft.AspNetCore.Blazor.Server.Circuits
{
    /// <summary>
    /// A Server-Side Blazor implementation of <see cref="IUriHelper"/>.
    /// </summary>
    public class RemoteUriHelper : UriHelperBase
    {
        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// Creates a new <see cref="RemoteUriHelper"/>.
        /// </summary>
        /// <param name="jsRuntime"></param>
        public RemoteUriHelper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Initializes the <see cref="RemoteUriHelper"/>.
        /// </summary>
        /// <param name="uriAbsolute">The absolute URI of the current page.</param>
        /// <param name="baseUriAbsolute">The absolute base URI of the current page.</param>
        public void Initialize(string uriAbsolute, string baseUriAbsolute)
        {
            SetAbsoluteBaseUri(baseUriAbsolute);
            SetAbsoluteUri(uriAbsolute);
            TriggerOnLocationChanged();

            _jsRuntime.InvokeAsync<object>(
                Interop.EnableNavigationInterception,
                typeof(RemoteUriHelper).Assembly.GetName().Name,
                nameof(NotifyLocationChanged));
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable(nameof(NotifyLocationChanged))]
        public static void NotifyLocationChanged(string uriAbsolute)
        {
            var circuit = Circuit.Current;
            if (circuit == null)
            {
                var message = $"{nameof(NotifyLocationChanged)} called without a circuit.";
                throw new InvalidOperationException(message);
            }

            var uriHelper = (RemoteUriHelper)circuit.Services.GetRequiredService<IUriHelper>();

            uriHelper.SetAbsoluteUri(uriAbsolute);
            uriHelper.TriggerOnLocationChanged();
        }

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="IUriHelper.GetBaseUri"/>).</param>
        protected override void NavigateToCore(string uri)
        {
            _jsRuntime.InvokeAsync<object>(Interop.NavigateTo, uri);
        }
    }
}
