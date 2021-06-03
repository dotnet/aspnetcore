// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.WebView.WebView2 are not recommended for use outside
    /// of the Blazor framework. These types will change in a future release.
    ///
    /// Defines an abstraction for different WebView2 implementations on different platforms.
    /// </summary>
    public interface ICoreWebView2Wrapper
    {
        /// <summary>
        /// Gets the <see cref="ICoreWebView2SettingsWrapper" /> object contains various modifiable settings for the running WebView.
        /// </summary>
        public ICoreWebView2SettingsWrapper Settings { get; }

        /// <summary>
        /// Adds the provided JavaScript to a list of scripts that should be run after the global object has been created, but before the HTML document has been parsed and before any other script included by the HTML document is run.
        /// </summary>
        /// <param name="javaScript">The JavaScript code to be run.</param>
        Task AddScriptToExecuteOnDocumentCreatedAsync(string javaScript);


        /// <summary>
        /// WebMessageReceived is raised when the IsWebMessageEnabled setting is set and the top-level document of the WebView runs <c>window.chrome.webview.postMessage</c>.
        /// </summary>
        Action AddWebMessageReceivedHandler(Action<WebMessageReceivedEventArgs> messageReceivedHandler);

        /// <summary>
        /// Adds a URI and resource context filter to the WebResourceRequested event.
        /// </summary>
        /// <param name="uri">An URI to be added to the WebResourceRequested event.</param>
        /// <param name="resourceContext">A resource context filter to be added to the WebResourceRequested event.</param>
        void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContextWrapper resourceContext);

        /// <summary>
        /// WebResourceRequested is raised when the WebView is performing a URL request to a matching URL and resource context filter that was added with AddWebResourceRequestedFilter />.
        /// </summary>
        Action AddWebResourceRequestedHandler(EventHandler<ICoreWebView2WebResourceRequestedEventArgsWrapper> webResourceRequestedHandler);

        /// <summary>
        /// Posts a message that is a simple <see cref="T:System.String" /> rather than a JSON string representation of a JavaScript object.
        /// </summary>
        /// <param name="webMessageAsString">The web message to be posted to the top level document in this WebView.</param>
        void PostWebMessageAsString(string webMessageAsString);
    }
}
