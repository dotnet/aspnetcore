// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Attribute for providing request header metdata that is used during routing.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class HeaderAttribute : Attribute, IHeaderMetadata
    {
        private const int DefaultMaximumValuesToInspect = 1;

        private int _maximumValuesToInspect = DefaultMaximumValuesToInspect;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
        /// Requests having the specified header <paramref name="headerName"/>
        /// with a value that matches any of the values in <paramref name="headerValues"/>
        /// will match.
        /// Properties <see cref="ValueMatchMode"/> and <see cref="ValueIgnoresCase"/>
        /// provide additional string matching options for header values.
        /// </summary>
        /// <param name="headerName">Header name to match.</param>
        /// <param name="headerValues">
        /// Acceptable header values that should match.
        /// An empty collection means any value will be accepted as long as the header is present.
        /// </param>
        public HeaderAttribute(string headerName, params string[] headerValues)
        {
            if (string.IsNullOrEmpty(headerName))
            {
                throw new ArgumentException(nameof(headerName));
            }

            _ = headerValues ?? throw new ArgumentNullException(nameof(headerValues));

            HeaderName = headerName;
            HeaderValues = headerValues.ToArray();
        }

        /// <inheritdoc/>
        public string HeaderName { get; }

        /// <inheritdoc/>
        public IReadOnlyList<string> HeaderValues { get; }

        /// <inheritdoc/>
        public HeaderValueMatchMode ValueMatchMode { get; set; } = HeaderValueMatchMode.Exact;

        /// <inheritdoc/>
        public bool ValueIgnoresCase { get; set; }

        /// <inheritdoc/>
        public int MaximumValuesToInspect
        {
            get => _maximumValuesToInspect;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"${nameof(value)} must be positive.");
                }

                _maximumValuesToInspect = value;
            }
        }

        private string DebuggerToString()
        {
            var valuesDisplay = (HeaderValues.Count == 0)
                ? "*"
                : string.Join(",", HeaderValues.Select(v => $"\"{v}\""));

            return $"Header {HeaderName} = {valuesDisplay} ({nameof(ValueMatchMode)}={ValueMatchMode}, {nameof(ValueIgnoresCase)}={ValueIgnoresCase}, {nameof(MaximumValuesToInspect)}={MaximumValuesToInspect})";
        }
    }
}
