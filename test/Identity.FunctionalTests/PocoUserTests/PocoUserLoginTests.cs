// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.IdentityUserTests
{
    public class PocoUserLoginTests : LoginTests<PocoUserStartup, IdentityDbContext>
    {
        public PocoUserLoginTests(ServerFactory<PocoUserStartup, IdentityDbContext> serverFactory) : base(serverFactory)
        {
        }
    }
}
