// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public static class TransportSelector
    {
        public static IWebHostBuilder GetWebHostBuilder()
        {
            return new WebHostBuilder().UseSockets().ConfigureServices(TestServer.RemoveDevCert);
        }
    }
}
