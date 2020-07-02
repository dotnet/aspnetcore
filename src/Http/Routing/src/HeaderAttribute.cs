// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Attribute for providing request header metdata that is used during routing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class HeaderAttribute : Attribute, IHeaderMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
        /// Requests having the specified header <paramref name="headerName"/>, with any value, will match.
        /// </summary>
        public HeaderAttribute(string headerName)
            : this(headerName, Array.Empty<string>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
        /// Requests having the specified header <paramref name="headerName"/>
        /// with a value that matches <paramref name="headerValue"/> will match.
        /// Properties <see cref="HeaderValueMatchMode"/> and <see cref="HeaderValueStringComparison"/>
        /// provide additional string matching options.
        /// </summary>
        public HeaderAttribute(string headerName, string headerValue)
            : this(headerName, new[] { headerValue })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
        /// Requests having the specified header <paramref name="headerName"/>
        /// with a value that matches any of the values in <paramref name="headerValues"/>
        /// will match.
        /// Properties <see cref="HeaderValueMatchMode"/> and <see cref="HeaderValueStringComparison"/>
        /// provide additional string matching options.
        /// </summary>
        public HeaderAttribute(string headerName, string[] headerValues)
        {
            if (string.IsNullOrEmpty(headerName))
            {
                throw new ArgumentException(nameof(headerName));
            }

            if (headerValues == null)
            {
                throw new ArgumentNullException(nameof(headerValues));
            }

            this.HeaderName = headerName;
            this.HeaderValues = headerValues.ToArray();
        }

        /// <inheritdoc/>
        public string HeaderName { get; }

        /// <inheritdoc/>
        public IReadOnlyList<string> HeaderValues { get; }

        /// <inheritdoc/>
        public HeaderValueMatchMode HeaderValueMatchMode { get; set; } = HeaderValueMatchMode.Exact;

        /// <inheritdoc/>
        public StringComparison HeaderValueStringComparison { get; set; } = StringComparison.Ordinal;
    }
}
