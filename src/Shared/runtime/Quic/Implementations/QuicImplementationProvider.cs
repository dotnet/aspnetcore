// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
