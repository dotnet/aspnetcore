// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Localization
{
    /// <summary>
    /// An <see cref="HtmlString"/> with localized content.
    /// </summary>
    public class LocalizedHtmlString : HtmlString
    {
        /// <summary>
        /// Creates an instance of <see cref="LocalizedHtmlString"/>.
        /// </summary>
        /// <param name="key">The name of the string resource.</param>
        /// <param name="value">The string resource.</param>
        public LocalizedHtmlString(string key, string value)
            : this(key, value, isResourceNotFound: false)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="LocalizedHtmlString"/>.
        /// </summary>
        /// <param name="key">The name of the string resource.</param>
        /// <param name="value">The string resource.</param>
        /// <param name="isResourceNotFound">A flag that indicates if the resource is not found.</param>
        public LocalizedHtmlString(string key, string value, bool isResourceNotFound)
            : base(value)
        {
            Key = key;
            IsResourceNotFound = isResourceNotFound;
        }

        /// <summary>
        /// The name of the string resource.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The string resource.
        /// </summary>
        public string Value => ToString();

        /// <summary>
        /// Gets a flag that indicates if the resource is not found.
        /// </summary>
        public bool IsResourceNotFound { get; }
    }
}