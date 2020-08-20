// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RoutingWebSite;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RoutingDynamicOrderTest : IClassFixture<MvcTestFixture<RoutingWebSite.StartupForDynamic>>
    {
        public RoutingDynamicOrderTest(MvcTestFixture<RoutingWebSite.StartupForDynamic> fixture)
        {
            Factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) => builder.UseStartup<RoutingWebSite.StartupForDynamicOrder>();

        public WebApplicationFactory<StartupForDynamic> Factory { get; }

        [Fact]
        public async Task PrefersAttributeRoutesOverDynamicControllerRoutes()
        {
            var factory = Factory
                .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.AttributeRouteDynamicRoute));

            var client = factory.CreateClient();

            // Arrange
            var url = "http://localhost/attribute-dynamic-order/Controller=Home,Action=Index";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("AttributeRouteSlug", content.RouteName);
        }

        [Fact]
        public async Task DynamicRoutesAreMatchedInDefinitionOrderOverPrecedence()
        {
            AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
            var factory = Factory
                .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.MultipleDynamicRoute));

            var client = factory.CreateClient();

            // Arrange
            var url = "http://localhost/dynamic-order/specific/Controller=Home,Action=Index";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(content.RouteValues.TryGetValue("identifier", out var identifier));
            Assert.Equal("slug", identifier);
        }

        [Fact]
        public async Task ConventionalRoutesDefinedEarlierWinOverDynamicControllerRoutes()
        {
            AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
            var factory = Factory
                .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.ConventionalRouteDynamicRoute));

            var client = factory.CreateClient();

            // Arrange
            var url = "http://localhost/conventional-dynamic-order-before";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(content.RouteValues.TryGetValue("identifier", out var identifier));
        }

        [Fact]
        public async Task ConventionalRoutesDefinedLaterLooseToDynamicControllerRoutes()
        {
            AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
            var factory = Factory
                .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.ConventionalRouteDynamicRoute));

            var client = factory.CreateClient();

            // Arrange
            var url = "http://localhost/conventional-dynamic-order-after";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(content.RouteValues.TryGetValue("identifier", out var identifier));
            Assert.Equal("slug", identifier);
        }

        [Fact]
        public async Task DynamicPagesDefinedEarlierWinOverDynamicControllers()
        {
            AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
            var factory = Factory
                .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.DynamicControllerAndPages));

            var client = factory.CreateClient();
            // Arrange
            var url = "http://localhost/dynamic-order-page-controller-before";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from dynamic page: /DynamicPagebefore", content);
        }

        [Fact]
        public async Task DynamicPagesDefinedLaterLooseOverDynamicControllers()
        {
            AppContext.SetSwitch("Microsoft.AspNetCore.Routing.UseCorrectCatchAllBehavior", isEnabled: true);
            var factory = Factory
                .WithWebHostBuilder(b => b.UseSetting("Scenario", RoutingWebSite.StartupForDynamicOrder.DynamicOrderScenarios.DynamicControllerAndPages));

            var client = factory.CreateClient();

            // Arrange
            var url = "http://localhost/dynamic-order-page-controller-after";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Act
            var response = await client.SendAsync(request);
            var content = await response.Content.ReadFromJsonAsync<RouteInfo>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(content.RouteValues.TryGetValue("identifier", out var identifier));
            Assert.Equal("controller", identifier);
        }

        private record RouteInfo(string RouteName, IDictionary<string,string> RouteValues);
    }
}
