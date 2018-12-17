// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Services
{
    /// <summary>
    /// Helpers for working with URIs and navigation state.
    /// </summary>
    public interface IUriHelper
    {
        /// <summary>
        /// Gets the current absolute URI.
        /// </summary>
        /// <returns>The current absolute URI.</returns>
        string GetAbsoluteUri();

        /// <summary>
        /// An event that fires when the navigation location has changed.
        /// </summary>
        event EventHandler<string> OnLocationChanged;

        /// <summary>
        /// Converts a relative URI into an absolute one (by resolving it
        /// relative to the current absolute URI).
        /// </summary>
        /// <param name="href">The relative URI.</param>
        /// <returns>The absolute URI.</returns>
        Uri ToAbsoluteUri(string href);

        /// <summary>
        /// Gets the base URI (with trailing slash) that can be prepended before relative URI paths to produce an absolute URI.
        /// Typically this corresponds to the 'href' attribute on the document's &lt;base&gt; element.
        /// </summary>
        /// <returns>The URI prefix, which has a trailing slash.</returns>
        string GetBaseUri();

        /// <summary>
        /// Given a base URI (e.g., one previously returned by <see cref="GetBaseUri"/>),
        /// converts an absolute URI into one relative to the base URI prefix.
        /// </summary>
        /// <param name="baseUri">The base URI prefix (e.g., previously returned by <see cref="GetBaseUri"/>).</param>
        /// <param name="locationAbsolute">An absolute URI that is within the space of the base URI.</param>
        /// <returns>A relative URI path.</returns>
        string ToBaseRelativePath(string baseUri, string locationAbsolute);

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="GetBaseUri"/>).</param>
        void NavigateTo(string uri);

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// (as returned by <see cref="GetBaseUri"/>).</param>
        /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
        void NavigateTo(string uri, bool forceLoad);
    }
}
