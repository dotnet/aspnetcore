// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using BasicWebSite.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class BasicTests : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        // Some tests require comparing the actual response body against an expected response baseline
        // so they require a reference to the assembly on which the resources are located, in order to
        // make the tests less verbose, we get a reference to the assembly with the resources and we
        // use it on all the rest of the tests.
        private static readonly Assembly _resourcesAssembly = typeof(BasicTests).GetTypeInfo().Assembly;

        public BasicTests(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task CanRender_CSharp7Views()
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/BasicWebSite.Home.CSharp7View.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync("Home/CSharp7View");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task CanRender_ViewComponentWithArgumentsFromController()
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/BasicWebSite.PassThrough.Index.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync("PassThrough/Index?value=123");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Theory]
        [InlineData("")]
        [InlineData("Home")]
        [InlineData("Home/Index")]
        public async Task CanRender_ViewsWithLayout(string url)
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/BasicWebSite.Home.Index.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await Client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task CanRender_SimpleViews()
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/BasicWebSite.Home.PlainView.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync("http://localhost/Home/PlainView");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task ViewWithAttributePrefix_RendersWithoutIgnoringPrefix()
        {
            // Arrange
            var outputFile = "compiler/resources/BasicWebSite.Home.ViewWithPrefixedAttributeValue.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync("Home/ViewWithPrefixedAttributeValue");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task CanReturn_ResultsWithoutContent()
        {
            // Act
            var response = await Client.GetAsync("Home/NoContentResult");
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
            // Act
            var response = await Client.GetAsync("Home/ActionReturningTask");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello, World!", Assert.Single(response.Headers.GetValues("Message")));
            Assert.Empty(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ActionDescriptors_CreatedOncePerRequest()
        {
            // Arrange
            var expectedContent = "1";

            // Act and Assert
            for (var i = 0; i < 3; i++)
            {
                var result = await Client.GetAsync("Monitor/CountActionDescriptorInvocations");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var responseContent = await result.Content.ReadAsStringAsync();

                Assert.Equal(expectedContent, responseContent);
            }
        }

        [Fact]
        public async Task ActionWithRequireHttps_RedirectsToSecureUrl_ForNonHttpsGetRequests()
        {
            // Act
            var response = await Client.GetAsync("Home/HttpsOnlyAction");

            // Assert
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("https://localhost/Home/HttpsOnlyAction", response.Headers.Location.ToString());
            Assert.Equal(0, response.Content.Headers.ContentLength);

            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Empty(responseBytes);
        }

        [Fact]
        public async Task ActionWithRequireHttps_ReturnsBadRequestResponse_ForNonHttpsNonGetRequests()
        {
            // Act
            var response = await Client.SendAsync(new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/Home/HttpsOnlyAction"));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);

            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Empty(responseBytes);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        public async Task ActionWithRequireHttps_AllowsHttpsRequests(string method)
        {
            // Act
            var response = await Client.SendAsync(new HttpRequestMessage(
                new HttpMethod(method),
                "https://localhost/Home/HttpsOnlyAction"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task JsonHelper_RendersJson_WithCamelCaseNames()
        {
            // Arrange
            var json = "{\"id\":9000,\"fullName\":\"John <b>Smith</b>\"}";
            var expectedBody = string.Format(
                @"<script type=""text/javascript"">
    var json = {0};
</script>",
                json);

            // Act
            var response = await Client.GetAsync("Home/JsonHelperInView");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task JsonHelperWithSettings_RendersJson_WithNamesUnchanged()
        {
            // Arrange
            var json = "{\"id\":9000,\"FullName\":\"John <b>Smith</b>\"}";
            var expectedBody = string.Format(
                @"<script type=""text/javascript"">
    var json = {0};
</script>",
                json);

            // Act
            var response = await Client.GetAsync("Home/JsonHelperWithSettingsInView?snakeCase=false");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task JsonHelperWithSettings_RendersJson_WithSnakeCaseNames()
        {
            // Arrange
            var json = "{\"id\":9000,\"full_name\":\"John <b>Smith</b>\"}";
            var expectedBody = string.Format(
                @"<script type=""text/javascript"">
    var json = {0};
</script>",
                json);

            // Act
            var response = await Client.GetAsync("Home/JsonHelperWithSettingsInView?snakeCase=true");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);

            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, actualBody, ignoreLineEndingDifferences: true);
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
            // Act
            var response = await Client.GetAsync("Links/Index?view=" + viewName);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Contains(expectedLink, responseData, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ConfigureMvc_AddsOptionsProperly()
        {
            // Act
            var response = await Client.GetAsync("Home/GetApplicationDescription");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal("This is a basic website.", responseData);
        }

        [Fact]
        public async Task TypesMarkedAsNonAction_AreInaccessible()
        {
            // Act
            var response = await Client.GetAsync("SqlData/TruncateAllDbRecords");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UsingPageRouteParameterInConventionalRouteWorks()
        {
            // Arrange
            var expected = "ConventionalRoute - Hello from mypage";

            // Act
            var response = await Client.GetStringAsync("/PageRoute/ConventionalRoute/mypage");

            // Assert
            Assert.Equal(expected, response.Trim());
        }

        [Fact]
        public async Task UsingPageRouteParameterInAttributeRouteWorks()
        {
            // Arrange
            var expected = "AttributeRoute - Hello from test-page";

            // Act
            var response = await Client.GetStringAsync("/PageRoute/Attribute/test-page");

            // Assert
            Assert.Equal(expected, response.Trim());
        }

        [Fact]
        public async Task RedirectToAction_WithEmptyActionName_UsesAmbientValue()
        {
            // Arrange
            var product = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("SampleInt", "20")
            };

            // Act
            var response = await Client.PostAsync("/Home/Product", new FormUrlEncodedContent(product));

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Equal("/Home/Product", response.Headers.Location.ToString());

            var responseBody = await Client.GetStringAsync("/Home/Product");
            Assert.Equal("Get Product", responseBody);
        }
        [Fact]
        public async Task ActionMethod_ReturningActionMethodOfT_WithBadRequest()
        {
            // Arrange
            var url = "ActionResultOfT/GetProduct";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ActionMethod_ReturningActionMethodOfT()
        {
            // Arrange
            var url = "ActionResultOfT/GetProduct?productId=10";

            // Act
            var response = await Client.GetStringAsync(url);

            // Assert
            var result = JsonConvert.DeserializeObject<Product>(response);
            Assert.Equal(10, result.SampleInt);
        }

        [Fact]
        public async Task ActionMethod_ReturningSequenceOfObjectsWrappedInActionResultOfT()
        {
            // Arrange
            var url = "ActionResultOfT/GetProductsAsync";

            // Act
            var response = await Client.GetStringAsync(url);

            // Assert
            var result = JsonConvert.DeserializeObject<Product[]>(response);
            Assert.Equal(2, result.Length);
        }

        [Fact]
        public async Task TestingInfrastructure_InvokesCreateDefaultBuilder()
        {
            // Act
            var response = await Client.GetStringAsync("Testing/Builder");

            // Assert
            Assert.Equal("true", response);
        }
    }
}
