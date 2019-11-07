// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicTransportContext
    {
        public MsQuicTransportContext(IHostApplicationLifetime appLifetime, IMsQuicTrace log, MsQuicTransportOptions options)
        {
            AppLifetime = appLifetime;
            Log = log;
            Options = options;
        }

        public IHostApplicationLifetime AppLifetime { get; }
        public IMsQuicTrace Log { get; }
        public MsQuicTransportOptions Options { get; }
    }
}
