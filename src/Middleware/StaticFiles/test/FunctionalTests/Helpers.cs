// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Microsoft.AspNetCore.StaticFiles
{
    public static class Helpers
    {
        public static string GetAddress(IWebHost server)
        {
            return server.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First();
        }
    }
}
