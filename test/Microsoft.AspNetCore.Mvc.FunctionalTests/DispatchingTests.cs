// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class DispatchingTests : RoutingTestsBase<RoutingWebSite.StartupWithDispatching>
    {
        public DispatchingTests(MvcTestFixture<RoutingWebSite.StartupWithDispatching> fixture)
            : base(fixture)
        {
        }
    }
}