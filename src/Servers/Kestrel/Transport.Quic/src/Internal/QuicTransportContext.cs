// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic.Internal
{
    internal class QuicTransportContext
    {
        public QuicTransportContext(IQuicTrace log, QuicTransportOptions options)
        {
            Log = log;
            Options = options;
        }

        public IQuicTrace Log { get; }
        public QuicTransportOptions Options { get; }
    }
}
