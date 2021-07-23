// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RoutingEndpointRoutingWithoutRazorPagesTests : RoutingWithoutRazorPagesTestsBase<BasicWebSite.Startup>
    {
        public RoutingEndpointRoutingWithoutRazorPagesTests(MvcTestFixture<BasicWebSite.Startup> fixture)
            : base(fixture)
        {
        }
    }
}