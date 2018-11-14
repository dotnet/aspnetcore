// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public class LibuvTransportContext
    {
        public LibuvTransportOptions Options { get; set; }

        public IApplicationLifetime AppLifetime { get; set; }

        public ILibuvTrace Log { get; set; }

        public IConnectionDispatcher ConnectionDispatcher { get; set; }
    }
}
