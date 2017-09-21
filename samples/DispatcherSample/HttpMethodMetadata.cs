// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace DispatcherSample
{
    public class HttpMethodMetadata : IHttpMethodMetadata
    {
        public HttpMethodMetadata(string httpMethod)
        {
            if (httpMethod == null)
            {
                throw new ArgumentNullException(nameof(httpMethod));
            }

            AllowedMethods = new[] { httpMethod, };
        }

        public IReadOnlyList<string> AllowedMethods { get; }
    }
}
