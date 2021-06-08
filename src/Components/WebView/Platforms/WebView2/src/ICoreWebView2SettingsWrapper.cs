// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.WebView.WebView2 are not recommended for use outside
    /// of the Blazor framework. These types will change in a future release.
    ///
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
}
