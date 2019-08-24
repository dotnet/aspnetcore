// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class GlobalAuthorizationFilterUseMvcTest : GlobalAuthorizationFilterTestBase, IClassFixture<MvcTestFixture<SecurityWebSite.StartupWithGlobalDenyAnonymousFilterWithUseMvc>>
    {
        public GlobalAuthorizationFilterUseMvcTest(MvcTestFixture<SecurityWebSite.StartupWithGlobalDenyAnonymousFilterWithUseMvc> fixture)
        {
            Factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = Factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<SecurityWebSite.StartupWithGlobalDenyAnonymousFilterWithUseMvc>();

        public WebApplicationFactory<SecurityWebSite.StartupWithGlobalDenyAnonymousFilterWithUseMvc> Factory { get; }
    }
}
