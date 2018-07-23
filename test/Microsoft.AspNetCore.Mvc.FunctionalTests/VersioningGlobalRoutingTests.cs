// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class VersioningGlobalRoutingTests : VersioningTestsBase<VersioningWebSite.StartupWithGlobalRouting>
    {
        public VersioningGlobalRoutingTests(MvcTestFixture<VersioningWebSite.StartupWithGlobalRouting> fixture)
            : base(fixture)
        {
        }
    }
}