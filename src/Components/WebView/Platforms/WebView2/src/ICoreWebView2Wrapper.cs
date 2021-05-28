// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Defines an abstraction for different WebView2 implementations on different platforms.
    /// </summary>
    public interface ICoreWebView2Wrapper
    {
        public ICoreWebView2SettingsWrapper Settings { get; }
        Task AddScriptToExecuteOnDocumentCreatedAsync(string javaScript);
        Action AddWebMessageReceivedHandler(Action<WebMessageReceivedEventArgs> messageReceivedHandler);
        void AddWebResourceRequestedFilter(string uri, CoreWebView2WebResourceContextWrapper resourceContext);
        Action AddWebResourceRequestedHandler(EventHandler<ICoreWebView2WebResourceRequestedEventArgsWrapper> webResourceRequestedHandler);
        void PostWebMessageAsString(string message);
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

    /// <summary>
    /// Provides an abstraction for different UI frameworks to provide access to APIs from
    /// Microsoft.Web.WebView2.Core.CoreWebView2 and related controls.
    /// </summary>
    public interface IWebView2Wrapper
    {
        Task CreateEnvironmentAsync();

        ICoreWebView2Wrapper CoreWebView2 { get; }

        /// <summary>
        /// Gets or sets the source URI of the control. Setting the source URI causes page navigation.
        /// </summary>
        Uri Source { get; set; }

        Task EnsureCoreWebView2Async();

        /// <summary>
        /// Event that occurs when an accelerator key is pressed.
        /// </summary>
        Action AddAcceleratorKeyPressedHandler(EventHandler<ICoreWebView2AcceleratorKeyPressedEventArgsWrapper> eventHandler);
    }

    public interface ICoreWebView2WebResourceRequestedEventArgsWrapper
    {
        /// <summary>
        /// Gets the web resource request.
        /// </summary>
        /// <remarks>
        /// The request object may be missing some headers that are added by network stack at a later time.
        /// </remarks>
        ICoreWebView2WebResourceRequestWrapper Request { get; }

        /// <summary>
        /// Gets the web resource request context.
        /// </summary>
        CoreWebView2WebResourceContextWrapper ResourceContext { get; }

        /// <summary>
        /// Set the response content for this web resource.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="statusCode"></param>
        /// <param name="statusMessage"></param>
        /// <param name="headerString"></param>
        void SetResponse(Stream content, int statusCode, string statusMessage, string headerString);
    }

    /// <summary>
    /// An HTTP request used with the <see cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceRequested" /> event.
    /// </summary>
    public interface ICoreWebView2WebResourceRequestWrapper
    {
        /// <summary>
        /// Gets or sets the request URI.
        /// </summary>
        string Uri { get; set; }

        /// <summary>
        /// Gets or sets the HTTP request method.
        /// </summary>
        string Method { get; set; }

        ///// <summary>
        ///// Gets or sets the HTTP request message body as stream.
        ///// </summary>
        ///// <remarks>
        ///// POST data should be here. If a stream is set, which overrides the message body, the stream must have all the content data available by the time the <see cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceRequested" /> event deferral of this request is completed. Stream should be agile or be created from a background STA to prevent performance impact to the UI thread. <c>null</c> means no content data.
        ///// </remarks>
        ///// <seealso cref="T:System.IO.Stream" />
        //Stream Content { get; set; }

        ///// <summary>
        ///// Gets the mutable HTTP request headers.
        ///// </summary>
        //ICoreWebView2HttpRequestHeadersWrapper Headers { get; }
    }

    /// <summary>
    /// Defines properties that enable, disable, or modify WebView features.
    /// </summary>
    /// <remarks>
    /// Setting changes made after <see cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.NavigationStarting" /> event do not apply until the next top-level navigation.
    /// </remarks>
    public interface ICoreWebView2SettingsWrapper
    {
        /// <summary>
        /// Determines whether running JavaScript is enabled in all future navigations in the WebView.
        /// </summary>
        /// <remarks>
        /// This only affects scripts in the document. Scripts injected with <see cref="M:Microsoft.Web.WebView2.Core.CoreWebView2.ExecuteScriptAsync(System.String)" /> runs even if script is disabled. The default value is <c>true</c>.
        /// </remarks>
        public bool IsScriptEnabled { get; set; }

        /// <summary>
        /// Determines whether communication from the host to the top-level HTML document of the WebView is allowed.
        /// </summary>
        /// <remarks>
        /// This is used when loading a new HTML document. If set to <c>true</c>, communication from the host to the top-level HTML document of the WebView is allowed using <see cref="M:Microsoft.Web.WebView2.Core.CoreWebView2.PostWebMessageAsJson(System.String)" />, <see cref="M:Microsoft.Web.WebView2.Core.CoreWebView2.PostWebMessageAsString(System.String)" />, and message event of <c>window.chrome.webview</c>. Communication from the top-level HTML document of the WebView to the host is allowed using <c>window.chrome.webview.postMessage</c> function and the <see cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.WebMessageReceived" /> event. If set to <c>false</c>, then communication is disallowed. <see cref="M:Microsoft.Web.WebView2.Core.CoreWebView2.PostWebMessageAsJson(System.String)" /> and <see cref="M:Microsoft.Web.WebView2.Core.CoreWebView2.PostWebMessageAsString(System.String)" /> fail and <c>window.chrome.webview.postMessage</c> fails by throwing an instance of an Error object. The default value is <c>true</c>.
        /// </remarks>
        /// <seealso cref="M:Microsoft.Web.WebView2.Core.CoreWebView2.PostWebMessageAsJson(System.String)" />
        /// <seealso cref="M:Microsoft.Web.WebView2.Core.CoreWebView2.PostWebMessageAsString(System.String)" />
        /// <seealso cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.WebMessageReceived" />
        public bool IsWebMessageEnabled { get; set; }

        /// <summary>
        /// Determines whether WebView renders the default Javascript dialog box.
        /// </summary>
        /// <remarks>
        /// This is used when loading a new HTML document. If set to <c>false</c>, WebView does not render the default JavaScript dialog box (specifically those displayed by the JavaScript alert, confirm, prompt functions and <c>beforeunload</c> event). Instead, WebView raises <see cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.ScriptDialogOpening" /> event that contains all of the information for the dialog and allow the host app to show a custom UI. The default value is <c>true</c>.
        /// </remarks>
        /// <seealso cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.ScriptDialogOpening" />
        public bool AreDefaultScriptDialogsEnabled { get; set; }

        /// <summary>
        /// Determines whether the status bar is displayed.
        /// </summary>
        /// <remarks>
        /// The status bar is usually displayed in the lower left of the WebView and shows things such as the URI of a link when the user hovers over it and other information. The default value is <c>true</c>.
        /// </remarks>
        public bool IsStatusBarEnabled { get; set; }

        /// <summary>
        /// Determines whether the user is able to use the context menu or keyboard shortcuts to open the DevTools window.
        /// </summary>
        /// <remarks>
        /// The default value is <c>true</c>.
        /// </remarks>
        public bool AreDevToolsEnabled { get; set; }

        /// <summary>
        /// Determines whether the default context menus are shown to the user in WebView.
        /// </summary>
        /// <remarks>
        /// The default value is <c>true</c>.
        /// </remarks>
        public bool AreDefaultContextMenusEnabled { get; set; }

        /// <summary>
        /// Determines whether host objects are accessible from the page in WebView.
        /// </summary>
        /// <remarks>
        /// The default value is <c>true</c>.
        /// </remarks>
        public bool AreHostObjectsAllowed { get; set; }

        /// <summary>
        /// Determines whether the user is able to impact the zoom of the WebView.
        /// </summary>
        /// <remarks>
        /// When disabled, the user is not able to zoom using Ctrl++, Ctr+-, or Ctrl+mouse wheel, but the zoom is set using <see cref="P:Microsoft.Web.WebView2.Core.CoreWebView2Controller.ZoomFactor" /> property. The default value is <c>true</c>.
        /// </remarks>
        public bool IsZoomControlEnabled { get; set; }

        /// <summary>
        /// Determines whether to disable built in error page for navigation failure and render process failure.
        /// </summary>
        /// <remarks>
        /// When disabled, blank page is displayed when related error happens. The default value is <c>true</c>.
        /// </remarks>
        public bool IsBuiltInErrorPageEnabled { get; set; }
    }

    public interface ICoreWebView2AcceleratorKeyPressedEventArgsWrapper
    {
        uint VirtualKey { get; }
        int KeyEventLParam { get; }
        bool Handled { get; set; }
    }
}
