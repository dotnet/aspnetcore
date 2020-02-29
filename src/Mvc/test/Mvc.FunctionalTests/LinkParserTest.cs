// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    // Functional tests for MVC's scenarios with LinkParser
    public class LinkParserTest : IClassFixture<MvcTestFixture<RoutingWebSite.StartupForLinkGenerator>>
    {
        public LinkParserTest(MvcTestFixture<RoutingWebSite.StartupForLinkGenerator> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<RoutingWebSite.StartupForLinkGenerator>();

        public HttpClient Client { get; }
        
        [Fact]
        public async Task ParsePathByEndpoint_CanParsedWithDefaultRoute()
        {
            // Act
            var response = await Client.GetAsync("LinkParser/Index/18");
            var values = await response.Content.ReadAsAsync<JObject>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Collection(
                values.Properties().OrderBy(p => p.Name),
                p => 
                {
                    Assert.Equal("action", p.Name);
                    Assert.Equal("Index", p.Value.Value<string>());
                },
                p => 
                {
                    Assert.Equal("controller", p.Name);
                    Assert.Equal("LinkParser", p.Value.Value<string>());
                },
                p => 
                {
                    Assert.Equal("id", p.Name);
                    Assert.Equal("18", p.Value.Value<string>());
                });
        }

        [Fact]
        public async Task ParsePathByEndpoint_CanParsedWithNamedAttributeRoute()
        {
            // Act
            //
            // %2F => /
            var response = await Client.GetAsync("LinkParser/Another?path=%2Fsome-path%2Fa%2Fb%2Fc");
            var values = await response.Content.ReadAsAsync<JObject>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Collection(
                values.Properties().OrderBy(p => p.Name),
                p =>
                {
                    Assert.Equal("action", p.Name);
                    Assert.Equal("AnotherRoute", p.Value.Value<string>());
                },
                p =>
                {
                    Assert.Equal("controller", p.Name);
                    Assert.Equal("LinkParser", p.Value.Value<string>());
                },
                p =>
                {
                    Assert.Equal("x", p.Name);
                    Assert.Equal("a", p.Value.Value<string>());
                },
                p =>
                {
                    Assert.Equal("y", p.Name);
                    Assert.Equal("b", p.Value.Value<string>());
                },
                p =>
                {
                    Assert.Equal("z", p.Name);
                    Assert.Equal("c", p.Value.Value<string>());
                });
        }
    }
}
