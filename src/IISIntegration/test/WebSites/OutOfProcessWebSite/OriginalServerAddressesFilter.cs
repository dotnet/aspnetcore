// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace TestSite
{
    internal class OriginalServerAddressesFilter: IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder => {
                ServerAddresses = builder.ServerFeatures.Get<IServerAddressesFeature>();
                next(builder);
            };
        }

        public IServerAddressesFeature ServerAddresses { get; set; }
    }
}
