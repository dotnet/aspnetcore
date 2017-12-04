// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Sockets.Features
{
    public class ConnectionInherentKeepAliveFeature : IConnectionInherentKeepAliveFeature
    {
        public TimeSpan KeepAliveInterval { get; }

        public ConnectionInherentKeepAliveFeature(TimeSpan keepAliveInterval)
        {
            KeepAliveInterval = keepAliveInterval;
        }
    }
}
