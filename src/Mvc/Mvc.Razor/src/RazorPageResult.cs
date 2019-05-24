// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Result of locating a <see cref="IRazorPage"/>.
    /// </summary>
    public readonly struct RazorPageResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RazorPageResult"/> for a successful discovery.
        /// </summary>
        /// <param name="name">The name of the page that was found.</param>
        /// <param name="page">The located <see cref="IRazorPage"/>.</param>
        public RazorPageResult(string name, IRazorPage page)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            Name = name;
            Page = page;
            SearchedLocations = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RazorPageResult"/> for an unsuccessful discovery.
        /// </summary>
        /// <param name="name">The name of the page that was not found.</param>
        /// <param name="searchedLocations">The locations that were searched.</param>
        public RazorPageResult(string name, IEnumerable<string> searchedLocations)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (searchedLocations == null)
            {
                throw new ArgumentNullException(nameof(searchedLocations));
            }

            Name = name;
            Page = null;
            SearchedLocations = searchedLocations;
        }

        /// <summary>
        /// Gets the name or the path of the page being located.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the <see cref="IRazorPage"/> if found.
        /// </summary>
        /// <remarks>This property is <c>null</c> if the page was not found.</remarks>
        public IRazorPage Page { get; }

        /// <summary>
        /// Gets the locations that were searched when <see cref="Page"/> could not be found.
        /// </summary>
        /// <remarks>This property is <c>null</c> if the page was found.</remarks>
        public IEnumerable<string> SearchedLocations { get; }
    }
}