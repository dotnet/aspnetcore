// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Identity.DefaultUI.WebSite;
using Identity.DefaultUI.WebSite.Data;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Bootstrap4Tests
{
    public class Bootstrap4AuthorizationTests : AuthorizationTests<ApplicationUserStartup, ApplicationDbContext>
    {
        public Bootstrap4AuthorizationTests(ServerFactory<ApplicationUserStartup, ApplicationDbContext> serverFactory) : base(serverFactory)
        {
            serverFactory.BootstrapFrameworkVersion = "V4";
        }
    }
}
