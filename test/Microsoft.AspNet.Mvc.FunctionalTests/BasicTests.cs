// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using BasicWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class BasicTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("BasicWebSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        // Some tests require comparing the actual response body against an expected response baseline
        // so they require a reference to the assembly on which the resources are located, in order to
        // make the tests less verbose, we get a reference to the assembly with the resources and we
        // use it on all the rest of the tests.
        private readonly Assembly _resourcesAssembly = typeof(BasicTests).GetTypeInfo().Assembly;

        [Theory]
        [InlineData("http://localhost/")]
        [InlineData("http://localhost/Home")]
        [InlineData("http://localhost/Home/Index")]
        public async Task CanRender_ViewsWithLayout(string url)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");

            // The K runtime compiles every file under compiler/resources as a resource at runtime with the same name
            // as the file name, in order to update a baseline you just need to change the file in that folder.
            var expectedContent = await _resourcesAssembly.ReadResourceAsStringAsync("compiler/resources/BasicWebSite.Home.Index.html");

            // Act

            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
            Assert.Equal(expectedContent, responseContent);
        }

        [Fact]
        public async Task CanRender_SimpleViews()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedContent = await _resourcesAssembly.ReadResourceAsStringAsync("compiler/resources/BasicWebSite.Home.PlainView.html");
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");


            // Act
            var response = await client.GetAsync("http://localhost/Home/PlainView");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
            Assert.Equal(expectedContent, responseContent);
        }

        [Fact]
        public async Task CanReturn_ResultsWithoutContent()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/NoContentResult");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(response.Content.Headers.ContentType);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            Assert.Equal(0, responseContent.Length);
        }

        [Fact]
        public async Task ReturningTaskFromAction_ProducesNoContentResult()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ActionReturningTask");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello world", body);
        }

        [Fact]
        public async Task ActionDescriptors_CreatedOncePerRequest()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expectedContent = "1";

            // Act and Assert
            for (var i = 0; i < 3; i++)
            {
                var result = await client.GetAsync("http://localhost/Monitor/CountActionDescriptorInvocations");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var responseContent = await result.Content.ReadAsStringAsync();

                Assert.Equal(expectedContent, responseContent);
            }
        }

        [Fact]
        public async Task ActionWithRequireHttps_RedirectsToSecureUrl_ForNonHttpsGetRequests()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/HttpsOnlyAction");

            // Assert
            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("https://localhost/Home/HttpsOnlyAction", response.Headers.Location.ToString());
            Assert.Equal(0, response.Content.Headers.ContentLength);

            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(0, responseBytes.Length);
        }

        [Fact]
        public async Task ActionWithRequireHttps_ReturnsBadRequestResponse_ForNonHttpsNonGetRequests()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/Home/HttpsOnlyAction"));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);

            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(0, responseBytes.Length);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        public async Task ActionWithRequireHttps_AllowsHttpsRequests(string method)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = new HttpClient(server.CreateHandler(), false);

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(
                new HttpMethod(method),
                "https://localhost/Home/HttpsOnlyAction"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task JsonViewComponent_RendersJson()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = new HttpClient(server.CreateHandler(), false);
            var expectedBody = JsonConvert.SerializeObject(new BasicWebSite.Models.Person()
            {
                Id = 10,
                Name = "John"
            });

            // Act
            var response = await client.GetAsync("https://localhost/Home/JsonTextInView");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody);
        }

        public static IEnumerable<object[]> HtmlHelperLinkGenerationData
        {
            get
            {
                yield return new[] {
                    "ActionLink_ActionOnSameController",
                    @"<a href=""/Links/Details"">linktext</a>" };
                yield return new[] {
                    "ActionLink_ActionOnOtherController",
                    @"<a href=""/Products/Details?print=true"">linktext</a>"
                };
                yield return new[] {
                    "ActionLink_SecurePage_ImplicitHostName",
                    @"<a href=""https://localhost/Products/Details?print=true"">linktext</a>"
                };
                yield return new[] {
                    "ActionLink_HostNameFragmentAttributes",
                    // note: attributes are alphabetically ordered
                    @"<a href=""https://www.contoso.com:9000/Products/Details?print=true#details"" p1=""p1-value"">linktext</a>"
                };
                yield return new[] {
                    "RouteLink_RestLinkToOtherController",
                    @"<a href=""/api/orders/10"">linktext</a>"
                };
                yield return new[] {
                    "RouteLink_SecureApi_ImplicitHostName",
                    @"<a href=""https://localhost/api/orders/10"">linktext</a>"
                };
                yield return new[] {
                    "RouteLink_HostNameFragmentAttributes",
                    @"<a href=""https://www.contoso.com:9000/api/orders/10?print=True#details"" p1=""p1-value"">linktext</a>"
                };
            }
        }

        [Theory]
        [MemberData(nameof(HtmlHelperLinkGenerationData))]
        public async Task HtmlHelperLinkGeneration(string viewName, string expectedLink)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = new HttpClient(server.CreateHandler(), false);

            // Act
            var response = await client.GetAsync("http://localhost/Links/Index?view=" + viewName);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Contains(expectedLink, responseData, StringComparison.OrdinalIgnoreCase);
        }
    }
}