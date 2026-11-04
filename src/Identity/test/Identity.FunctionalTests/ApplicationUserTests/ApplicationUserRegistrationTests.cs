// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Identity.DefaultUI.WebSite;
using Identity.DefaultUI.WebSite.Data;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.IdentityUserTests;

public class ApplicationUserRegistrationTests : RegistrationTests<ApplicationUserStartup, ApplicationDbContext>
{
    public ApplicationUserRegistrationTests(ServerFactory<ApplicationUserStartup, ApplicationDbContext> serverFactory) : base(serverFactory)
    {
    }
}
