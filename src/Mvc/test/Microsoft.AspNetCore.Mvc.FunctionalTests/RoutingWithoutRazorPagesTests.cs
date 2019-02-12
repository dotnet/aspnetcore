// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RoutingWithoutRazorPagesTests : RoutingWithoutRazorPagesTestsBase<BasicWebSite.StartupWithoutEndpointRouting>
    {
        public RoutingWithoutRazorPagesTests(MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> fixture)
            : base(fixture)
        {
        }
    }
}