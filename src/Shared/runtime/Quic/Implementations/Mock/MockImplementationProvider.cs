// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Security;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockImplementationProvider : QuicImplementationProvider
    {
        internal override QuicListenerProvider CreateListener(QuicListenerOptions options)
        {
            return new MockListener(options.ListenEndPoint, options.ServerAuthenticationOptions);
        }

        internal override QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options)
        {
            return new MockConnection(options.RemoteEndPoint, options.ClientAuthenticationOptions, options.LocalEndPoint);
        }
    }
}
