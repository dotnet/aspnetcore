// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

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
