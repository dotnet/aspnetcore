// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Services
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
        /// Gets the URI prefix that can be prepended before URI paths to produce an absolute URI.
        /// Typically this corresponds to the 'href' attribute on the document's &lt;base&gt; element.
        /// </summary>
        /// <returns>The URI prefix.</returns>
        string GetBaseUriPrefix();

        /// <summary>
        /// Given a base URI prefix (e.g., one previously returned by <see cref="GetBaseUriPrefix"/>),
        /// converts an absolute URI into one relative to the base URI prefix.
        /// </summary>
        /// <param name="baseUriPrefix">The base URI prefix (e.g., previously returned by <see cref="GetBaseUriPrefix"/>).</param>
        /// <param name="locationAbsolute">An absolute URI that is within the space of the base URI prefix.</param>
        /// <returns>A relative URI path.</returns>
        string ToBaseRelativePath(string baseUriPrefix, string locationAbsolute);

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI. This can be absolute, or relative to the base URI
        /// prefix (as returned by <see cref="GetBaseUriPrefix"/>).</param>
        void NavigateTo(string uri);
    }
}
