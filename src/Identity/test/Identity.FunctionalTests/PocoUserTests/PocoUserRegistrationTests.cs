// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.IdentityUserTests;

public class PocoUserRegistrationTests : RegistrationTests<PocoUserStartup, IdentityDbContext>
{
    public PocoUserRegistrationTests(ServerFactory<PocoUserStartup, IdentityDbContext> serverFactory) : base(serverFactory)
    {
    }
}
