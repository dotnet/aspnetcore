// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class FormTagHelperTest
{
    [Fact]
    public async Task ProcessAsync_InvokesGeneratePageForm_WithOnlyPageHandler()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var context = new TagHelperContext(
            tagName: "form",
            allAttributes: new TagHelperAttributeList()
            {
                    { "asp-handler", "page-handler" },
                    { "method", "get" }
            },
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
            .Setup(mock => mock.GeneratePageForm(
                viewContext,
                null,
                "page-handler",
                null,
                null,
                null,
                null))
            .Returns(new TagBuilder("form"))
            .Verifiable();
        var formTagHelper = new FormTagHelper(generator.Object)
        {
            ViewContext = viewContext,
            PageHandler = "page-handler",
            Method = "get"
        };

        // Act & Assert
        await formTagHelper.ProcessAsync(context, output);
        generator.Verify();
    }

    [Fact]
    public async Task ProcessAsync_ActionAndControllerGenerateAntiforgery()
    {
        // Arrange
        var expectedTagName = "form";
        var metadataProvider = new TestModelMetadataProvider();
        var tagHelperContext = new TagHelperContext(
            tagName: "form",
            allAttributes: new TagHelperAttributeList()
            {
                    { "asp-action", "index" },
                    { "asp-controller", "home" }
            },
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
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
        var expectedPostContent = HtmlContentUtilities.HtmlContentToString(
            htmlGenerator.GenerateAntiforgery(viewContext),
            HtmlEncoder.Default);
        var formTagHelper = new FormTagHelper(htmlGenerator)
        {
            ViewContext = viewContext,
            Action = "index",
            Controller = "home",
        };

        // Act
        await formTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(2, output.Attributes.Count);
        var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("method"));
        Assert.Equal("post", attribute.Value);
        attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("action"));
        Assert.Equal("home/index", attribute.Value);
        Assert.Empty(output.PreContent.GetContent());
        Assert.True(output.Content.GetContent().Length == 0);
        Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_AspAntiforgeryAloneGeneratesProperFormTag()
    {
        // Arrange
        var expectedTagName = "form";
        var metadataProvider = new TestModelMetadataProvider();
        var tagHelperContext = new TagHelperContext(
            tagName: "form",
            allAttributes: new TagHelperAttributeList()
            {
                    { "asp-antiforgery", true }
            },
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
        var urlHelper = new Mock<IUrlHelper>();
        var htmlGenerator = new TestableHtmlGenerator(metadataProvider, urlHelper.Object);
        var viewContext = TestableHtmlGenerator.GetViewContext(
            model: null,
            htmlGenerator: htmlGenerator,
            metadataProvider: metadataProvider);
        viewContext.HttpContext.Request.Path = "/home/index";
        var expectedPostContent = HtmlContentUtilities.HtmlContentToString(
            htmlGenerator.GenerateAntiforgery(viewContext),
            HtmlEncoder.Default);
        var formTagHelper = new FormTagHelper(htmlGenerator)
        {
            ViewContext = viewContext,
            Antiforgery = true,
        };

        // Act
        await formTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Equal(2, output.Attributes.Count);
        var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("method"));
        Assert.Equal("post", attribute.Value);
        attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("action"));
        Assert.Equal("/home/index", attribute.Value);
        Assert.Empty(output.PreContent.GetContent());
        Assert.True(output.Content.GetContent().Length == 0);
        Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_EmptyHtmlStringActionGeneratesAntiforgery()
    {
        // Arrange
        var expectedTagName = "form";
        var metadataProvider = new TestModelMetadataProvider();
        var tagHelperContext = new TagHelperContext(
            tagName: "form",
            allAttributes: new TagHelperAttributeList()
            {
                    { "method", new HtmlString("post") }
            },
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList()
            {
                    { "action", HtmlString.Empty },
            },
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
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
        var expectedPostContent = HtmlContentUtilities.HtmlContentToString(
            htmlGenerator.GenerateAntiforgery(viewContext),
            HtmlEncoder.Default);
        var formTagHelper = new FormTagHelper(htmlGenerator)
        {
            ViewContext = viewContext,
            Method = "post",
        };

        // Act
        await formTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("action"));
        Assert.Equal(HtmlString.Empty, attribute.Value);
        Assert.Empty(output.PreElement.GetContent());
        Assert.Empty(output.PreContent.GetContent());
        Assert.Empty(output.Content.GetContent());
        Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        Assert.Empty(output.PostElement.GetContent());
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_EmptyStringActionGeneratesAntiforgery()
    {
        // Arrange
        var expectedTagName = "form";
        var metadataProvider = new TestModelMetadataProvider();
        var tagHelperContext = new TagHelperContext(
            tagName: "form",
            allAttributes: new TagHelperAttributeList()
            {
                    { "method", new HtmlString("post") }
            },
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList()
            {
                    { "action", string.Empty },
            },
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something");
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
        var expectedPostContent = HtmlContentUtilities.HtmlContentToString(
            htmlGenerator.GenerateAntiforgery(viewContext),
            HtmlEncoder.Default);
        var formTagHelper = new FormTagHelper(htmlGenerator)
        {
            ViewContext = viewContext,
            Method = "post",
        };

        // Act
        await formTagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        var attribute = Assert.Single(output.Attributes, attr => attr.Name.Equals("action"));
        Assert.Equal(string.Empty, attribute.Value);
        Assert.Empty(output.PreElement.GetContent());
        Assert.Empty(output.PreContent.GetContent());
        Assert.Empty(output.Content.GetContent());
        Assert.Equal(expectedPostContent, output.PostContent.GetContent());
        Assert.Empty(output.PostElement.GetContent());
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_GeneratesExpectedOutput()
    {
        // Arrange
        var expectedTagName = "not-form";
        var metadataProvider = new TestModelMetadataProvider();
        var tagHelperContext = new TagHelperContext(
            tagName: "form",
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
            tagName: "form",
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
            tagName: "form",
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

                    var routeValueDictionary = Assert.IsType<RouteValueDictionary>(routeValues);
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
            tagName: "form",
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
            tagName: "form",
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
            tagName: "form",
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
            tagName: "form",
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
            tagName: "form",
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
            tagName: "form",
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
                It.Is<RouteValueDictionary>(m => string.Equals(m["name"], "value")),
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

    [Fact]
    public async Task ProcessAsync_InvokesGeneratePageForm()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var context = new TagHelperContext(
            tagName: "form",
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
            .Setup(mock => mock.GeneratePageForm(
                viewContext,
                "/Home/Admin/Post",
                "page-handler",
                null,
                "hello-world",
                null,
                null))
            .Returns(new TagBuilder("form"))
            .Verifiable();
        var formTagHelper = new FormTagHelper(generator.Object)
        {
            Antiforgery = false,
            ViewContext = viewContext,
            Page = "/Home/Admin/Post",
            PageHandler = "page-handler",
            Fragment = "hello-world",
        };

        // Act & Assert
        await formTagHelper.ProcessAsync(context, output);
        generator.Verify();
    }

    [Fact]
    public async Task ProcessAsync_WithPageAndArea_InvokesGeneratePageForm()
    {
        // Arrange
        var viewContext = CreateViewContext();
        var context = new TagHelperContext(
            tagName: "form",
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
            .Setup(mock => mock.GeneratePageForm(
                viewContext,
                "/Home/Admin/Post",
                "page-handler",
                It.IsAny<object>(),
                "hello-world",
                null,
                null))
            .Callback((ViewContext _, string pageName, string pageHandler, object routeValues, string fragment, string method, object htmlAttributes) =>
            {
                var rvd = Assert.IsType<RouteValueDictionary>(routeValues);
                Assert.Collection(
                    rvd.OrderBy(item => item.Key),
                    item =>
                    {
                        Assert.Equal("area", item.Key);
                        Assert.Equal("test-area", item.Value);
                    });
            })
            .Returns(new TagBuilder("form"))
            .Verifiable();
        var formTagHelper = new FormTagHelper(generator.Object)
        {
            Antiforgery = false,
            ViewContext = viewContext,
            Page = "/Home/Admin/Post",
            PageHandler = "page-handler",
            Fragment = "hello-world",
            Area = "test-area",
        };

        // Act & Assert
        await formTagHelper.ProcessAsync(context, output);
        generator.Verify();
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
            tagName: "form",
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
    [InlineData("Page")]
    [InlineData("PageHandler")]
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
            "'asp-action', 'asp-controller', 'asp-fragment', 'asp-area', 'asp-route', 'asp-page' or 'asp-page-handler' attribute.";

        var context = new TagHelperContext(
            tagName: "form",
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
        var expectedErrorMessage = string.Join(
            Environment.NewLine,
            "Cannot determine the 'action' attribute for <form>. The following attributes are mutually exclusive:",
            "asp-route",
            "asp-controller, asp-action",
            "asp-page");

        var context = new TagHelperContext(
            tagName: "form",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => formTagHelper.ProcessAsync(context, output));

        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_ThrowsIfRouteAndPageProvided()
    {
        // Arrange
        var formTagHelper = new FormTagHelper(new TestableHtmlGenerator(new EmptyModelMetadataProvider()))
        {
            Route = "Default",
            Page = "Page",
        };
        var output = new TagHelperOutput(
            "form",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var expectedErrorMessage = string.Join(
            Environment.NewLine,
            "Cannot determine the 'action' attribute for <form>. The following attributes are mutually exclusive:",
            "asp-route",
            "asp-controller, asp-action",
            "asp-page");

        var context = new TagHelperContext(
            tagName: "form",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => formTagHelper.ProcessAsync(context, output));

        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_ThrowsIfActionAndPageProvided()
    {
        // Arrange
        var formTagHelper = new FormTagHelper(new TestableHtmlGenerator(new EmptyModelMetadataProvider()))
        {
            Action = "Default",
            Page = "Page",
        };
        var output = new TagHelperOutput(
            "form",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var expectedErrorMessage = string.Join(
            Environment.NewLine,
            "Cannot determine the 'action' attribute for <form>. The following attributes are mutually exclusive:",
            "asp-route",
            "asp-controller, asp-action",
            "asp-page");

        var context = new TagHelperContext(
            tagName: "form",
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
            new ViewDataDictionary(new TestModelMetadataProvider(), new ModelStateDictionary()),
            Mock.Of<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());
    }
}
