// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Provides an abstraction for different UI frameworks to provide access to APIs from
    /// Microsoft.Web.WebView2.Core.CoreWebView2 and related controls.
    /// </summary>
    public interface IWebView2Wrapper<TWebView2, TCoreWebView2Environment>
    {
        TWebView2 WebView2 { get; }

        Task<ICoreWebView2EnvironmentWrapper<TCoreWebView2Environment>> CreateEnvironmentAsync();


        ICoreWebView2Wrapper<TCoreWebView2Environment> CoreWebView2 { get; }

        TCoreWebView2Environment Environment { get; }

        /// <summary>
        /// Gets or sets the source URI of the control. Setting the source URI causes page navigation.
        /// </summary>
        Uri Source { get; set; }

        Task EnsureCoreWebView2Async(ICoreWebView2EnvironmentWrapper<TCoreWebView2Environment> environment);

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
        public ICoreWebView2WebResourceRequestWrapper Request { get; }

        /// <summary>
        /// Gets the web resource request context.
        /// </summary>
        public CoreWebView2WebResourceContextWrapper ResourceContext { get; }

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

        /// <summary>
        /// Gets or sets the HTTP request message body as stream.
        /// </summary>
        /// <remarks>
        /// POST data should be here. If a stream is set, which overrides the message body, the stream must have all the content data available by the time the <see cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceRequested" /> event deferral of this request is completed. Stream should be agile or be created from a background STA to prevent performance impact to the UI thread. <c>null</c> means no content data.
        /// </remarks>
        /// <seealso cref="T:System.IO.Stream" />
        Stream Content { get; set; }

        ///// <summary>
        ///// Gets the mutable HTTP request headers.
        ///// </summary>
        //ICoreWebView2HttpRequestHeadersWrapper Headers { get; }
    }


    /// <summary>
    /// An HTTP response used with the <see cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceRequested" /> event.
    /// </summary>
    public interface ICoreWebView2WebResourceResponseWrapper
    {
        /// <summary>
        /// Gets HTTP response content as stream.
        /// </summary>
        /// <remarks>
        /// Stream must have all the content data available by the time the <see cref="E:Microsoft.Web.WebView2.Core.CoreWebView2.WebResourceRequested" /> event deferral of this response is completed. Stream should be agile or be created from a background thread to prevent performance impact to the UI thread. <c>null</c> means no content data.
        /// </remarks>
        /// <seealso cref="T:System.IO.Stream" />
        Stream Content { get; set; }

        /// <summary>
        /// Gets the overridden HTTP response headers.
        /// </summary>
        ICoreWebView2HttpResponseHeadersWrapper Headers { get; }

        /// <summary>
        /// Gets or sets the HTTP response status code.
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the HTTP response reason phrase.
        /// </summary>
        string ReasonPhrase { get; set; }
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




    //public interface ICoreWebView2HttpRequestHeadersWrapper : IEnumerable<KeyValuePair<string, string>>, IEnumerable
    //{
    //    /// <inheritdoc />
    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return GetEnumerator();
    //    }

    //    /// <inheritdoc />
    //    IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
    //    {
    //        return GetEnumerator();
    //    }

    //    /// <summary>
    //    /// Returns an enumerator that iterates through the <see cref="T:Microsoft.Web.WebView2.Core.CoreWebView2HttpRequestHeaders" /> or <see cref="T:Microsoft.Web.WebView2.Core.CoreWebView2HttpResponseHeaders" /> collection.
    //    /// </summary>
    //    CoreWebView2HttpHeadersCollectionIterator GetEnumerator()
    //    {
    //        return GetIterator();
    //    }

    //    internal CoreWebView2HttpRequestHeaders(object rawCoreWebView2HttpRequestHeaders)
    //    {
    //        _rawNative = rawCoreWebView2HttpRequestHeaders;
    //    }

    //    /// <summary>
    //    /// Gets the header value matching the name.
    //    /// </summary>
    //    /// <returns>The header value matching the name.</returns>
    //    string GetHeader(string name)
    //    {
    //        return _nativeICoreWebView2HttpRequestHeaders.GetHeader(name);
    //    }

    //    /// <summary>
    //    /// Gets the header value matching the name using a <see cref="T:Microsoft.Web.WebView2.Core.CoreWebView2HttpHeadersCollectionIterator" />.
    //    /// </summary>
    //    /// <returns>The header value matching the name.</returns>
    //    CoreWebView2HttpHeadersCollectionIterator GetHeaders(string name)
    //    {
    //        return new CoreWebView2HttpHeadersCollectionIterator(_nativeICoreWebView2HttpRequestHeaders.GetHeaders(name));
    //    }

    //    /// <summary>
    //    /// Checks whether the headers contain an entry that matches the header name.
    //    /// </summary>
    //    /// <returns>Whether the headers contain an entry that matches the header name.</returns>
    //    bool Contains(string name)
    //    {
    //        return _nativeICoreWebView2HttpRequestHeaders.Contains(name) != 0;
    //    }

    //    /// <summary>
    //    /// Adds or updates header that matches the name.
    //    /// </summary>
    //    void SetHeader(string name, string value)
    //    {
    //        _nativeICoreWebView2HttpRequestHeaders.SetHeader(name, value);
    //    }

    //    /// <summary>
    //    /// Removes header that matches the name.
    //    /// </summary>
    //    void RemoveHeader(string name)
    //    {
    //        _nativeICoreWebView2HttpRequestHeaders.RemoveHeader(name);
    //    }

    //    /// <summary>
    //    /// Gets a <see cref="T:Microsoft.Web.WebView2.Core.CoreWebView2HttpHeadersCollectionIterator" /> over the collection of request headers.
    //    /// </summary>
    //    CoreWebView2HttpHeadersCollectionIterator GetIterator()
    //    {
    //        return new CoreWebView2HttpHeadersCollectionIterator(_nativeICoreWebView2HttpRequestHeaders.GetIterator());
    //    }
    //}

    public interface ICoreWebView2HttpResponseHeadersWrapper : IEnumerable<KeyValuePair<string, string>>, IEnumerable
    {
        /// <summary>
        /// Appends header line with name and value.
        /// </summary>
        /// <param name="name">The header name to be appended.</param>
        /// <param name="value">The header value to be appended.</param>
        void AppendHeader(string name, string value);

        /// <summary>
        /// Checks whether this CoreWebView2HttpResponseHeaders  contain entries matching the header name.
        /// </summary>
        /// <param name="name">The name of the header to seek.</param>
        bool Contains(string name);

        /// <summary>
        /// Gets the first header value in the collection matching the name.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <returns>The first header value in the collection matching the name.</returns>
        string GetHeader(string name);

        ///// <summary>
        ///// Gets the header values matching the name.
        ///// </summary>
        ///// <param name="name">The header name.</param>
        //CoreWebView2HttpHeadersCollectionIterator GetHeaders(string name);

    }

    public interface ICoreWebView2AcceleratorKeyPressedEventArgsWrapper
    {
        uint VirtualKey { get; }
        int KeyEventLParam { get; }
        bool Handled { get; set; }
    }

    public interface ICoreWebView2EnvironmentWrapper<TCoreWebView2Environment>
    {
        TCoreWebView2Environment CoreWebView2Environment { get; }
    }
}
