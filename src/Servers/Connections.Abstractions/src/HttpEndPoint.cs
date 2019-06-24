// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// An <see cref="EndPoint"/> defined by a <see cref="Uri"/>.
    /// </summary>
    public class HttpEndPoint : EndPoint
    {
        public HttpEndPoint(Uri url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
        }

        /// <summary>
        /// The <see cref="Uri"/> defining the <see cref="EndPoint"/>.
        /// </summary>
        public Uri Url { get;  }
    }
}
