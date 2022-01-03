// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Identity.DefaultUI.WebSite;
using Identity.DefaultUI.WebSite.Data;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.IdentityUserTests;

public class ApplicationUserAuthorizationTests : AuthorizationTests<ApplicationUserStartup, ApplicationDbContext>
{
    public ApplicationUserAuthorizationTests(ServerFactory<ApplicationUserStartup, ApplicationDbContext> serverFactory) : base(serverFactory)
    {
    }
}
