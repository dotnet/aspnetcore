// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class AnchorTagHelperTest
    {
        [Fact]
        public async Task ProcessAsync_GeneratesExpectedOutput()
        {
            // Arrange
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var anchorTagHelper = new AnchorTagHelper
            {
                Action = "index",
                Controller = "home",
                Fragment = "hello=world",
                Host = "contoso.com",
                Protocol = "http"
            };

            var tagHelperContext = new TagHelperContext(
                allAttributes: new Dictionary<string, object>
                {
                    { "id", "myanchor" },
                    { "route-foo", "bar" },
                    { "action", "index" },
                    { "controller", "home" },
                    { "fragment", "hello=world" },
                    { "host", "contoso.com" },
                    { "protocol", "http" }
                });
            var output = new TagHelperOutput(
                "a",
                attributes: new Dictionary<string, string>
                {
                    { "id", "myanchor" },
                    { "route-foo", "bar" },
                },
                content: "Something");

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns("home/index");

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider, urlHelper.Object);
            var viewContext = TestableHtmlGenerator.GetViewContext(model: null,
                                                                   htmlGenerator: htmlGenerator,
                                                                   metadataProvider: metadataProvider);
            anchorTagHelper.Generator = htmlGenerator;

            // Act
            await anchorTagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(2, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("id"));
            Assert.Equal("myanchor", attribute.Value);
            attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("href"));
            Assert.Equal("home/index", attribute.Value);
            Assert.Equal("Something", output.Content);
            Assert.Equal("a", output.TagName);
        }

        [Fact]
        public async Task ProcessAsync_CallsIntoRouteLinkWithExpectedParameters()
        {
            // Arrange
            var anchorTagHelper = new AnchorTagHelper
            {
                Route = "Default",
                Protocol = "http",
                Host = "contoso.com",
                Fragment = "hello=world"
            };
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>());
            var output = new TagHelperOutput(
                "a",
                attributes: new Dictionary<string, string>(),
                content: string.Empty);

            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateRouteLink(
                    string.Empty, "Default", "http", "contoso.com", "hello=world", null, null))
                .Returns(new TagBuilder("a"))
                .Verifiable();
            anchorTagHelper.Generator = generator.Object;

            // Act & Assert
            await anchorTagHelper.ProcessAsync(context, output);
            generator.Verify();
            Assert.Equal("a", output.TagName);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.Content);
        }

        [Fact]
        public async Task ProcessAsync_CallsIntoActionLinkWithExpectedParameters()
        {
            // Arrange
            var anchorTagHelper = new AnchorTagHelper
            {
                Action = "Index",
                Controller = "Home",
                Protocol = "http",
                Host = "contoso.com",
                Fragment = "hello=world"
            };
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>());
            var output = new TagHelperOutput(
                "a",
                attributes: new Dictionary<string, string>(),
                content: string.Empty);

            var generator = new Mock<IHtmlGenerator>();
            generator
                .Setup(mock => mock.GenerateActionLink(
                    string.Empty, "Index", "Home", "http", "contoso.com", "hello=world", null, null))
                .Returns(new TagBuilder("a"))
                .Verifiable();
            anchorTagHelper.Generator = generator.Object;

            // Act & Assert
            await anchorTagHelper.ProcessAsync(context, output);
            generator.Verify();
            Assert.Equal("a", output.TagName);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.Content);
        }

        [Theory]
        [InlineData("Action")]
        [InlineData("Controller")]
        [InlineData("Route")]
        [InlineData("Protocol")]
        [InlineData("Host")]
        [InlineData("Fragment")]
        [InlineData("route-")]
        public async Task ProcessAsync_ThrowsIfHrefConflictsWithBoundAttributes(string propertyName)
        {
            // Arrange
            var anchorTagHelper = new AnchorTagHelper();
            var output = new TagHelperOutput(
                "a",
                attributes: new Dictionary<string, string>()
                {
                    { "href", "http://www.contoso.com" }
                },
                content: string.Empty);

            if (propertyName == "route-")
            {
                output.Attributes.Add("route-foo", "bar");
            }
            else
            {
                typeof(AnchorTagHelper).GetProperty(propertyName).SetValue(anchorTagHelper, "Home");
            }

            var expectedErrorMessage = "Cannot determine an 'href' for <a>. An <a> with a specified 'href' must not " +
                                       "have attributes starting with 'route-' or an 'action', 'controller', " +
                                       "'route', 'protocol', 'host', or 'fragment' attribute.";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => anchorTagHelper.ProcessAsync(context: null, output: output));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Theory]
        [InlineData("Action")]
        [InlineData("Controller")]
        public async Task ProcessAsync_ThrowsIfRouteAndActionOrControllerProvided(string propertyName)
        {
            // Arrange
            var anchorTagHelper = new AnchorTagHelper
            {
                Route = "Default"
            };
            typeof(AnchorTagHelper).GetProperty(propertyName).SetValue(anchorTagHelper, "Home");
            var output = new TagHelperOutput(
                "a",
                attributes: new Dictionary<string, string>(),
                content: string.Empty);
            var expectedErrorMessage = "Cannot determine an 'href' for <a>. An <a> with a " +
                                       "specified 'route' must not have an 'action' or 'controller' attribute.";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => anchorTagHelper.ProcessAsync(context: null, output: output));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }
    }
}