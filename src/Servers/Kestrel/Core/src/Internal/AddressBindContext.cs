// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class AddressBindContext
    {
        public AddressBindContext(
            ServerAddressesFeature serverAddressesFeature,
            KestrelServerOptions serverOptions,
            ILogger logger,
            Func<ListenOptions, CancellationToken, Task> createBinding)
        {
            ServerAddressesFeature = serverAddressesFeature;
            ServerOptions = serverOptions;
            Logger = logger;
            CreateBinding = createBinding;
        }

        public ServerAddressesFeature ServerAddressesFeature { get; }
        public ICollection<string> Addresses => ServerAddressesFeature.InternalCollection;

        public KestrelServerOptions ServerOptions { get; }
        public ILogger Logger { get; }

        public Func<ListenOptions, CancellationToken, Task> CreateBinding { get; }
    }
}
