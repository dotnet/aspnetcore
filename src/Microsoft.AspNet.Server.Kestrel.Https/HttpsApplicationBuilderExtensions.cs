// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Filter;

namespace Microsoft.AspNet.Server.Kestrel.Https
{
    public static class HttpsApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseKestrelHttps(this IApplicationBuilder app, X509Certificate2 cert)
        {
            var serverInfo = app.ServerFeatures.Get<IKestrelServerInformation>();

            if (serverInfo == null)
            {
                return app;
            }

            var prevFilter = serverInfo.ConnectionFilter ?? new NoOpConnectionFilter();

            serverInfo.ConnectionFilter = new HttpsConnectionFilter(cert, prevFilter);

            return app;
        }
    }
}
