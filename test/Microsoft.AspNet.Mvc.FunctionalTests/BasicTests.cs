// Copyright (c) .NET Foundation. All rights reserved.
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
using Microsoft.Framework.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class BasicTests
    {
        private const string SiteName = nameof(BasicWebSite);

        // Some tests require comparing the actual response body against an expected response baseline
        // so they require a reference to the assembly on which the resources are located, in order to
        // make the tests less verbose, we get a reference to the assembly with the resources and we
        // use it on all the rest of the tests.
        private static readonly Assembly _resourcesAssembly = typeof(BasicTests).GetTypeInfo().Assembly;

        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Theory]
        [InlineData("http://localhost/")]
        [InlineData("http://localhost/Home")]
        [InlineData("http://localhost/Home/Index")]
        public async Task CanRender_ViewsWithLayout(string url)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/BasicWebSite.Home.Index.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent);
#endif
        }

        [Fact]
        public async Task CanRender_SimpleViews()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/BasicWebSite.Home.PlainView.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await client.GetAsync("http://localhost/Home/PlainView");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent);
#endif
        }

        [Fact]
        public async Task ViewWithAttributePrefix_RendersWithoutIgnoringPrefix()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/BasicWebSite.Home.ViewWithPrefixedAttributeValue.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await client.GetAsync("http://localhost/Home/ViewWithPrefixedAttributeValue");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent);
#endif
        }

        [Fact]
        public async Task CanReturn_ResultsWithoutContent()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
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
        public async Task ReturningTaskFromAction_ProducesEmptyResult()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/ActionReturningTask");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello, World!", Assert.Single(response.Headers.GetValues("Message")));
            Assert.Empty(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ActionDescriptors_CreatedOncePerRequest()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
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

        [Fact]
        public async Task JsonHelper_RendersJson()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = new HttpClient(server.CreateHandler(), false);

            var json = JsonConvert.SerializeObject(new BasicWebSite.Models.Person()
            {
                Id = 9000,
                Name = "John <b>Smith</b>"
            });

            var expectedBody = string.Format(@"<script type=""text/javascript"">" + Environment.NewLine +
                                             @"    var json = {0};" + Environment.NewLine +
                                             @"</script>", json);

            // Act
            var response = await client.GetAsync("https://localhost/Home/JsonHelperInView");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody);
        }

        [Fact]
        public async Task JsonHelperWithSettings_RendersJson()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = new HttpClient(server.CreateHandler(), false);

            var json = JsonConvert.SerializeObject(new BasicWebSite.Models.Person()
            {
                Id = 9000,
                Name = "John <b>Smith</b>"
            }, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

            var expectedBody = string.Format(@"<script type=""text/javascript"">" + Environment.NewLine +
                                             @"    var json = {0};" + Environment.NewLine +
                                             @"</script>", json);

            // Act
            var response = await client.GetAsync("https://localhost/Home/JsonHelperWithSettingsInView");

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
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = new HttpClient(server.CreateHandler(), false);

            // Act
            var response = await client.GetAsync("http://localhost/Links/Index?view=" + viewName);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Contains(expectedLink, responseData, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ConfigureMvc_AddsOptionsProperly()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = new HttpClient(server.CreateHandler(), false);

            // Act
            var response = await client.GetAsync("http://localhost/Home/GetApplicationDescription");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a basic website.", responseData);
        }

        [Fact]
        public async Task TypesWithoutControllerSuffix_DerivingFromTypesWithControllerSuffix_CanBeAccessed()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = new HttpClient(server.CreateHandler(), false);

            // Act
            var response = await client.GetStringAsync("http://localhost/appointments");

            // Assert
            Assert.Equal("2 appointments available.", response);
        }

        [Fact]
        public async Task TypesMarkedAsNonAction_AreInaccessible()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = new HttpClient(server.CreateHandler(), false);

            // Act
            var response = await client.GetAsync("http://localhost/SqlData/TruncateAllDbRecords");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}