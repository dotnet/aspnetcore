// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using ConnegWebsite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ConnegTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("ConnegWebsite");
        private readonly Action<IBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ProducesContentAttribute_SingleContentType_PicksTheFirstSupportedFormatter()
        {
            // Arrange            
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;

            // Selects custom even though it is last in the list.
            var expectedContentType = "application/custom;charset=utf-8";
            var expectedBody = "Written using custom format.";

            // Act
            var result = await client.GetAsync("http://localhost/Normal/WriteUserUsingCustomFormat");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesContentAttribute_MultipleContentTypes_RunsConnegToSelectFormatter()
        {
            // Arrange            
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "application/json;charset=utf-8";
            var expectedBody = "{\r\n  \"Name\": \"My name\",\r\n  \"Address\": \"My address\"\r\n}";

            // Act
            var result = await client.GetAsync("http://localhost/Normal/MultipleAllowedContentTypes");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task NoProducesContentAttribute_ActionReturningString_RunsUsingTextFormatter()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "text/plain;charset=utf-8";
            var expectedBody = "NormalController";

            // Act
            var result = await client.GetAsync("http://localhost/Normal/ReturnClassName");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task NoProducesContentAttribute_ActionReturningAnyObject_RunsUsingDefaultFormatters()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "application/json;charset=utf-8";
            //var expectedBody = "\"NormalController\"";

            // Act
            var result = await client.GetAsync("http://localhost/Normal/ReturnUser");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task NoMatchingFormatter_ForTheGivenContentType_Returns406()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;

            // Act
            var result = await client.GetAsync("http://localhost/Normal/ReturnUser_NoMatchingFormatter");

            // Assert
            Assert.Equal(406, result.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task ProducesContentAttribute_OnAction_OverridesTheValueOnClass()
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;

            // Value on the class is application/json.
            var expectedContentType = "application/custom_ProducesContentBaseController_Action;charset=utf-8";
            var expectedBody = "ProducesContentBaseController";

            // Act
            var result = await client.GetAsync("http://localhost/ProducesContentBase/ReturnClassName");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesContentAttribute_OnDerivedClass_OverridesTheValueOnBaseClass()
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "application/custom_ProducesContentOnClassController;charset=utf-8";
            var expectedBody = "ProducesContentOnClassController";

            // Act
            var result = await client.GetAsync(
                            "http://localhost/ProducesContentOnClass/ReturnClassNameWithNoContentTypeOnAction");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesContentAttribute_OnDerivedAction_OverridesTheValueOnBaseClass()
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "application/custom_NoProducesContentOnClassController_Action;charset=utf-8";
            var expectedBody = "NoProducesContentOnClassController";

            // Act
            var result = await client.GetAsync("http://localhost/NoProducesContentOnClass/ReturnClassName");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesContentAttribute_OnDerivedAction_OverridesTheValueOnBaseAction()
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "application/custom_NoProducesContentOnClassController_Action;charset=utf-8";
            var expectedBody = "NoProducesContentOnClassController";

            // Act
            var result = await client.GetAsync("http://localhost/NoProducesContentOnClass/ReturnClassName");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Fact]
        public async Task ProducesContentAttribute_OnDerivedClassAndAction_OverridesTheValueOnBaseClass()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "application/custom_ProducesContentOnClassController_Action;charset=utf-8";
            var expectedBody = "ProducesContentOnClassController";

            // Act
            var result = await client.GetAsync("http://localhost/ProducesContentOnClass/ReturnClassNameContentTypeOnDerivedAction");

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }


        [InlineData("ReturnTaskOfString")]
        [InlineData("ReturnTaskOfObject_StringValue")]
        [InlineData("ReturnString")]
        [InlineData("ReturnObject_StringValue")]
        [InlineData("ReturnString_NullValue")]
        public async Task TextPlainFormatter_ReturnsTextPlainContentType(string actionName)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "text/plain;charset=utf-8";
            var expectedBody = actionName;

            // Act
            var result = await client.GetAsync("http://localhost/TextPlain/" + actionName);

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [InlineData("ReturnTaskOfObject_ObjectValue")]
        [InlineData("ReturnObject_ObjectValue")]
        [InlineData("ReturnObject_NullValue")]
        public async Task TextPlainFormatter_DoesNotSelectTextPlainFormatterForNonStringValue(string actionName)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "application/json;charset=utf-8";
            var expectedBody = actionName;

            // Act
            var result = await client.GetAsync("http://localhost/TextPlain/" + actionName);

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
        }

        [InlineData("ReturnString_NullValue")]
        public async Task TextPlainFormatter_DoesNotWriteNullValue(string actionName)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "text/plain;charset=utf-8";
            string expectedBody = null;

            // Act
            var result = await client.GetAsync("http://localhost/TextPlain/" + actionName);

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }
    }
}