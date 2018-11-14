// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class AddressBindContext
    {
        public ICollection<string> Addresses { get; set; }
        public List<ListenOptions> ListenOptions { get; set; }
        public KestrelServerOptions ServerOptions { get; set; }
        public ILogger Logger { get; set; }

        public Func<ListenOptions, Task> CreateBinding { get; set; }
    }
}
