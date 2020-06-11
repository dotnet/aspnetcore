// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    /// <summary>
    /// These tests are for scenarios when <see cref="MvcOptions.RespectBrowserAcceptHeader"/> is <c>False</c>, which is the default.
    /// </summary>
    public class DoNotRespectBrowserAcceptHeaderTests : IClassFixture<MvcTestFixture<FormatterWebSite.Startup>>
    {
        public DoNotRespectBrowserAcceptHeaderTests(MvcTestFixture<FormatterWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("application/xml,*/*;q=0.2")]
        [InlineData("application/xml,*/*")]
        public async Task AllMediaRangeAcceptHeader_FirstFormatterInListWritesResponse(string acceptHeader)
        {
            // Arrange
            var request = RequestWithAccept("http://localhost/DoNotRespectBrowserAcceptHeader/EmployeeInfo", acceptHeader);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal("{\"id\":10,\"name\":\"John\"}", responseData);
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData("application/xml,*/*;q=0.2")]
        [InlineData("application/xml,*/*")]
        public async Task AllMediaRangeAcceptHeader_ProducesAttributeIsHonored(string acceptHeader)
        {
            // Arrange
            var request = RequestWithAccept(
                "http://localhost/DoNotRespectBrowserAcceptHeader/EmployeeInfoWithProduces",
                acceptHeader);
            var expectedResponseData =
                "<DoNotRespectBrowserAcceptHeaderController.Employee xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"" +
                " xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite.Controllers\"><Id>20</Id><Name>Mike" +
                "</Name></DoNotRespectBrowserAcceptHeaderController.Employee>";

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/xml; charset=utf-8", response.Content.Headers.ContentType.ToString());
            var responseData = await response.Content.ReadAsStringAsync();
            XmlAssert.Equal(expectedResponseData, responseData);
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData("application/xml,*/*;q=0.2")]
        [InlineData("application/xml,*/*")]
        public async Task AllMediaRangeAcceptHeader_WithContentTypeHeader_ContentTypeIsIgnored(string acceptHeader)
        {
            // Arrange
            var requestData =
                "<DoNotRespectBrowserAcceptHeaderController.Employee xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"" +
                " xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite.Controllers\"><Id>35</Id><Name>Jimmy" +
                "</Name></DoNotRespectBrowserAcceptHeaderController.Employee>";
            var expectedResponseData = @"{""id"":35,""name"":""Jimmy""}";
            var request = RequestWithAccept("http://localhost/DoNotRespectBrowserAcceptHeader/CreateEmployee", acceptHeader);
            request.Content = new StringContent(requestData, Encoding.UTF8, "application/xml");
            request.Method = HttpMethod.Post;

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);

            // Site uses default output formatter (ignores Accept header) because that header contained a wildcard match.
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());

            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedResponseData, responseData);
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData("application/xml,application/json;q=0.2")]
        [InlineData("application/xml,application/json")]
        public async Task AllMediaRangeAcceptHeader_WithExactMatch_ReturnsExpectedContent(string acceptHeader)
        {
            // Arrange
            var requestData =
                "<DoNotRespectBrowserAcceptHeaderController.Employee xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"" +
                " xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite.Controllers\"><Id>35</Id><Name>Jimmy" +
                "</Name></DoNotRespectBrowserAcceptHeaderController.Employee>";
            var request = RequestWithAccept("http://localhost/DoNotRespectBrowserAcceptHeader/CreateEmployee", acceptHeader);
            request.Content = new StringContent(requestData, Encoding.UTF8, "application/xml");
            request.Method = HttpMethod.Post;

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/xml; charset=utf-8", response.Content.Headers.ContentType.ToString());
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal(requestData, responseData);
        }

        private static HttpRequestMessage RequestWithAccept(string url, string accept)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", accept);

            return request;
        }
    }
}