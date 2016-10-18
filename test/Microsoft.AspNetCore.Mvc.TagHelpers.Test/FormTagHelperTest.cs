// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TestCommon;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class FormTagHelperTest
    {
        [Fact]
        public async Task ProcessAsync_GeneratesExpectedOutput()
        {
            // Arrange
            var expectedTagName = "not-form";
            var metadataProvider = new TestModelMetadataProvider();
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList
                {
                    { "id", "myform" },
                    { "asp-route-name", "value" },
                    { "asp-action", "index" },
                    { "asp-controller", "home" },
                    { "method", "post" },
                    { "asp-antiforgery", true }
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                expectedTagName,
                attributes: new TagHelperAttributeList
                {
                    { "id", "myform" },
                },
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something Else");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            output.PostContent.SetContent("Something");
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(It.IsAny<UrlActionContext>())).Returns("home/index");

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider, urlHelper.Object);
            var viewContext = TestableHtmlGenerator.GetViewContext(
                model: null,
                htmlGenerator: htmlGenerator,
                metadataProvider: metadataProvider);
            var expectedPostContent = "Something" +
                HtmlContentUtilities.HtmlContentToString(
                    htmlGenerator.GenerateAntiforgery(viewContext),
                    HtmlEncoder.Default);
            var formTagHelper = new FormTagHelper(htmlGenerator)
            {
                Action = "index",
                Antiforgery = true,
                Controller = "home",
                ViewContext = viewContext,
                RouteValues =
                {
                    { "name", "value" },
                },
            };

            // Act
            await formTagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(3, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("id"));
            Assert.Equal("myform", attribute.Value);
            attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("method"));
            Assert.Equal("post", attribute.Value);
            attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("action"));
            Assert.Equal("home/index", attribute.Value);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData(null, FormMethod.Post, "<input />")]
        [InlineData(true, FormMethod.Post, "<input />")]
        [InlineData(false, FormMethod.Post, "")]
        [InlineData(null, FormMethod.Get, "")]
        [InlineData(true, FormMethod.Get, "<input />")]
        [InlineData(false, FormMethod.Get, "")]
        public async Task ProcessAsync_GeneratesAntiforgeryCorrectly(
            bool? antiforgery,
            FormMethod method,
            string expectedPostContent)
        {
            // Arrange
            var viewContext = CreateViewContext();
            var expectedAttribute = new TagHelperAttribute("method", method.ToString().ToLowerInvariant());
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(new[] { expectedAttribute }),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    It.IsAny<ViewContext>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .Returns(new TagBuilder("form"));

            generator.Setup(mock => mock.GenerateAntiforgery(viewContext))
                     .Returns(new HtmlString("<input />"));
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                Antiforgery = antiforgery,
                ViewContext = viewContext,
                Method = method.ToString().ToLowerInvariant()
            };

            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            var attribute = Assert.Single(output.Attributes);
            Assert.Equal(expectedAttribute, attribute);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_BindsRouteValues()
        {
            // Arrange
            var testViewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var expectedAttribute = new TagHelperAttribute("asp-ROUTEE-NotRoute", "something");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            output.Attributes.Add(expectedAttribute);

            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    It.IsAny<ViewContext>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .Callback<ViewContext, string, string, object, string, object>(
                    (viewContext, actionName, controllerName, routeValues, method, htmlAttributes) =>
                    {
                        // Fixes Roslyn bug with lambdas
                        generator.ToString();

                        var routeValueDictionary = (Dictionary<string, object>)routeValues;

                        Assert.Equal(2, routeValueDictionary.Count);
                        var routeValue = Assert.Single(routeValueDictionary, attr => attr.Key.Equals("val"));
                        Assert.Equal("hello", routeValue.Value);
                        routeValue = Assert.Single(routeValueDictionary, attr => attr.Key.Equals("-Name"));
                        Assert.Equal("Value", routeValue.Value);
                    })
                .Returns(new TagBuilder("form"))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                Antiforgery = false,
                ViewContext = testViewContext,
                RouteValues =
                {
                    { "val", "hello" },
                    { "-Name", "Value" },
                },
            };

            // Act & Assert
            await formTagHelper.ProcessAsync(context, output);

            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            var attribute = Assert.Single(output.Attributes);
            Assert.Equal(expectedAttribute, attribute);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Empty(output.PostContent.GetContent());
            generator.Verify();
        }

        [Fact]
        public async Task ProcessAsync_CallsIntoGenerateFormWithExpectedParameters()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    viewContext,
                    "Index",
                    "Home",
                    It.IsAny<IDictionary<string, object>>(),
                    null,
                    null))
                .Returns(new TagBuilder("form"))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                Antiforgery = false,
                Controller = "Home",
                ViewContext = viewContext,
            };

            // Act & Assert
            await formTagHelper.ProcessAsync(context, output);
            generator.Verify();

            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreElement.GetContent());
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Empty(output.PostContent.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_AspFragmentAddsFragmentToAction()
        {
            // Arrange
            var expectedTagName = "form";
            var metadataProvider = new TestModelMetadataProvider();
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList
                {
                    { "id", "myform" },
                    { "asp-route-name", "value" },
                    { "asp-action", "index" },
                    { "asp-controller", "home" },
                    { "asp-fragment", "test" },
                    { "method", "post" },
                    { "asp-antiforgery", true }
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            var output = new TagHelperOutput(
                expectedTagName,
                attributes: new TagHelperAttributeList
                {
                    { "id", "myform" },
                },
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(It.IsAny<UrlActionContext>())).Returns("home/index");

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider, urlHelper.Object);
            var viewContext = TestableHtmlGenerator.GetViewContext(
                model: null,
                htmlGenerator: htmlGenerator,
                metadataProvider: metadataProvider);

            var formTagHelper = new FormTagHelper(htmlGenerator)
            {
                Action = "index",
                Antiforgery = true,
                Controller = "home",
                Fragment = "test",
                ViewContext = viewContext,
                RouteValues =
                {
                    { "name", "value" },
                },
            };

            // Act
            await formTagHelper.ProcessAsync(tagHelperContext, output);

            // Assert

            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("action"));
            Assert.Equal("home/index#test", attribute.Value);
        }

        [Fact]
        public async Task ProcessAsync_AspAreaAddsAreaToRouteValues()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            var expectedRouteValues = new Dictionary<string, object> { { "area", "Admin" } };
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    viewContext,
                    "Index",
                    "Home",
                    expectedRouteValues,
                    null,
                    null))
                .Returns(new TagBuilder("form"))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                Antiforgery = false,
                Controller = "Home",
                Area = "Admin",
                ViewContext = viewContext,
            };

            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            generator.Verify();

            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreElement.GetContent());
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Empty(output.PostContent.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_EmptyStringOnAspAreaIsPassedThroughToRouteValues()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            var expectedRouteValues = new Dictionary<string, object> { { "area", string.Empty } };
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    viewContext,
                    "Index",
                    "Home",
                    expectedRouteValues,
                    null,
                    null))
                .Returns(new TagBuilder("form"))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                Antiforgery = false,
                Controller = "Home",
                Area = string.Empty,
                ViewContext = viewContext,
            };

            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            generator.Verify();

            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreElement.GetContent());
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Empty(output.PostContent.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_AspAreaOverridesAspRouteArea()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            var expectedRouteValues = new Dictionary<string, object> { { "area", "Admin" } };
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    viewContext,
                    "Index",
                    "Home",
                    expectedRouteValues,
                    null,
                    null))
                .Returns(new TagBuilder("form"))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                Antiforgery = false,
                Controller = "Home",
                Area = "Admin",
                RouteValues = new Dictionary<string, string> { { "area", "Client" } },
                ViewContext = viewContext,
            };

            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            generator.Verify();

            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreElement.GetContent());
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Empty(output.PostContent.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_CallsIntoGenerateRouteFormWithExpectedParameters()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateRouteForm(
                    viewContext,
                    "Default",
                    It.Is<Dictionary<string, object>>(m => string.Equals(m["name"], "value")),
                    null,
                    null))
                .Returns(new TagBuilder("form"))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Antiforgery = false,
                Route = "Default",
                ViewContext = viewContext,
                RouteValues =
                {
                    { "name", "value" },
                },
            };

            // Act & Assert
            await formTagHelper.ProcessAsync(context, output);
            generator.Verify();

            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreElement.GetContent());
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Empty(output.PostContent.GetContent());
            Assert.Empty(output.PostElement.GetContent());
        }

        [Theory]
        [InlineData(true, "<input />")]
        [InlineData(false, "")]
        [InlineData(null, "")]
        public async Task ProcessAsync_SupportsAntiforgeryIfActionIsSpecified(
            bool? antiforgery,
            string expectedPostContent)
        {
            // Arrange
            var viewContext = CreateViewContext();
            var generator = new Mock<IHtmlGenerator>();

            generator.Setup(mock => mock.GenerateAntiforgery(It.IsAny<ViewContext>()))
                     .Returns(new HtmlString("<input />"));
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Antiforgery = antiforgery,
                ViewContext = viewContext,
            };

            var output = new TagHelperOutput(
                tagName: "form",
                attributes: new TagHelperAttributeList
                {
                    { "aCTiON", "my-action" },
                },
                getChildContentAsync: (useCachedResult, encoder) =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");


            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("form", output.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
            var attribute = Assert.Single(output.Attributes);
            Assert.Equal(new TagHelperAttribute("aCTiON", "my-action"), attribute);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.GetContent().Length == 0);
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        }

        [Theory]
        [InlineData("Action")]
        [InlineData("Controller")]
        [InlineData("asp-route-")]
        public async Task ProcessAsync_ThrowsIfActionConflictsWithBoundAttributes(string propertyName)
        {
            // Arrange
            var formTagHelper = new FormTagHelper(new TestableHtmlGenerator(new EmptyModelMetadataProvider()));
            var tagHelperOutput = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList
                {
                    { "action", "my-action" },
                },
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
            if (propertyName == "asp-route-")
            {
                formTagHelper.RouteValues.Add("name", "value");
            }
            else
            {
                typeof(FormTagHelper).GetProperty(propertyName).SetValue(formTagHelper, "Home");
            }

            var expectedErrorMessage = "Cannot override the 'action' attribute for <form>. A <form> with a specified " +
                                       "'action' must not have attributes starting with 'asp-route-' or an " +
                                       "'asp-action', 'asp-controller', 'asp-fragment', 'asp-area', or 'asp-route' attribute.";

            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => formTagHelper.ProcessAsync(context, tagHelperOutput));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Theory]
        [InlineData("Action")]
        [InlineData("Controller")]
        public async Task ProcessAsync_ThrowsIfRouteAndActionOrControllerProvided(string propertyName)
        {
            // Arrange
            var formTagHelper = new FormTagHelper(new TestableHtmlGenerator(new EmptyModelMetadataProvider()))
            {
                Route = "Default",
            };
            typeof(FormTagHelper).GetProperty(propertyName).SetValue(formTagHelper, "Home");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList(),
                getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
            var expectedErrorMessage = "Cannot determine an 'action' attribute for <form>. A <form> with a specified " +
                "'asp-route' must not have an 'asp-action', 'asp-controller', or 'asp-fragment' attribute.";

            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(
                    Enumerable.Empty<TagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => formTagHelper.ProcessAsync(context, output));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        private static ViewContext CreateViewContext()
        {
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor());

            return new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                new ViewDataDictionary(new TestModelMetadataProvider()),
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null,
                new HtmlHelperOptions());
        }
    }
}