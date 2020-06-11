// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
