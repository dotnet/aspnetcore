// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.`

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    public class ClientParametersTagHelperTests
    {
        [Fact]
        public void ProcessThrows_WhenClientIdNotFound()
        {
            // Arrange
            var clientRequestParametersProvider = new Mock<IClientRequestParametersProvider>();
            clientRequestParametersProvider.Setup(c => c.GetClientParameters(It.IsAny<HttpContext>(), It.IsAny<string>())).Returns<IDictionary<string, string>>(null);
            var tagHelperContext = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "id");
            var tagHelperOutput = new TagHelperOutput("meta", new TagHelperAttributeList(), (something, encoder) => Task.FromResult<TagHelperContent>(null));
            var tagHelper = new ClientParametersTagHelper(clientRequestParametersProvider.Object);
            tagHelper.ClientId = "id";
            tagHelper.ViewContext = new ViewContext() { HttpContext = new DefaultHttpContext() };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => tagHelper.Process(tagHelperContext, tagHelperOutput));
            Assert.Equal("Parameters for client 'id' not found.", exception.Message);
        }

        [Fact]
        public void ProcessAddsAttributesToTag_WhenClientIdFound()
        {
            // Arrange
            var clientRequestParametersProvider = new Mock<IClientRequestParametersProvider>();
            clientRequestParametersProvider.Setup(c => c.GetClientParameters(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .Returns(new Dictionary<string, string>()
                {
                    ["client_id"] = "SampleApp",
                    ["scope"] = "SampleAPI openid",
                    ["redirect_uri"] = "https://www.example.com/auth-callback",
                    ["response_type"] = "id_token code"
                });

            var tagHelperContext = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "id");
            var tagHelperOutput = new TagHelperOutput("meta", new TagHelperAttributeList(), (something, encoder) => Task.FromResult<TagHelperContent>(null));
            var tagHelper = new ClientParametersTagHelper(clientRequestParametersProvider.Object);
            tagHelper.ViewContext = new ViewContext() { HttpContext = new DefaultHttpContext() };

            // Act
            tagHelper.Process(tagHelperContext, tagHelperOutput);

            // Assert
            Assert.Contains(tagHelperOutput.Attributes, th => th.Name == "data-client_id" && th.Value is string value && value == "SampleApp");
            Assert.Contains(tagHelperOutput.Attributes, th => th.Name == "data-scope" && th.Value is string value && value == "SampleAPI openid");
            Assert.Contains(tagHelperOutput.Attributes, th => th.Name == "data-redirect_uri" && th.Value is string value && value == "https://www.example.com/auth-callback");
            Assert.Contains(tagHelperOutput.Attributes, th => th.Name == "data-response_type" && th.Value is string value && value == "id_token code");
        }
    }
}
