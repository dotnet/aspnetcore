// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.WebView.WebView2
{
    /// <summary>
    /// Types in the Microsoft.AspNetCore.Components.WebView.WebView2 are not recommended for use outside
    /// of the Blazor framework. These types will change in a future release.
    ///
    /// Event arguments for the WebMessageReceived event.
    /// </summary>
    public class WebMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a new instance of <see cref="WebMessageReceivedEventArgs"/> with the provider source and message.
        /// </summary>
        /// <param name="source">The URI of the document that sent this web message.</param>
        /// <param name="webMessageAsString">the message posted from the WebView content to the host as a <see cref="T:System.String" />.</param>
        public WebMessageReceivedEventArgs(string source, string webMessageAsString)
        {
            Source = source;
            WebMessageAsString = webMessageAsString;
        }

        /// <summary>
        /// Gets the URI of the document that sent this web message.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Gets the message posted from the WebView content to the host as a <see cref="T:System.String" />.
        /// </summary>
        public string WebMessageAsString { get; }
    }
}
