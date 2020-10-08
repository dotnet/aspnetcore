// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class Http3StreamContext : HttpConnectionContext
    {
        public ConnectionContext StreamContext { get; set; }
        public Http3PeerSettings ServerSettings { get; set; }
    }
}
