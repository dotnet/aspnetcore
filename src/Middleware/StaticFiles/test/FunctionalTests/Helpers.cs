// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.StaticFiles
{
    public static class Helpers
    {
        public static string GetAddress(IHost server)
        {
            return server.Services.GetService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();
        }
    }
}
