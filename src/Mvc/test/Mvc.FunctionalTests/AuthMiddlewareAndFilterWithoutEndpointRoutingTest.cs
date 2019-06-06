// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class AuthMiddlewareAndFilterWithoutEndpointRoutingTest : AuthMiddlewareAndFilterTestBase<SecurityWebSite.StartupWithGlobalAuthFilterWithoutEndpointRouting>
    {
        public AuthMiddlewareAndFilterWithoutEndpointRoutingTest(MvcTestFixture<SecurityWebSite.StartupWithGlobalAuthFilterWithoutEndpointRouting> fixture)
            : base(fixture)
        {
        }
    }
}
