// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class GlobalAuthorizationFilterUseMvcTest : GlobalAuthorizationFilterTestBase<SecurityWebSite.StartupWithGlobalDenyAnonymousFilterWithUseMvc>
{
    public override void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<SecurityWebSite.StartupWithGlobalDenyAnonymousFilterWithUseMvc>();
}
