// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ContentNegotiationWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc.Xml;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ContentNegotiationTest
    {
        private const string SiteName = nameof(ContentNegotiationWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ProducesAttribute_SingleContentType_PicksTheFirstSupportedFormatter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Selects custom even though it is last in the list.
            var expectedContentType = MediaTypeHeaderValue.Parse("application/custom;charset=utf-8");
            var expectedBody = "Written using custom format.";

            // Act
            var response = await client.GetAsync("http://localhost/Normal/WriteUserUsingCustomFormat");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesAttribute_MultipleContentTypes_RunsConnegToSelectFormatter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");
            var expectedBody = "{\r\n  \"Name\": \"My name\",\r\n  \"Address\": \"My address\"\r\n}";

            // Act
            var response = await client.GetAsync("http://localhost/Normal/MultipleAllowedContentTypes");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task NoProducesAttribute_ActionReturningString_RunsUsingTextFormatter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("text/plain;charset=utf-8");
            var expectedBody = "NormalController";

            // Act
            var response = await client.GetAsync("http://localhost/Normal/ReturnClassName");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task NoProducesAttribute_ActionReturningAnyObject_RunsUsingDefaultFormatters()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");

            // Act
            var response = await client.GetAsync("http://localhost/Normal/ReturnUser");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
        }

        [Fact]
        public async Task ProducesAttributeWithTypeOnly_RunsRegularContentNegotiation()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");
            var expectedOutput = "{\"Name\":\"John\",\"Address\":\"One Microsoft Way\"}";

            // Act
            var response = await client.GetAsync("http://localhost/Home/UserInfo_ProducesWithTypeOnly");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var actual = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedOutput, actual);
        }

        [Fact]
        public async Task ProducesAttribute_WithTypeAndContentType_UsesContentType()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            var expectedContentType = MediaTypeHeaderValue.Parse("application/xml;charset=utf-8");
            var expectedOutput = "<User xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                                "xmlns=\"http://schemas.datacontract.org/2004/07/ContentNegotiationWebSite\">" +
                                "<Address>One Microsoft Way</Address><Name>John</Name></User>";

            // Act
            var response = await client.GetAsync("http://localhost/Home/UserInfo_ProducesWithTypeAndContentType");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var actual = await response.Content.ReadAsStringAsync();
            XmlAssert.Equal(expectedOutput, actual);
        }

        [Theory]
        [InlineData("http://localhost/FallbackOnTypeBasedMatch/UseTheFallback_WithDefaultFormatters")]
        [InlineData("http://localhost/FallbackOnTypeBasedMatch/OverrideTheFallback_WithDefaultFormatters")]
        public async Task NoAcceptAndRequestContentTypeHeaders_UsesFirstFormatterWhichCanWriteType(string url)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");

            // Act
            var response = await client.GetAsync(url + "?input=100");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var actual = await response.Content.ReadAsStringAsync();
            Assert.Equal("100", actual);
        }

        [Fact]
        public async Task NoMatchingFormatter_ForTheGivenContentType_Returns406()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Normal/ReturnUser_NoMatchingFormatter");

            // Assert
            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        [Theory]
        [InlineData("ContactInfoUsingV3Format", "text/vcard; version=v3.0; charset=utf-8", "BEGIN:VCARD#FN:John Williams#END:VCARD#")]
        [InlineData("ContactInfoUsingV4Format", "text/vcard; version=v4.0; charset=utf-8", "BEGIN:VCARD#FN:John Williams#GENDER:M#END:VCARD#")]
        public async Task ProducesAttribute_WithMediaTypeHavingParameters_IsCaseInsensitiveMatch(
                                                                                            string action,
                                                                                            string expectedMediaType,
                                                                                            string expectedResponseBody)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            expectedResponseBody = expectedResponseBody.Replace("#", Environment.NewLine);

            // Act
            var response = await client.GetAsync("http://localhost/ProducesWithMediaTypeParameters/" + action);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            var contentType = response.Content.Headers.ContentType;
            Assert.NotNull(contentType);
            Assert.Equal(expectedMediaType, contentType.ToString());

            var actualResponseBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedResponseBody, actualResponseBody);
        }

        [Fact]
        public async Task ProducesAttribute_OnAction_OverridesTheValueOnClass()
        {
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Value on the class is application/json.
            var expectedContentType = MediaTypeHeaderValue.Parse(
                "application/custom_ProducesContentBaseController_Action;charset=utf-8");
            var expectedBody = "ProducesContentBaseController";

            // Act
            var response = await client.GetAsync("http://localhost/ProducesContentBase/ReturnClassName");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesAttribute_OnDerivedClass_OverridesTheValueOnBaseClass()
        {
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse(
                "application/custom_ProducesContentOnClassController;charset=utf-8");
            var expectedBody = "ProducesContentOnClassController";

            // Act
            var response = await client.GetAsync(
                            "http://localhost/ProducesContentOnClass/ReturnClassNameWithNoContentTypeOnAction");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesAttribute_OnDerivedAction_OverridesTheValueOnBaseClass()
        {
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse(
                "application/custom_NoProducesContentOnClassController_Action;charset=utf-8");
            var expectedBody = "NoProducesContentOnClassController";

            // Act
            var response = await client.GetAsync("http://localhost/NoProducesContentOnClass/ReturnClassName");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesAttribute_OnDerivedAction_OverridesTheValueOnBaseAction()
        {
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse(
                "application/custom_NoProducesContentOnClassController_Action;charset=utf-8");
            var expectedBody = "NoProducesContentOnClassController";

            // Act
            var response = await client.GetAsync("http://localhost/NoProducesContentOnClass/ReturnClassName");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesAttribute_OnDerivedClassAndAction_OverridesTheValueOnBaseClass()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse(
                "application/custom_ProducesContentOnClassController_Action;charset=utf-8");
            var expectedBody = "ProducesContentOnClassController";

            // Act
            var response = await client.GetAsync("http://localhost/ProducesContentOnClass/ReturnClassNameContentTypeOnDerivedAction");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesAttribute_IsNotHonored_ForJsonResult()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");
            var expectedBody = "{\"MethodName\":\"Produces_WithNonObjectResult\"}";

            // Act
            var response = await client.GetAsync("http://localhost/JsonResult/Produces_WithNonObjectResult");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task JsonResult_UsesDefaultContentTypes_IfNoneAreAddedExplicitly()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");
            var expectedBody = "{\"MethodName\":\"ReturnJsonResult\"}";

            // Act
            var response = await client.GetAsync("http://localhost/JsonResult/ReturnJsonResult");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task JsonResult_UsesExplicitContentTypeAndFormatter_IfAdded()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/custom-json;charset=utf-8");
            var expectedBody = "{ MethodName = ReturnJsonResult_WithCustomMediaType }";

            // Act
            var response = await client.GetAsync("http://localhost/JsonResult/ReturnJsonResult_WithCustomMediaType");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task JsonResult_UsesDefaultJsonFormatter_IfNoMatchingFormatterIsFound()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");
            var expectedBody = "{\"MethodName\":\"ReturnJsonResult_WithCustomMediaType_NoFormatter\"}";

            // Act
            var response = await client.GetAsync("http://localhost/JsonResult/ReturnJsonResult_WithCustomMediaType_NoFormatter");

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task JsonFormatter_SupportedMediaType_DoesNotChangeAcrossRequests()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");
            var expectedBody = "{\"MethodName\":\"ReturnJsonResult\"}";

            for (int i = 0; i < 5; i++)
            {
                // Act and Assert
                var response = await client.GetAsync("http://localhost/JsonResult/ReturnJsonResult");

                Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
                var body = await response.Content.ReadAsStringAsync();
                Assert.Equal(expectedBody, body);
            }
        }

        [Fact]
        public async Task XmlFormatter_SupportedMediaType_DoesNotChangeAcrossRequests()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            var expectedContentType = MediaTypeHeaderValue.Parse("application/xml;charset=utf-8");
            var expectedBody = @"<User xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" " +
                                @"xmlns=""http://schemas.datacontract.org/2004/07/ContentNegotiationWebSite""><Address>"
                                + @"One Microsoft Way</Address><Name>John</Name></User>";

            for (int i = 0; i < 5; i++)
            {
                // Act and Assert
                var response = await client.GetAsync("http://localhost/Home/UserInfo");

                Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
                var body = await response.Content.ReadAsStringAsync();
                Assert.Equal(expectedBody, body);
            }
        }

        [Theory]
        [InlineData("UseTheFallback_WithDefaultFormatters")]
        [InlineData("UseTheFallback_UsingCustomFormatters")]
        public async Task NoMatchOn_RequestContentType_FallsBackOnTypeBasedMatch_MatchFound(string actionName)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");
            var expectedBody = "1234";
            var targetUri = "http://localhost/FallbackOnTypeBasedMatch/" + actionName + "/?input=1234";
            var content = new StringContent("1234", Encoding.UTF8, "application/custom");
            var request = new HttpRequestMessage(HttpMethod.Post, targetUri);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/custom1"));
            request.Content = content;

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ObjectResult_WithStringReturnType_WritesTextPlainFormat(bool matchFormatterOnObjectType)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var targetUri = "http://localhost/FallbackOnTypeBasedMatch/ReturnString?matchFormatterOnObjectType=" +
                matchFormatterOnObjectType;
            var request = new HttpRequestMessage(HttpMethod.Get, targetUri);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
            var actualBody = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World!", actualBody);
        }

        [Theory]
        [InlineData("OverrideTheFallback_WithDefaultFormatters")]
        [InlineData("OverrideTheFallback_UsingCustomFormatters")]
        public async Task NoMatchOn_RequestContentType_SkipTypeMatchByAddingACustomFormatter(string actionName)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var targetUri = "http://localhost/FallbackOnTypeBasedMatch/" + actionName + "/?input=1234";
            var content = new StringContent("1234", Encoding.UTF8, "application/custom");
            var request = new HttpRequestMessage(HttpMethod.Post, targetUri);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/custom1"));
            request.Content = content;

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        [Fact]
        public async Task NoMatchOn_RequestContentType_FallsBackOnTypeBasedMatch_NoMatchFound_Returns406()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var targetUri = "http://localhost/FallbackOnTypeBasedMatch/FallbackGivesNoMatch/?input=1234";
            var content = new StringContent("1234", Encoding.UTF8, "application/custom");
            var request = new HttpRequestMessage(HttpMethod.Post, targetUri);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/custom1"));
            request.Content = content;

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
        }

        [Fact]
        public async Task ProducesAttribute_And_FormatFilterAttribute_Conflicting()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContentType = MediaTypeHeaderValue.Parse("application/json");

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/MethodWithFormatFilter.json");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ProducesAttribute_And_FormatFilterAttribute_Collaborating()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/MethodWithFormatFilter");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(body, "MethodWithFormatFilter");
        }
    }
}