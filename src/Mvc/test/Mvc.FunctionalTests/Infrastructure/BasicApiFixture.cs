// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicApi;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class BasicApiFixture : MvcTestFixture<Startup>
    {
        // Do not leave .db file behind. Also, ensure added pet gets expected id (1) in subsequent runs.
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
