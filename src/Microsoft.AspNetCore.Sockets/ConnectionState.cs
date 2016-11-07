// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionState
    {
        public Connection Connection { get; set; }

        // These are used for long polling mostly
        public Action Close { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public bool Active { get; set; } = true;
    }
}
