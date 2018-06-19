// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RoutingTests : RoutingTestsBase<RoutingWebSite.Startup>
    {
        public RoutingTests(MvcTestFixture<RoutingWebSite.Startup> fixture)
            : base(fixture)
        {
        }
    }
}
