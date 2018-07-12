// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class DispatchingTests : RoutingTestsBase<RoutingWebSite.StartupWithDispatching>
    {
        public DispatchingTests(MvcTestFixture<RoutingWebSite.StartupWithDispatching> fixture)
            : base(fixture)
        {
        }

        [Fact(Skip = "Link generation issue in dispatching. Need to fix - https://github.com/aspnet/Routing/issues/590")]
        public override Task AttributeRoutedAction_InArea_ExplicitLeaveArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Link generation issue in dispatching. Need to fix - https://github.com/aspnet/Routing/issues/590")]
        public override Task AttributeRoutedAction_InArea_StaysInArea_ActionDoesntExist()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Link generation issue in dispatching. Need to fix - https://github.com/aspnet/Routing/issues/590")]
        public override Task ConventionalRoutedAction_InArea_ExplicitLeaveArea()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Link generation issue in dispatching. Need to fix - https://github.com/aspnet/Routing/issues/590")]
        public override Task ConventionalRoutedAction_InArea_StaysInArea()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async override Task RouteData_Routers_ConventionalRoute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/RouteData/Conventional");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Equal(
                Array.Empty<string>(),
                result.Routers);
        }

        [Fact]
        public async override Task RouteData_Routers_AttributeRoute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/RouteData/Attribute");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Equal(
                Array.Empty<string>(),
                result.Routers);
        }
    }
}