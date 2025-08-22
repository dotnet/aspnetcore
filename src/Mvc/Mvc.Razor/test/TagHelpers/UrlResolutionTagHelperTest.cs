// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

public class UrlResolutionTagHelperTest
{
    public static TheoryData<string, string> ResolvableUrlData
    {
        get
        {
            // url, expectedHref
            return new TheoryData<string, string>
                {
                   { "~/home/index.html", "/approot/home/index.html" },
                   { "~/home/index.html\r\n", "/approot/home/index.html" },
                   { "  ~/home/index.html", "/approot/home/index.html" },
                   { "\u000C~/home/index.html\r\n", "/approot/home/index.html" },
                   { "\t ~/home/index.html\n", "/approot/home/index.html" },
                   { "\r\n~/home/index.html\u000C\t", "/approot/home/index.html" },
                   { "\r~/home/index.html\t", "/approot/home/index.html" },
                   { "\n~/home/index.html\u202F", "/approot/home/index.html\u202F" },
                   {
                        "~/home/index.html ~/secondValue/index.html",
                        "/approot/home/index.html ~/secondValue/index.html"
                   },
                   { "  ~/   ", "/approot/" },
                   { "  ~/", "/approot/" },
                };
        }
    }

    public static TheoryData<string, string> ResolvableUrlVersionData
    {
        get
        {
            // url, expectedHref
            return new TheoryData<string, string>
                {
                   { "~/home/index.html", "/approot/home/index.fingerprint.html" },
                   { "~/home/index.html\r\n", "/approot/home/index.fingerprint.html" },
                   { "  ~/home/index.html", "/approot/home/index.fingerprint.html" },
                   { "\u000C~/home/index.html\r\n", "/approot/home/index.fingerprint.html" },
                   { "\t ~/home/index.html\n", "/approot/home/index.fingerprint.html" },
                   { "\r\n~/home/index.html\u000C\t", "/approot/home/index.fingerprint.html" },
                   { "\r~/home/index.html\t", "/approot/home/index.fingerprint.html" },
                   { "\n~/home/index.html\u202F", "/approot/home/index.fingerprint.html\u202F" },
                   {
                        "~/home/index.html ~/secondValue/index.html",
                        "/approot/home/index.html ~/secondValue/index.html"
                   },
                };
        }
    }

    [Fact]
    public void Process_DoesNothingIfTagNameIsNull()
    {
        // Arrange
        var tagHelperOutput = new TagHelperOutput(
            tagName: null,
            attributes: new TagHelperAttributeList
            {
                    { "href", "~/home/index.html" }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));

        var tagHelper = new UrlResolutionTagHelper(Mock.Of<IUrlHelperFactory>(), new HtmlTestEncoder());
        var context = new TagHelperContext(
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        tagHelper.Process(context, tagHelperOutput);

        // Assert
        var attribute = Assert.Single(tagHelperOutput.Attributes);
        Assert.Equal("href", attribute.Name, StringComparer.Ordinal);
        var attributeValue = Assert.IsType<string>(attribute.Value);
        Assert.Equal("~/home/index.html", attributeValue, StringComparer.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ResolvableUrlData))]
    public void Process_ResolvesTildeSlashValues(string url, string expectedHref)
    {
        // Arrange
        var tagHelperOutput = new TagHelperOutput(
            tagName: "a",
            attributes: new TagHelperAttributeList
            {
                    { "href", url }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(urlHelper => urlHelper.Content(It.IsAny<string>()))
            .Returns(new Func<string, string>(value => "/approot" + value.Substring(1)));
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelperMock.Object);
        var tagHelper = new UrlResolutionTagHelper(urlHelperFactory.Object, new HtmlTestEncoder())
        {
            ViewContext = new Rendering.ViewContext { HttpContext = new DefaultHttpContext() }
        };

        var context = new TagHelperContext(
            tagName: "a",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        tagHelper.Process(context, tagHelperOutput);

        // Assert
        var attribute = Assert.Single(tagHelperOutput.Attributes);
        Assert.Equal("href", attribute.Name, StringComparer.Ordinal);
        var attributeValue = Assert.IsType<string>(attribute.Value);
        Assert.Equal(expectedHref, attributeValue, StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.DoubleQuotes, attribute.ValueStyle);
    }

    public static TheoryData<string, string> ResolvableUrlHtmlStringData
    {
        get
        {
            // url, expectedHref
            return new TheoryData<string, string>
                {
                   { "~/home/index.html", "HtmlEncode[[/approot/]]home/index.html" },
                   { "  ~/home/index.html", "HtmlEncode[[/approot/]]home/index.html" },
                   {
                        "~/home/index.html ~/secondValue/index.html",
                        "HtmlEncode[[/approot/]]home/index.html ~/secondValue/index.html"
                   },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ResolvableUrlHtmlStringData))]
    public void Process_ResolvesTildeSlashValues_InHtmlString(string url, string expectedHref)
    {
        // Arrange
        var tagHelperOutput = new TagHelperOutput(
            tagName: "a",
            attributes: new TagHelperAttributeList
            {
                    { "href", new HtmlString(url) }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(urlHelper => urlHelper.Content(It.IsAny<string>()))
            .Returns(new Func<string, string>(value => "/approot" + value.Substring(1)));
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelperMock.Object);
        var tagHelper = new UrlResolutionTagHelper(urlHelperFactory.Object, new HtmlTestEncoder())
        {
            ViewContext = new Rendering.ViewContext { HttpContext = new DefaultHttpContext() }
        };

        var context = new TagHelperContext(
            tagName: "a",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        tagHelper.Process(context, tagHelperOutput);

        // Assert
        var attribute = Assert.Single(tagHelperOutput.Attributes);
        Assert.Equal("href", attribute.Name, StringComparer.Ordinal);
        var htmlContent = Assert.IsAssignableFrom<IHtmlContent>(attribute.Value);
        Assert.Equal(expectedHref, HtmlContentUtilities.HtmlContentToString(htmlContent), StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.DoubleQuotes, attribute.ValueStyle);
    }

    public static TheoryData<string> UnresolvableUrlData
    {
        get
        {
            // url
            return new TheoryData<string>
                {
                   { "/home/index.html" },
                   { "~ /home/index.html" },
                   { "/home/index.html ~/second/wontresolve.html" },
                   { "  ~\\home\\index.html" },
                   { "~\\/home/index.html" },
                   { "  ~" },
                   { "   " },
                };
        }
    }

    [Theory]
    [MemberData(nameof(UnresolvableUrlData))]
    public void Process_DoesNotResolveNonTildeSlashValues(string url)
    {
        // Arrange
        var tagHelperOutput = new TagHelperOutput(
            tagName: "a",
            attributes: new TagHelperAttributeList
            {
                    { "href", url }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(urlHelper => urlHelper.Content(It.IsAny<string>()))
            .Returns("approot/home/index.html");
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelperMock.Object);
        var tagHelper = new UrlResolutionTagHelper(urlHelperFactory.Object, new HtmlTestEncoder());

        var context = new TagHelperContext(
            tagName: "a",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        tagHelper.Process(context, tagHelperOutput);

        // Assert
        var attribute = Assert.Single(tagHelperOutput.Attributes);
        Assert.Equal("href", attribute.Name, StringComparer.Ordinal);
        var attributeValue = Assert.IsType<string>(attribute.Value);
        Assert.Equal(url, attributeValue, StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.DoubleQuotes, attribute.ValueStyle);
    }

    public static TheoryData<string> UnresolvableUrlHtmlStringData
    {
        get
        {
            // url
            return new TheoryData<string>
                {
                   { "/home/index.html" },
                   { "~ /home/index.html" },
                   { "/home/index.html ~/second/wontresolve.html" },
                   { "~\\home\\index.html" },
                   { "~\\/home/index.html" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(UnresolvableUrlHtmlStringData))]
    public void Process_DoesNotResolveNonTildeSlashValues_InHtmlString(string url)
    {
        // Arrange
        var tagHelperOutput = new TagHelperOutput(
            tagName: "a",
            attributes: new TagHelperAttributeList
            {
                    { "href", new HtmlString(url) }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(urlHelper => urlHelper.Content(It.IsAny<string>()))
            .Returns("approot/home/index.html");
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelperMock.Object);
        var tagHelper = new UrlResolutionTagHelper(urlHelperFactory.Object, new HtmlTestEncoder());

        var context = new TagHelperContext(
            tagName: "a",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        tagHelper.Process(context, tagHelperOutput);

        // Assert
        var attribute = Assert.Single(tagHelperOutput.Attributes);
        Assert.Equal("href", attribute.Name, StringComparer.Ordinal);
        var attributeValue = Assert.IsType<HtmlString>(attribute.Value);
        Assert.Equal(url.ToString(), attributeValue.ToString(), StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.DoubleQuotes, attribute.ValueStyle);
    }

    [Fact]
    public void Process_IgnoresNonHtmlStringOrStringValues()
    {
        // Arrange
        var tagHelperOutput = new TagHelperOutput(
            tagName: "a",
            attributes: new TagHelperAttributeList
            {
                    { "href", true }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var tagHelper = new UrlResolutionTagHelper(urlHelperFactory: null, htmlEncoder: null);

        var context = new TagHelperContext(
            tagName: "a",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        tagHelper.Process(context, tagHelperOutput);

        // Assert
        var attribute = Assert.Single(tagHelperOutput.Attributes);
        Assert.Equal("href", attribute.Name, StringComparer.Ordinal);
        Assert.True(Assert.IsType<bool>(attribute.Value));
        Assert.Equal(HtmlAttributeValueStyle.DoubleQuotes, attribute.ValueStyle);
    }

    [Fact]
    public void Process_ThrowsWhenEncodingNeededAndIUrlHelperActsUnexpectedly()
    {
        // Arrange
        var relativeUrl = "~/home/index.html";
        var expectedExceptionMessage = Resources.FormatCouldNotResolveApplicationRelativeUrl_TagHelper(
            relativeUrl,
            nameof(IUrlHelper),
            nameof(IUrlHelper.Content),
            "removeTagHelper",
            typeof(UrlResolutionTagHelper).FullName,
            typeof(UrlResolutionTagHelper).Assembly.GetName().Name);
        var tagHelperOutput = new TagHelperOutput(
            tagName: "a",
            attributes: new TagHelperAttributeList
            {
                    { "href", new HtmlString(relativeUrl) }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(urlHelper => urlHelper.Content(It.IsAny<string>()))
            .Returns("UnexpectedResult");
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelperMock.Object);
        var tagHelper = new UrlResolutionTagHelper(urlHelperFactory.Object, new HtmlTestEncoder())
        {
            ViewContext = new Rendering.ViewContext { HttpContext = new DefaultHttpContext() }
        };

        var context = new TagHelperContext(
            tagName: "a",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => tagHelper.Process(context, tagHelperOutput));
        Assert.Equal(expectedExceptionMessage, exception.Message, StringComparer.Ordinal);
    }

    [Theory]
    [MemberData(nameof(ResolvableUrlVersionData))]
    public void Process_ResolvesVersionedUrls_WhenResourceCollectionIsAvailable(string url, string expectedHref)
    {
        // Arrange
        var tagHelperOutput = new TagHelperOutput(
            tagName: "a",
            attributes: new TagHelperAttributeList
            {
                    { "href", url }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock
            .Setup(urlHelper => urlHelper.Content(It.IsAny<string>()))
            .Returns(new Func<string, string>(value => "/approot" + value.Substring(1)));
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelperMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(
            (context) => Task.CompletedTask,
            new EndpointMetadataCollection(
                [new ResourceAssetCollection([new("home/index.fingerprint.html", [new ResourceAssetProperty("label", "home/index.html")])])]),
            "Test"));

        var tagHelper = new UrlResolutionTagHelper(urlHelperFactory.Object, new HtmlTestEncoder())
        {
            ViewContext = new Rendering.ViewContext { HttpContext = httpContext }
        };

        var context = new TagHelperContext(
            tagName: "a",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act
        tagHelper.Process(context, tagHelperOutput);

        // Assert
        var attribute = Assert.Single(tagHelperOutput.Attributes);
        Assert.Equal("href", attribute.Name, StringComparer.Ordinal);
        var attributeValue = Assert.IsType<string>(attribute.Value);
        Assert.Equal(expectedHref, attributeValue, StringComparer.Ordinal);
        Assert.Equal(HtmlAttributeValueStyle.DoubleQuotes, attribute.ValueStyle);
    }
}
