// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
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
