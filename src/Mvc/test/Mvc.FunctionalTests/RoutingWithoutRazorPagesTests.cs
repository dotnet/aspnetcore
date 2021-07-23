// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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