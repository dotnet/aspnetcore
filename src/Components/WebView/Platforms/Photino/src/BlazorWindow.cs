// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileProviders;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebView.Photino
{
    /// <summary>
    /// A window containing a Blazor web view.
    /// </summary>
    public class BlazorWindow
    {
        private readonly PhotinoWindow _window;
        private readonly PhotinoWebViewManager _manager;

        /// <summary>
        /// Constructs an instance of <see cref="BlazorWindow"/>.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="hostPage">The path to the host page.</param>
        /// <param name="services">The service provider.</param>
        /// <param name="configureWindow">A callback that configures the window.</param>
        public BlazorWindow(
            string title,
            string hostPage,
            IServiceProvider services,
            Action<PhotinoWindowOptions>? configureWindow = null)
        {
            _window = new PhotinoWindow(title, options =>
            {
                options.CustomSchemeHandlers.Add(PhotinoWebViewManager.BlazorAppScheme, HandleWebRequest);
                configureWindow?.Invoke(options);
            });

            // We assume the host page is always in the root of the content directory, because it's
            // unclear there's any other use case. We can add more options later if so.
            var contentRootDir = Path.GetDirectoryName(Path.GetFullPath(hostPage))!;
            var hostPageRelativePath = Path.GetRelativePath(contentRootDir, hostPage);
            var fileProvider = new PhysicalFileProvider(contentRootDir);

            var dispatcher = new PhotinoDispatcher(_window);
            _manager = new PhotinoWebViewManager(_window, services, dispatcher, new Uri(PhotinoWebViewManager.AppBaseUri), fileProvider, hostPageRelativePath);
        }

        /// <summary>
        /// Gets the underlying <see cref="PhotinoWindow"/>.
        /// </summary>
        public PhotinoWindow Photino => _window;

        /// <summary>
        /// Adds a root component to the window.
        /// </summary>
        /// <typeparam name="TComponent">The component type.</typeparam>
        /// <param name="selector">A CSS selector describing where the component should be added in the host page.</param>
        /// <param name="parameters">An optional dictionary of parameters to pass to the component.</param>
        public void AddRootComponent<TComponent>(string selector, IDictionary<string, object?>? parameters = null) where TComponent: IComponent
        {
            var parameterView = parameters == null
                ? ParameterView.Empty
                : ParameterView.FromDictionary(parameters);

            // Dispatch because this is going to be async, and we want to catch any errors
            _ = _manager.Dispatcher.InvokeAsync(async () =>
            {
                await _manager.AddRootComponentAsync(typeof(TComponent), selector, parameterView);
            }); 
        }

        /// <summary>
        /// Shows the window and waits for it to be closed.
        /// </summary>
        public void Run()
        {
            _manager.Navigate("/");
            _window.WaitForClose();
        }

        private Stream HandleWebRequest(string url, out string contentType)
            => _manager.HandleWebRequest(url, out contentType!)!;
    }
}
