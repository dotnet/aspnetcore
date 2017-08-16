// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Tls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    public static class ListenOptionsTlsExtensions
    {
        public static ListenOptions UseTls(this ListenOptions listenOptions, string certificatePath, string privateKeyPath)
        {
            return listenOptions.UseTls(new TlsConnectionAdapterOptions
            {
                CertificatePath = certificatePath,
                PrivateKeyPath = privateKeyPath
            });
        }

        public static ListenOptions UseTls(this ListenOptions listenOptions, TlsConnectionAdapterOptions tlsOptions)
        {
            var loggerFactory = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<ILoggerFactory>();
            listenOptions.ConnectionAdapters.Add(new TlsConnectionAdapter(tlsOptions, loggerFactory));
            return listenOptions;
        }
    }
}
