// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
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
                    { "asp-anti-forgery", true }
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something Else");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var output = new TagHelperOutput(
                expectedTagName,
                attributes: new TagHelperAttributeList
                {
                    { "id", "myform" },
                });
            output.PostContent.SetContent("Something");
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(It.IsAny<UrlActionContext>())).Returns("home/index");

            var htmlGenerator = new TestableHtmlGenerator(metadataProvider, urlHelper.Object);
            var viewContext = TestableHtmlGenerator.GetViewContext(model: null,
                                                                   htmlGenerator: htmlGenerator,
                                                                   metadataProvider: metadataProvider);
            var expectedPostContent = "Something" + htmlGenerator.GenerateAntiForgery(viewContext)
                                                                 .ToString(TagRenderMode.SelfClosing);
            var formTagHelper = new FormTagHelper(htmlGenerator)
            {
                Action = "index",
                AntiForgery = true,
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
            Assert.True(output.Content.IsEmpty);
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData(null, "<input />")]
        [InlineData(true, "<input />")]
        [InlineData(false, "")]
        public async Task ProcessAsync_GeneratesAntiForgeryCorrectly(
            bool? antiForgery,
            string expectedPostContent)
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList());
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    It.IsAny<ViewContext>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .Returns(new TagBuilder("form", new CommonTestEncoder()));

            generator.Setup(mock => mock.GenerateAntiForgery(viewContext))
                     .Returns(new TagBuilder("input", new CommonTestEncoder()));
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                AntiForgery = antiForgery,
                ViewContext = viewContext,
            };

            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("form", output.TagName);
            Assert.False(output.SelfClosing);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.IsEmpty);
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_BindsRouteValues()
        {
            // Arrange
            var testViewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var expectedAttribute = new TagHelperAttribute("asp-ROUTEE-NotRoute", "something");
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList());
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
                .Returns(new TagBuilder("form", new CommonTestEncoder()))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                AntiForgery = false,
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
            Assert.False(output.SelfClosing);
            var attribute = Assert.Single(output.Attributes);
            Assert.Equal(expectedAttribute, attribute);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.IsEmpty);
            Assert.Empty(output.PostContent.GetContent());
            generator.Verify();
        }

        [Fact]
        public async Task ProcessAsync_CallsIntoGenerateFormWithExpectedParameters()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList());
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    viewContext,
                    "Index",
                    "Home",
                    It.IsAny<IDictionary<string, object>>(),
                    null,
                    null))
                .Returns(new TagBuilder("form", new CommonTestEncoder()))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                Action = "Index",
                AntiForgery = false,
                Controller = "Home",
                ViewContext = viewContext,
            };

            // Act & Assert
            await formTagHelper.ProcessAsync(context, output);
            generator.Verify();

            Assert.Equal("form", output.TagName);
            Assert.False(output.SelfClosing);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreElement.GetContent());
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.IsEmpty);
            Assert.Empty(output.PostContent.GetContent());
        }

        [Fact]
        public async Task ProcessAsync_CallsIntoGenerateRouteFormWithExpectedParameters()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var output = new TagHelperOutput(
                "form",
                attributes: new TagHelperAttributeList());
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateRouteForm(
                    viewContext,
                    "Default",
                    It.Is<Dictionary<string, object>>(m => string.Equals(m["name"], "value")),
                    null,
                    null))
                .Returns(new TagBuilder("form", new CommonTestEncoder()))
                .Verifiable();
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                AntiForgery = false,
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
            Assert.False(output.SelfClosing);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreElement.GetContent());
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.IsEmpty);
            Assert.Empty(output.PostContent.GetContent());
            Assert.Empty(output.PostElement.GetContent());
        }

        [Theory]
        [InlineData(true, "<input />")]
        [InlineData(false, "")]
        [InlineData(null, "")]
        public async Task ProcessAsync_SupportsAntiForgeryIfActionIsSpecified(
            bool? antiForgery,
            string expectedPostContent)
        {
            // Arrange
            var viewContext = CreateViewContext();
            var generator = new Mock<IHtmlGenerator>();

            generator.Setup(mock => mock.GenerateAntiForgery(It.IsAny<ViewContext>()))
                     .Returns(new TagBuilder("input", new CommonTestEncoder()));
            var formTagHelper = new FormTagHelper(generator.Object)
            {
                AntiForgery = antiForgery,
                ViewContext = viewContext,
            };

            var output = new TagHelperOutput("form",
                                             attributes: new TagHelperAttributeList
                                             {
                                                 { "aCTiON", "my-action" },
                                             });
            var context = new TagHelperContext(
                allAttributes: new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                    Enumerable.Empty<IReadOnlyTagHelperAttribute>()),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });


            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("form", output.TagName);
            Assert.False(output.SelfClosing);
            var attribute = Assert.Single(output.Attributes);
            Assert.Equal(new TagHelperAttribute("aCTiON", "my-action"), attribute);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.IsEmpty);
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
                });
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
                                       "'asp-action' or 'asp-controller' or 'asp-route' attribute.";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => formTagHelper.ProcessAsync(context: null, output: tagHelperOutput));

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
                attributes: new TagHelperAttributeList());
            var expectedErrorMessage = "Cannot determine an 'action' attribute for <form>. A <form> with a specified " +
                "'asp-route' must not have an 'asp-action' or 'asp-controller' attribute.";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => formTagHelper.ProcessAsync(context: null, output: output));

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