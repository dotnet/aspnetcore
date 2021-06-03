// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.WebView.WebView2 are not recommended for use outside
    /// of the Blazor framework. These types will change in a future release.
    ///
    /// Event args for the WebResourceRequested event.
    /// </summary>
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
}
