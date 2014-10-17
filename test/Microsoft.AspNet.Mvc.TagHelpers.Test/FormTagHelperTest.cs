// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PipelineCore;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Routing;
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
            var metadataProvider = new DataAnnotationsModelMetadataProvider();
            var formTagHelper = new FormTagHelper
            {
                Action = "index",
                Controller = "home",
                Method = "post",
                AntiForgery = true
            };
            var tagHelperContext = new TagHelperContext(
                allAttributes: new Dictionary<string, object>
                {
                    { "id", "myform" },
                    { "route-foo", "bar" },
                    { "action", "index" },
                    { "controller", "home" },
                    { "method", "post" },
                    { "anti-forgery", true }
                });
            var output = new TagHelperOutput(
                "form",
                attributes: new Dictionary<string, string>
                {
                    { "id", "myform" },
                    { "route-foo", "bar" },
                },
                content: "Something");
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper
                .Setup(mock => mock.Action(It.IsAny<string>(),
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
            var expectedContent = "Something" + htmlGenerator.GenerateAntiForgery(viewContext)
                                                             .ToString(TagRenderMode.SelfClosing);
            formTagHelper.ViewContext = viewContext;
            formTagHelper.Generator = htmlGenerator;

            // Act
            await formTagHelper.ProcessAsync(tagHelperContext, output);

            // Assert
            Assert.Equal(3, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("id"));
            Assert.Equal("myform", attribute.Value);
            attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("method"));
            Assert.Equal("post", attribute.Value);
            attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("action"));
            Assert.Equal("home/index", attribute.Value);
            Assert.Equal(expectedContent, output.Content);
            Assert.Equal("form", output.TagName);
        }

        [Theory]
        [InlineData(true, "<input />")]
        [InlineData(false, "")]
        [InlineData(null, "<input />")]
        public async Task ProcessAsync_GeneratesAntiForgeryCorrectly(bool? antiForgery, string expectedContent)
        {
            // Arrange
            var viewContext = CreateViewContext();
            var formTagHelper = new FormTagHelper
            {
                Action = "Index",
                AntiForgery = antiForgery
            };
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>());
            var output = new TagHelperOutput(
                "form",
                attributes: new Dictionary<string, string>(),
                content: string.Empty);
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

            generator.Setup(mock => mock.GenerateAntiForgery(viewContext))
                     .Returns(new TagBuilder("input"));
            formTagHelper.ViewContext = viewContext;
            formTagHelper.Generator = generator.Object;

            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("form", output.TagName);
            Assert.Empty(output.Attributes);
            Assert.Equal(expectedContent, output.Content);
        }

        [Fact]
        public async Task ProcessAsync_BindsRouteValuesFromTagHelperOutput()
        {
            // Arrange
            var testViewContext = CreateViewContext();
            var formTagHelper = new FormTagHelper
            {
                Action = "Index",
                AntiForgery = false
            };
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>());
            var expectedAttribute = new KeyValuePair<string, string>("ROUTEE-NotRoute", "something");
            var output = new TagHelperOutput(
                "form",
                attributes: new Dictionary<string, string>()
                {
                    { "route-val", "hello" },
                    { "roUte--Foo", "bar" }
                },
                content: string.Empty);
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
                        var routeValue = Assert.Single(routeValueDictionary, kvp => kvp.Key.Equals("val"));
                        Assert.Equal("hello", routeValue.Value);
                        routeValue = Assert.Single(routeValueDictionary, kvp => kvp.Key.Equals("-Foo"));
                        Assert.Equal("bar", routeValue.Value);
                    })
                .Returns(new TagBuilder("form"))
                .Verifiable();
            formTagHelper.ViewContext = testViewContext;
            formTagHelper.Generator = generator.Object;

            // Act & Assert
            await formTagHelper.ProcessAsync(context, output);

            Assert.Equal("form", output.TagName);
            var attribute = Assert.Single(output.Attributes);
            Assert.Equal(expectedAttribute, attribute);
            Assert.Empty(output.Content);
            generator.Verify();
        }

        [Fact]
        public async Task ProcessAsync_CallsIntoGenerateFormWithExpectedParameters()
        {
            // Arrange
            var viewContext = CreateViewContext();
            var formTagHelper = new FormTagHelper
            {
                Action = "Index",
                Controller = "Home",
                Method = "POST",
                AntiForgery = false
            };
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>());
            var output = new TagHelperOutput(
                "form",
                attributes: new Dictionary<string, string>(),
                content: string.Empty);
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(viewContext, "Index", "Home", null, "POST", null))
                .Returns(new TagBuilder("form"))
                .Verifiable();
            formTagHelper.ViewContext = viewContext;
            formTagHelper.Generator = generator.Object;

            // Act & Assert
            await formTagHelper.ProcessAsync(context, output);
            generator.Verify();

            Assert.Equal("form", output.TagName);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.Content);
        }

        [Fact]
        public async Task ProcessAsync_RestoresBoundAttributesIfActionIsURL()
        {
            // Arrange
            var formTagHelper = new FormTagHelper
            {
                Action = "http://www.contoso.com",
                Method = "POST"
            };
            var output = new TagHelperOutput("form",
                                             attributes: new Dictionary<string, string>(),
                                             content: string.Empty);
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>()
                {
                    { "aCTiON", "http://www.contoso.com" },
                    { "METhod", "POST" }
                });

            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("form", output.TagName);
            Assert.Equal(2, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("aCTiON"));
            Assert.Equal("http://www.contoso.com", attribute.Value);
            attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("METhod"));
            Assert.Equal("POST", attribute.Value);
            Assert.Empty(output.Content);
        }

        [Theory]
        [InlineData(true, "<input />")]
        [InlineData(false, "")]
        [InlineData(null, "")]
        public async Task ProcessAsync_SupportsAntiForgeryIfActionIsURL(bool? antiForgery, string expectedContent)
        {
            // Arrange
            var viewContext = CreateViewContext();
            var generator = new Mock<IHtmlGenerator>();
            generator.Setup(mock => mock.GenerateAntiForgery(It.IsAny<ViewContext>()))
                     .Returns(new TagBuilder("input"));
            var formTagHelper = new FormTagHelper
            {
                Action = "http://www.contoso.com",
                AntiForgery = antiForgery,
            };
            formTagHelper.ViewContext = viewContext;
            formTagHelper.Generator = generator.Object;

            var output = new TagHelperOutput("form",
                                             attributes: new Dictionary<string, string>(),
                                             content: string.Empty);
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>()
                {
                    { "aCTiON", "http://www.contoso.com" }
                });

            // Act
            await formTagHelper.ProcessAsync(context, output);

            // Assert
            Assert.Equal("form", output.TagName);
            var attribute = Assert.Single(output.Attributes);
            Assert.Equal(new KeyValuePair<string, string>("aCTiON", "http://www.contoso.com"), attribute);
            Assert.Equal(expectedContent, output.Content);
        }

        [Fact]
        public async Task ProcessAsync_ThrowsIfActionIsUrlWithSpecifiedController()
        {
            // Arrange
            var formTagHelper = new FormTagHelper
            {
                Action = "http://www.contoso.com",
                Controller = "Home",
                Method = "POST"
            };
            var expectedErrorMessage = "Cannot determine an 'action' for <form>. A <form> with a URL-based 'action' " +
                                       "must not have attributes starting with 'route-' or a 'controller' attribute.";
            var tagHelperOutput = new TagHelperOutput(
                "form",
                attributes: new Dictionary<string, string>(),
                content: string.Empty);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => formTagHelper.ProcessAsync(context: null, output: tagHelperOutput));

            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Fact]
        public async Task ProcessAsync_ThrowsIfActionIsUrlWithSpecifiedRoutes()
        {
            // Arrange
            var formTagHelper = new FormTagHelper
            {
                Action = "http://www.contoso.com",
                Method = "POST"
            };
            var expectedErrorMessage = "Cannot determine an 'action' for <form>. A <form> with a URL-based 'action' " +
                                       "must not have attributes starting with 'route-' or a 'controller' attribute.";
            var tagHelperOutput = new TagHelperOutput(
                "form",
                attributes: new Dictionary<string, string>
                {
                    { "route-foo", "bar" }
                },
                content: string.Empty);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => formTagHelper.ProcessAsync(context: null, output: tagHelperOutput));

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
                new ViewDataDictionary(
                    new DataAnnotationsModelMetadataProvider()),
                TextWriter.Null);
        }
    }
}