// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal class Http2HttpMessageHandler : DelegatingHandler
{
    public Http2HttpMessageHandler(HttpMessageHandler inner) : base(inner) { }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_1_OR_GREATER || NET7_0_OR_GREATER
        // Check just in case HttpRequestMessage defaults to 3 or higher for some reason
        if (request.Version == HttpVersion.Version11)
        {
            // HttpClient gracefully falls back to HTTP/1.1,
            // so it's fine to set the preferred version to a higher version
            request.Version = HttpVersion.Version20;
        }
#endif

        return base.SendAsync(request, cancellationToken);
    }
}
