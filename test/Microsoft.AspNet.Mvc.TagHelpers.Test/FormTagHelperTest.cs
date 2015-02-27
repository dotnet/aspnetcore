// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.WebEncoders;
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
                allAttributes: new Dictionary<string, object>
                {
                    { "id", "myform" },
                    { "asp-route-foo", "bar" },
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
                attributes: new Dictionary<string, string>
                {
                    { "id", "myform" },
                    { "asp-route-foo", "bar" },
                },
                htmlEncoder: new HtmlEncoder());
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
            var formTagHelper = new FormTagHelper
            {
                Action = "index",
                AntiForgery = true,
                Controller = "home",
                Generator = htmlGenerator,
                Method = "post",
                ViewContext = viewContext,
            };

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
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.IsEmpty);
            Assert.Equal(expectedPostContent, output.PostContent.GetContent());
            Assert.Equal(expectedTagName, output.TagName);
        }

        [Theory]
        [InlineData(true, "<input />")]
        [InlineData(false, "")]
        [InlineData(null, "<input />")]
        public async Task ProcessAsync_GeneratesAntiForgeryCorrectly(bool? antiForgery, string expectedPostContent)
        {
            // Arrange
            var viewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>(),
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
                attributes: new Dictionary<string, string>(),
                htmlEncoder: new HtmlEncoder());
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(
                    It.IsAny<ViewContext>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .Returns(new TagBuilder("form", new HtmlEncoder()));

            generator.Setup(mock => mock.GenerateAntiForgery(viewContext))
                     .Returns(new TagBuilder("input", new HtmlEncoder()));
            var formTagHelper = new FormTagHelper
            {
                Action = "Index",
                AntiForgery = antiForgery,
                Generator = generator.Object,
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
        public async Task ProcessAsync_BindsRouteValuesFromTagHelperOutput()
        {
            // Arrange
            var testViewContext = CreateViewContext();
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>(),
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.SetContent("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var expectedAttribute = new KeyValuePair<string, string>("asp-ROUTEE-NotRoute", "something");
            var output = new TagHelperOutput(
                "form",
                attributes: new Dictionary<string, string>()
                {
                    { "asp-route-val", "hello" },
                    { "asp-roUte--Foo", "bar" }
                },
                htmlEncoder: new HtmlEncoder());
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
                .Returns(new TagBuilder("form", new HtmlEncoder()))
                .Verifiable();
            var formTagHelper = new FormTagHelper
            {
                Action = "Index",
                AntiForgery = false,
                Generator = generator.Object,
                ViewContext = testViewContext,
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
                allAttributes: new Dictionary<string, object>(),
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
                attributes: new Dictionary<string, string>(),
                htmlEncoder: new HtmlEncoder());
            var generator = new Mock<IHtmlGenerator>(MockBehavior.Strict);
            generator
                .Setup(mock => mock.GenerateForm(viewContext, "Index", "Home", null, "POST", null))
                .Returns(new TagBuilder("form", new HtmlEncoder()))
                .Verifiable();
            var formTagHelper = new FormTagHelper
            {
                Action = "Index",
                AntiForgery = false,
                Controller = "Home",
                Generator = generator.Object,
                Method = "POST",
                ViewContext = viewContext,
            };

            // Act & Assert
            await formTagHelper.ProcessAsync(context, output);
            generator.Verify();

            Assert.Equal("form", output.TagName);
            Assert.False(output.SelfClosing);
            Assert.Empty(output.Attributes);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.IsEmpty);
            Assert.Empty(output.PostContent.GetContent());
        }

        [Theory]
        [InlineData("my-action")]
        [InlineData("http://www.contoso.com")]
        [InlineData("my/action")]
        public async Task ProcessAsync_RestoresBoundAttributesIfActionIsSpecified(string htmlAction)
        {
            // Arrange
            var formTagHelper = new FormTagHelper
            {
                Method = "POST"
            };
            var output = new TagHelperOutput("form",
                                             attributes: new Dictionary<string, string>
                                             {
                                                 { "aCTiON", htmlAction },
                                             },
                                             htmlEncoder: new HtmlEncoder());

            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>()
                {
                    { "METhod", "POST" }
                },
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
            Assert.Equal(2, output.Attributes.Count);
            var attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("aCTiON"));
            Assert.Equal(htmlAction, attribute.Value);
            attribute = Assert.Single(output.Attributes, kvp => kvp.Key.Equals("METhod"));
            Assert.Equal("POST", attribute.Value);
            Assert.Empty(output.PreContent.GetContent());
            Assert.True(output.Content.IsEmpty);
            Assert.Empty(output.PostContent.GetContent());
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
                     .Returns(new TagBuilder("input", new HtmlEncoder()));
            var formTagHelper = new FormTagHelper
            {
                AntiForgery = antiForgery,
                Generator = generator.Object,
                ViewContext = viewContext,
            };

            var output = new TagHelperOutput("form",
                                             attributes: new Dictionary<string, string>
                                             {
                                                 { "aCTiON", "my-action" },
                                             },
                                             htmlEncoder: new HtmlEncoder());
            var context = new TagHelperContext(
                allAttributes: new Dictionary<string, object>(),
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
            Assert.Equal(new KeyValuePair<string, string>("aCTiON", "my-action"), attribute);
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
            var formTagHelper = new FormTagHelper
            {
                Method = "POST"
            };
            var tagHelperOutput = new TagHelperOutput(
                "form",
                attributes: new Dictionary<string, string>
                {
                    { "action", "my-action" },
                },
                htmlEncoder: new HtmlEncoder());
            if (propertyName == "asp-route-")
            {
                tagHelperOutput.Attributes.Add("asp-route-foo", "bar");
            }
            else
            {
                typeof(FormTagHelper).GetProperty(propertyName).SetValue(formTagHelper, "Home");
            }

            var expectedErrorMessage = "Cannot override the 'action' attribute for <form>. A <form> with a specified " +
                                       "'action' must not have attributes starting with 'asp-route-' or an " +
                                       "'asp-action' or 'asp-controller' attribute.";

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
                new ViewDataDictionary(new TestModelMetadataProvider()),
                TextWriter.Null);
        }
    }
}