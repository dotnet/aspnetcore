// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Represents HTTP method metadata used during routing.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public sealed class HttpMethodMetadata : IHttpMethodMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMethodMetadata" /> class.
        /// </summary>
        /// <param name="httpMethods">
        /// The HTTP methods used during routing.
        /// An empty collection means any HTTP method will be accepted.
        /// </param>
        public HttpMethodMetadata(IEnumerable<string> httpMethods)
            : this(httpMethods, acceptCorsPreflight: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMethodMetadata" /> class.
        /// </summary>
        /// <param name="httpMethods">
        /// The HTTP methods used during routing.
        /// An empty collection means any HTTP method will be accepted.
        /// </param>
        /// <param name="acceptCorsPreflight">A value indicating whether routing accepts CORS preflight requests.</param>
        public HttpMethodMetadata(IEnumerable<string> httpMethods, bool acceptCorsPreflight)
        {
            if (httpMethods == null)
            {
                throw new ArgumentNullException(nameof(httpMethods));
            }

            HttpMethods = httpMethods.ToArray();
            AcceptCorsPreflight = acceptCorsPreflight;
        }

        /// <summary>
        /// Returns a value indicating whether the associated endpoint should accept CORS preflight requests.
        /// </summary>
        public bool AcceptCorsPreflight { get; }

        /// <summary>
        /// Returns a read-only collection of HTTP methods used during routing.
        /// An empty collection means any HTTP method will be accepted.
        /// </summary>
        public IReadOnlyList<string> HttpMethods { get; }

        private string DebuggerToString()
        {
            return $"HttpMethods: {string.Join(",", HttpMethods)} - Cors: {AcceptCorsPreflight}";
        }
    }
}
