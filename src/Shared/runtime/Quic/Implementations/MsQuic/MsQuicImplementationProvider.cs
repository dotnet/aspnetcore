// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicImplementationProvider : QuicImplementationProvider
    {
        internal override QuicListenerProvider CreateListener(QuicListenerOptions options)
        {
            return new MsQuicListener(options);
        }

        internal override QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options)
        {
            return new MsQuicConnection(options);
        }
    }
}
