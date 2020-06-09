// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockImplementationProvider : QuicImplementationProvider
    {
        internal override QuicListenerProvider CreateListener(QuicListenerOptions options)
        {
            return new MockListener(options.ListenEndPoint!, options.ServerAuthenticationOptions);
        }

        internal override QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options)
        {
            return new MockConnection(options.RemoteEndPoint, options.ClientAuthenticationOptions, options.LocalEndPoint);
        }
    }
}
