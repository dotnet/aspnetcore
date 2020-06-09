// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    public class BasicTestAppServerSiteFixture<TStartup> : AspNetSiteServerFixture where TStartup : class
    {
        public BasicTestAppServerSiteFixture()
        {
            BuildWebHostMethod = TestServer.Program.BuildWebHost<TStartup>;
        }
    }
}
