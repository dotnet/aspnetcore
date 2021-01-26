// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Localization
{
    /// <summary>
    /// An <see cref="IHtmlContent"/> with localized content.
    /// </summary>
    public class LocalizedHtmlString : IHtmlContent
    {
        private readonly object[] _arguments;

        /// <summary>
        /// Creates an instance of <see cref="LocalizedHtmlString"/>.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="value">The string resource.</param>
        public LocalizedHtmlString(string name, string value)
            : this(name, value, isResourceNotFound: false, arguments: Array.Empty<object>())
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="LocalizedHtmlString"/>.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="value">The string resource.</param>
        /// <param name="isResourceNotFound">A flag that indicates if the resource is not found.</param>
        public LocalizedHtmlString(string name, string value, bool isResourceNotFound)
            : this(name, value, isResourceNotFound, arguments: Array.Empty<object>())
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="LocalizedHtmlString"/>.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="value">The string resource.</param>
        /// <param name="isResourceNotFound">A flag that indicates if the resource is not found.</param>
        /// <param name="arguments">The values to format the <paramref name="value"/> with.</param>
        public LocalizedHtmlString(string name, string value, bool isResourceNotFound, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            Name = name;
            Value = value;
            IsResourceNotFound = isResourceNotFound;
            _arguments = arguments;
        }

        /// <summary>
        /// The name of the string resource.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The original resource string, prior to formatting with any constructor arguments.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets a flag that indicates if the resource is not found.
        /// </summary>
        public bool IsResourceNotFound { get; }

        /// <inheritdoc />
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            var formattableString = new HtmlFormattableString(Value, _arguments);
            formattableString.WriteTo(writer, encoder);
        }
    }
}
