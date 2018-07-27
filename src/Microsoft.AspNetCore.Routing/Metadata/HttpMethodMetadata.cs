// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Metadata
{
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    public sealed class HttpMethodMetadata : IHttpMethodMetadata
    {
        public HttpMethodMetadata(IEnumerable<string> httpMethods)
            : this(httpMethods, acceptCorsPreflight: false)
        {
        }

        public HttpMethodMetadata(IEnumerable<string> httpMethods, bool acceptCorsPreflight)
        {
            if (httpMethods == null)
            {
                throw new ArgumentNullException(nameof(httpMethods));
            }

            HttpMethods = httpMethods.ToArray();
            AcceptCorsPreflight = acceptCorsPreflight;
        }

        public bool AcceptCorsPreflight { get; }

        public IReadOnlyList<string> HttpMethods { get; }

        private string DebuggerToString()
        {
            return $"HttpMethods: {string.Join(",", HttpMethods)} - Cors: {AcceptCorsPreflight}";
        }
    }
}
