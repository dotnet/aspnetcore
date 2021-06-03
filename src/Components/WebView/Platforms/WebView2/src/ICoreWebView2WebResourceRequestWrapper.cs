// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.WebView.WebView2 are not recommended for use outside
    /// of the Blazor framework. These types will change in a future release.
    ///
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
}
