// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    internal class QuicTransportContext
    {
        public QuicTransportContext(IHostApplicationLifetime appLifetime, IQuicTrace log, QuicTransportOptions options)
        {
            AppLifetime = appLifetime;
            Log = log;
            Options = options;
        }

        public IHostApplicationLifetime AppLifetime { get; }
        public IQuicTrace Log { get; }
        public QuicTransportOptions Options { get; }
    }
}
