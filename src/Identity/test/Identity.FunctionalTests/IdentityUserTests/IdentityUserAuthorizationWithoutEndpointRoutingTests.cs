// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.IdentityUserTests
{
    public class IdentityUserAuthorizationWithoutEndpointRoutingTests : AuthorizationTests<StartupWithoutEndpointRouting, IdentityDbContext>
    {
        public IdentityUserAuthorizationWithoutEndpointRoutingTests(ServerFactory<StartupWithoutEndpointRouting, IdentityDbContext> serverFactory)
            : base(serverFactory)
        {
        }
    }
}
