// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ActionConstraintsWebSite;
using Microsoft.AspNet.Builder;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ConsumesAttributeTests
    {
        private const string SiteName = nameof(ActionConstraintsWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task NoRequestContentType_SelectsActionWithoutConstraint()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_Company/CreateProduct");

            // Act
            var response = await client.SendAsync(request);
            var product = JsonConvert.DeserializeObject<Product>(
                    await response.Content.ReadAsStringAsync());
            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(product);
        }

        [Fact]
        public async Task NoRequestContentType_Throws_IfMultipleActionsWithConstraints()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_AmbiguousActions/CreateProduct");

            // Act
            var response = await client.SendAsync(request);
            var exception = response.GetServerException();

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(typeof(AmbiguousActionException).FullName, exception.ExceptionType);
            Assert.Equal(
                "Multiple actions matched. The following actions matched route data and had all constraints "+
                "satisfied:____ActionConstraintsWebSite.ConsumesAttribute_NoFallBackActionController."+
                "CreateProduct__ActionConstraintsWebSite.ConsumesAttribute_NoFallBackActionController.CreateProduct",
                exception.ExceptionMessage);
        }

        [Fact]
        public async Task NoRequestContentType_Selects_IfASingleActionWithConstraintIsPresent()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_PassThrough/CreateProduct");

            // Act
            var response = await client.SendAsync(request);
            var product = JsonConvert.DeserializeObject<Product>(
                      await response.Content.ReadAsStringAsync());
            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(product);
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("text/json")]
        public async Task Selects_Action_BasedOnRequestContentType(string requestContentType)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var input = "{SampleString:\""+requestContentType+"\"}";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_AmbiguousActions/CreateProduct");
            request.Content = new StringContent(input, Encoding.UTF8, requestContentType);
            // Act
            var response = await client.SendAsync(request);
            var product = JsonConvert.DeserializeObject<Product>(
                      await response.Content.ReadAsStringAsync());
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(requestContentType, product.SampleString);
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("text/json")]
        public async Task ActionLevelAttribute_OveridesClassLevel(string requestContentType)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var input = "{SampleString:\"" + requestContentType + "\"}";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_OverridesBase/CreateProduct");
            request.Content = new StringContent(input, Encoding.UTF8, requestContentType);
            var expectedString = "ConsumesAttribute_OverridesBaseController_" + requestContentType;

            // Act
            var response = await client.SendAsync(request);
            var product = JsonConvert.DeserializeObject<Product>(
                      await response.Content.ReadAsStringAsync());
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedString, product.SampleString);
        }

        [Fact]
        public async Task DerivedClassLevelAttribute_OveridesBaseClassLevel()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var input = "<Product xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/ActionConstraintsWebSite\">" +
                "<SampleString>application/xml</SampleString></Product>";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_Overrides/CreateProduct");
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");
            var expectedString = "ConsumesAttribute_OverridesController_application/xml";

            // Act
            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            var product = JsonConvert.DeserializeObject<Product>(responseString);
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedString, product.SampleString);
        }
    }
}