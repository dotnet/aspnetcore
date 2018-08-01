// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class ConsumesAttributeEndpointRoutingTests : ConsumesAttributeTestsBase<BasicWebSite.StartupWithEndpointRouting>
    {
        public ConsumesAttributeEndpointRoutingTests(MvcTestFixture<BasicWebSite.StartupWithEndpointRouting> fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async override Task HasEndpointMatch()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Routing/HasEndpointMatch");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<bool>(body);

            Assert.True(result);
        }

        // The endpoint routing version of this feature has fixed https://github.com/aspnet/Mvc/issues/8174
        [Fact]
        public override async Task NoRequestContentType_Selects_IfASingleActionWithConstraintIsPresent()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_PassThrough/CreateProduct");

            // Act
            var response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        }
    }
}