// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost
{
    public static class HostBuilderTestServerExtensions
    {
        /// <summary>
        /// Retrieves the TestServer from the host services.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static TestServer GetTestServer(this IHost host)
        {
            return (TestServer)host.Services.GetRequiredService<IServer>();
        }

        /// <summary>
        /// Retrieves the test client from the TestServer in the host services.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static HttpClient GetTestClient(this IHost host)
        {
            return host.GetTestServer().CreateClient();
        }
    }
}
