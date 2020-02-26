// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;

namespace System.Net.Quic.Implementations
{
    internal abstract class QuicImplementationProvider
    {
        internal QuicImplementationProvider() { }

        internal abstract QuicListenerProvider CreateListener(QuicListenerOptions options);

        internal abstract QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options);
    }
}
