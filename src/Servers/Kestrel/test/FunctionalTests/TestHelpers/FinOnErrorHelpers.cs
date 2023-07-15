// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if SOCKETS
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
#else
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
#endif
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public static class FinOnErrorHelpers
    {
        public static void SetFinOnError(IServiceCollection services, bool finOnError) {
#if SOCKETS
            services.Configure<SocketTransportOptions>(o =>
            {
                o.FinOnError = finOnError;
            });
#else
            services.Configure<LibuvTransportOptions>(options => {
                options.FinOnError = finOnError;
            });
#endif
        }

        public static bool ExpectFinOnError(bool finOnError) {
#if SOCKETS
            return finOnError;
#else
            // libuv support for finOnError has only been implemented on Windows
            return finOnError || !PlatformApis.IsWindows;
#endif
        }
    }
}
