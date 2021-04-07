// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Sockets;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed partial class SocketConnection : IConnectionSocketFeature
    {
        public Socket Socket => _socket;

        private void InitiaizeFeatures()
        {
            _currentIConnectionSocketFeature = this;
        }
    }
}
