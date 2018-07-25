// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Routing.Metadata
{
    public sealed class HttpMethodMetadata : IHttpMethodMetadata
    {
        public HttpMethodMetadata(IEnumerable<string> httpMethods)
        {
            if (httpMethods == null)
            {
                throw new ArgumentNullException(nameof(httpMethods));
            }

            HttpMethods = httpMethods.ToArray();
        }

        public IReadOnlyList<string> HttpMethods { get; }
    }
}
