// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicViews;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class BasicViewsFixture : MvcTestFixture<Startup>
    {
        // Do not leave .db file behind.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Startup.DropDatabase(Server.Host.Services);
            }

            base.Dispose(disposing);
        }
    }
}
