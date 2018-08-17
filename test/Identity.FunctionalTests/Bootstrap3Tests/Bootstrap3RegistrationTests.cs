// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Identity.DefaultUI.WebSite;
using Identity.DefaultUI.WebSite.Data;

namespace Microsoft.AspNetCore.Identity.FunctionalTests.Bootstrap3Tests
{
    public class Bootstrap3RegistrationTests : RegistrationTests<Bootstrap3Startup, ApplicationDbContext>
    {
        public Bootstrap3RegistrationTests(ServerFactory<Bootstrap3Startup, ApplicationDbContext> serverFactory) : base(serverFactory)
        {
        }
    }
}
