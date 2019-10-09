// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public abstract class ConsumesAttributeTestsBase<TStartup> : IClassFixture<MvcTestFixture<TStartup>> where TStartup : class
    {
        protected ConsumesAttributeTestsBase(MvcTestFixture<TStartup> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
           builder.UseStartup<TStartup>();

        public HttpClient Client { get; }

        [Fact]
        public abstract Task HasEndpointMatch();

        [Fact]
        public async Task NoRequestContentType_SelectsActionWithoutConstraint()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_WithFallbackActionController/CreateProduct");

            // Act
            var response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("CreateProduct_Product_Text", body);
        }

        [Fact]
        public async Task NoRequestContentType_Selects_IfASingleActionWithConstraintIsPresent()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_PassThrough/CreateProduct");

            // Act
            var response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("ConsumesAttribute_PassThrough_Product_Json", body);
        }

        [Fact]
        public async Task NoRequestContentType_MultipleMatches_IfAMultipleActionWithConstraintIsPresent()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_PassThrough/CreateProductMultiple");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("text/json")]
        public async Task Selects_Action_BasedOnRequestContentType(string requestContentType)
        {
            // Arrange
            var input = "{SampleString:\""+requestContentType+"\"}";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_AmbiguousActions/CreateProduct");
            request.Content = new StringContent(input, Encoding.UTF8, requestContentType);

            // Act
            var response = await Client.SendAsync(request);
            var product = JsonConvert.DeserializeObject<Product>(await response.Content.ReadAsStringAsync());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(requestContentType, product.SampleString);
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("text/json")]
        public async Task ActionLevelAttribute_OverridesClassLevel(string requestContentType)
        {
            // Arrange
            var input = "{SampleString:\"" + requestContentType + "\"}";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_OverridesBase/CreateProduct");
            request.Content = new StringContent(input, Encoding.UTF8, requestContentType);
            var expectedString = "ConsumesAttribute_OverridesBaseController_" + requestContentType;

            // Act
            var response = await Client.SendAsync(request);
            var product = JsonConvert.DeserializeObject<Product>(await response.Content.ReadAsStringAsync());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedString, product.SampleString);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task DerivedClassLevelAttribute_OverridesBaseClassLevel()
        {
            // Arrange
            var input = "<Product xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/BasicWebSite.Models\">" +
                "<SampleString>application/xml</SampleString></Product>";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_Overrides/CreateProduct");
            request.Content = new StringContent(input, Encoding.UTF8, "application/xml");
            var expectedString = "ConsumesAttribute_OverridesController_application/xml";

            // Act
            var response = await Client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            var product = JsonConvert.DeserializeObject<Product>(responseString);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedString, product.SampleString);
        }

        [Fact]
        public async Task JsonSyntaxSuffix_SelectsActionConsumingJson()
        {
            // Arrange
            var input = "{SampleString:\"some input\"}";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_MediaTypeSuffix/CreateProduct");
            request.Content = new StringContent(input, Encoding.UTF8, "application/vnd.example+json");

            // Act
            var response = await Client.SendAsync(request);
            var product = JsonConvert.DeserializeObject<Product>(await response.Content.ReadAsStringAsync());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Read from JSON: some input", product.SampleString);
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task XmlSyntaxSuffix_SelectsActionConsumingXml()
        {
            // Arrange
            var input = "<Product xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/BasicWebSite.Models\">" +
                "<SampleString>some input</SampleString></Product>";
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/ConsumesAttribute_MediaTypeSuffix/CreateProduct");
            request.Content = new StringContent(input, Encoding.UTF8, "application/vnd.example+xml");

            // Act
            var response = await Client.SendAsync(request);
            var product = JsonConvert.DeserializeObject<Product>(await response.Content.ReadAsStringAsync());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Read from XML: some input", product.SampleString);
        }
    }
}