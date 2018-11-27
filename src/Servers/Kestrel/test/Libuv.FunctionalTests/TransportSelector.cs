// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public static class TransportSelector
    {
        public static IWebHostBuilder GetWebHostBuilder()
        {
            return new WebHostBuilder().UseLibuv().ConfigureServices(RemoveDevCert);
        }

        private static void RemoveDevCert(IServiceCollection services)
        {
            // KestrelServerOptionsSetup would scan all system certificates on every test server creation
            // making test runs very slow
            foreach (var descriptor in services.ToArray())
            {
                if (descriptor.ImplementationType == typeof(KestrelServerOptionsSetup))
                {
                    services.Remove(descriptor);
                }
            }
        }
    }
}
