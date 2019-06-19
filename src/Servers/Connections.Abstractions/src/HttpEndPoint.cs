// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace Microsoft.AspNetCore.Connections
{
    public class HttpEndPoint : EndPoint
    {
        public HttpEndPoint(Uri url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
        }

        public Uri Url { get;  }
    }
}
