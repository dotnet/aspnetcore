// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    public interface ICoreWebView2Wrapper<TCoreWebView2Environment>
    {
        public ICoreWebView2SettingsWrapper Settings { get; }
        void PostWebMessageAsString(string message);
        void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContextWrapper ResourceContext);
        Action AddWebResourceRequestedHandler(EventHandler<ICoreWebView2WebResourceRequestedEventArgsWrapper> eventHandler);
        Task AddScriptToExecuteOnDocumentCreatedAsync(string javaScript);
        void AddWebMessageReceivedHandler(Action<WebMessageReceivedEventArgs> messageReceived);
    }

    public class WebMessageReceivedEventArgs : EventArgs
    {
        public WebMessageReceivedEventArgs(string source, string webMessageAsString)
        {
            Source = source;
            WebMessageAsString = webMessageAsString;
        }

        public string Source { get; }
        public string WebMessageAsString { get; }
    }

    public enum CoreWebView2WebResourceContextWrapper
    {
        /// <summary>
        /// Specifies all resources.
        /// </summary>
        All,
        /// <summary>
        /// Specifies a document resources.
        /// </summary>
        Document,
        /// <summary>
        /// Specifies a CSS resources.
        /// </summary>
        Stylesheet,
        /// <summary>
        /// Specifies an image resources.
        /// </summary>
        Image,
        /// <summary>
        /// Specifies another media resource such as a video.
        /// </summary>
        Media,
        /// <summary>
        /// Specifies a font resource.
        /// </summary>
        Font,
        /// <summary>
        /// Specifies a script resource.
        /// </summary>
        Script,
        /// <summary>
        /// Specifies an XML HTTP request.
        /// </summary>
        XmlHttpRequest,
        /// <summary>
        /// Specifies a Fetch API communication.
        /// </summary>
        Fetch,
        /// <summary>
        /// Specifies a TextTrack resource.
        /// </summary>
        TextTrack,
        /// <summary>
        /// Specifies an EventSource API communication.
        /// </summary>
        EventSource,
        /// <summary>
        /// Specifies a WebSocket API communication.
        /// </summary>
        Websocket,
        /// <summary>
        /// Specifies a Web App Manifest.
        /// </summary>
        Manifest,
        /// <summary>
        /// Specifies a Signed HTTP Exchange.
        /// </summary>
        SignedExchange,
        /// <summary>
        /// Specifies a Ping request.
        /// </summary>
        Ping,
        /// <summary>
        /// Specifies a CSP Violation Report.
        /// </summary>
        CspViolationReport,
        /// <summary>
        /// Specifies an other resource.
        /// </summary>
        Other
    }
}
