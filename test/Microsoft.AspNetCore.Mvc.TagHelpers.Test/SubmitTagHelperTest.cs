// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class SubmitTagHelperTest
    {
        [Fact]
        public async Task ProcessAsync_GeneratesExpectedOutput()
        {
            // Arrange
            var expectedTagName = "not-input";
            var metadataProvider = new TestModelMetadataProvider();

            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList
                {
                    { "id", "myinput" },
                    { "type", "submit" },
                    { "asp-route-name", "value" },
                    { "asp-action", "index" },
                    { "asp-controller", "home" },
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                expectedTagName,
                attributes: new TagHelperAttributeList
                {
                    { "id", "myinput" },
                    { "type", "submit" },
                },
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something Else");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            output.Content.SetContent("Something");

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(It.IsAny<UrlActionContext>())).Returns("home/index").Verifiable();

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider, urlHelper.Object);
            var viewContext = TestableHtmlGenerator.GetViewContext(model: null,
                                                                   htmlGenerator: htmlGenerator,
                                                                   metadataProvider: metadataProvider);
            var submitTagHelper = new SubmitTagHelper(urlHelperFactory.Object)
            {
                Action = "index",
                Controller = "home",
                RouteValues =
                {
                    {  "name", "value" },
                },
                ViewContext = viewContext,
            };

            // Act
            await submitTagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            urlHelper.Verify();
            Assert.Equal(3, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("id"));
            Assert.Equal("myinput", attribute.Value);
            attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("type"));
            Assert.Equal("submit", attribute.Value);
            attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("formaction"));
            Assert.Equal("home/index", attribute.Value);
            Assert.Equal("Something", output.Content.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Fact]
        public async Task ProcessAsync_CallsIntoRouteLinkWithExpectedParameters()
        {
            // Arrange
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "input",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            output.Content.SetContent(string.Empty);

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("home/index").Verifiable();

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            var metadataProvider = new TestModelMetadataProvider();
            var htmlGenerator = new TestableHtmlGenerator(metadataProvider, urlHelper.Object);
            var viewContext = TestableHtmlGenerator.GetViewContext(model: null,
                                                                   htmlGenerator: htmlGenerator,
                                                                   metadataProvider: metadataProvider);
            var submitTagHelper = new SubmitTagHelper(urlHelperFactory.Object)
            {
                Route = "Default",
                ViewContext = viewContext,
                RouteValues =
                {
                    { "name", "value" },
                },
            };

            // Act 
            await submitTagHelper.ProcessAsync(context, output);

            // Assert
            urlHelper.Verify();
            Assert.Equal("input", output.TagName);
            Assert.Equal(1, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("formaction"));
            Assert.Equal("home/index", attribute.Value);
            Assert.True(output.Content.GetContent().Length == 0);
        }

        [Fact]
        public async Task ProcessAsync_AddsAreaToRouteValuesAndCallsIntoActionLinkWithExpectedParameters()
        {
            // Arrange
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "input",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            output.Content.SetContent(string.Empty);

            var expectedRouteValues = new RouteValueDictionary(new Dictionary<string, string> { { "area", "Admin" } });
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(It.IsAny<UrlActionContext>()))
                .Returns("admin/dashboard/index")
                .Callback<UrlActionContext>(param => Assert.Equal(param.Values, expectedRouteValues))
                .Verifiable();

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            var submitTagHelper = new SubmitTagHelper(urlHelperFactory.Object)
            {
                Action = "Index",
                Controller = "Dashboard",
                Area = "Admin",
            };

            // Act
            await submitTagHelper.ProcessAsync(context, output);

            // Assert
            urlHelper.Verify();
            Assert.Equal("input", output.TagName);
            Assert.Equal(1, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("formaction"));
            Assert.Equal("admin/dashboard/index", attribute.Value);
            Assert.True(output.Content.GetContent().Length == 0);
        }

        [Fact]
        public async Task ProcessAsync_AspAreaOverridesAspRouteArea()
        {
            // Arrange
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "input",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            output.Content.SetContent(string.Empty);

            var expectedRouteValues = new RouteValueDictionary(new Dictionary<string, string> { { "area", "Admin" } });
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(It.IsAny<UrlActionContext>()))
                .Returns("admin/dashboard/index")
                .Callback<UrlActionContext>(param => Assert.Equal(param.Values, expectedRouteValues))
                .Verifiable();

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            var submitTagHelper = new SubmitTagHelper(urlHelperFactory.Object)
            {
                Action = "Index",
                Controller = "Dashboard",
                Area = "Admin",
                RouteValues = new Dictionary<string, string> { { "area", "Home" } }
            };

            // Act
            await submitTagHelper.ProcessAsync(context, output);

            // Assert
            urlHelper.Verify();
            Assert.Equal("input", output.TagName);
            Assert.Equal(1, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("formaction"));
            Assert.Equal("admin/dashboard/index", attribute.Value);
            Assert.True(output.Content.GetContent().Length == 0);
        }

        [Fact]
        public async Task ProcessAsync_EmptyStringOnAspAreaIsPassedThroughToRouteValues()
        {
            // Arrange
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "input",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            output.Content.SetContent(string.Empty);

            var expectedRouteValues = new RouteValueDictionary(new Dictionary<string, string> { { "area", string.Empty } });
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(It.IsAny<UrlActionContext>()))
                .Returns("admin/dashboard/index")
                .Callback<UrlActionContext>(param => Assert.Equal(param.Values, expectedRouteValues))
                .Verifiable();

            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);

            var submitTagHelper = new SubmitTagHelper(urlHelperFactory.Object)
            {
                Action = "Index",
                Controller = "Dashboard",
                Area = string.Empty,
            };

            // Act
            await submitTagHelper.ProcessAsync(context, output);

            // Assert
            urlHelper.Verify();
            Assert.Equal("input", output.TagName);
            Assert.Equal(1, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("formaction"));
            Assert.Equal("admin/dashboard/index", attribute.Value);
            Assert.True(output.Content.GetContent().Length == 0);
        }

        [Theory]
        [InlineData("Action")]
        [InlineData("Controller")]
        [InlineData("Route")]
        [InlineData("asp-route-")]
        public async Task ProcessAsync_ThrowsIfFormActionConflictsWithBoundAttributes(string propertyName)
        {
            // Arrange
            var urlHelperFactory = new Mock<IUrlHelperFactory>().Object;

            var submitTagHelper = new SubmitTagHelper(urlHelperFactory);

            var output = new TagHelperOutput(
                "input",
                attributes: new TagHelperAttributeList
                {
                    { "formaction", "my-action" }
                },
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
            if (propertyName == "asp-route-")
            {
                submitTagHelper.RouteValues.Add("name", "value");
            }
            else
            {
                typeof(SubmitTagHelper).GetProperty(propertyName).SetValue(submitTagHelper, "Home");
            }

            var expectedErrorMessage = "Cannot override the 'formaction' attribute for <input>. An <input> with a specified " +
                                       "'formaction' must not have attributes starting with 'asp-route-' or an " +
                                       "'asp-action', 'asp-controller', 'asp-area', or 'asp-route' attribute.";

            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => submitTagHelper.ProcessAsync(context, output));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Theory]
        [InlineData("Action")]
        [InlineData("Controller")]
        public async Task ProcessAsync_ThrowsIfRouteAndActionOrControllerProvided(string propertyName)
        {
            // Arrange
            var urlHelperFactory = new Mock<IUrlHelperFactory>().Object;

            var submitTagHelper = new SubmitTagHelper(urlHelperFactory)
            {
                Route = "Default",
            };

            typeof(SubmitTagHelper).GetProperty(propertyName).SetValue(submitTagHelper, "Home");
            var output = new TagHelperOutput(
                "input",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
            var expectedErrorMessage = "Cannot determine a 'formaction' attribute for <input>. An <input> with a specified " +
                "'asp-route' must not have an 'asp-action' or 'asp-controller' attribute.";

            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => submitTagHelper.ProcessAsync(context, output));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }
    }
}