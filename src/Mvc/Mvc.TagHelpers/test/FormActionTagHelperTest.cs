// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class FormActionTagHelperTest
{
    [Fact]
    public async Task ProcessAsync_GeneratesExpectedOutput()
    {
        // Arrange
        var expectedTagName = "not-button-or-submit";
        var metadataProvider = new TestModelMetadataProvider();

        var tagHelperContext = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList
            {
                    { "id", "my-id" },
            },
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList
            {
                    { "id", "my-id" },
            },
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something Else");  // ignored
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelper
            .Setup(mock => mock.Action(It.IsAny<UrlActionContext>()))
            .Returns<UrlActionContext>(c => $"{c.Controller}/{c.Action}/{(c.Values as RouteValueDictionary)["name"]}");

        var viewContext = new ViewContext();
        var urlHelperFactory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(viewContext))
            .Returns(urlHelper.Object);

        var tagHelper = new FormActionTagHelper(urlHelperFactory.Object)
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
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Collection(
            output.Attributes,
            attribute =>
            {
                Assert.Equal("id", attribute.Name, StringComparer.Ordinal);
                Assert.Equal("my-id", attribute.Value as string, StringComparer.Ordinal);
            },
            attribute =>
            {
                Assert.Equal("formaction", attribute.Name, StringComparer.Ordinal);
                Assert.Equal("home/index/value", attribute.Value as string, StringComparer.Ordinal);
            });
        Assert.False(output.IsContentModified);
        Assert.False(output.PostContent.IsModified);
        Assert.False(output.PostElement.IsModified);
        Assert.False(output.PreContent.IsModified);
        Assert.False(output.PreElement.IsModified);
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
        Assert.Equal(expectedTagName, output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_GeneratesExpectedOutput_WithRoute()
    {
        // Arrange
        var expectedTagName = "not-button-or-submit";
        var metadataProvider = new TestModelMetadataProvider();

        var tagHelperContext = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList
            {
                    { "id", "my-id" },
            },
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            expectedTagName,
            attributes: new TagHelperAttributeList
            {
                    { "id", "my-id" },
            },
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent("Something Else");  // ignored
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelper
            .Setup(mock => mock.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Returns<UrlRouteContext>(c => $"{c.RouteName}/{(c.Values as RouteValueDictionary)["name"]}");

        var viewContext = new ViewContext();
        var urlHelperFactory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(viewContext))
            .Returns(urlHelper.Object);

        var tagHelper = new FormActionTagHelper(urlHelperFactory.Object)
        {
            Route = "routine",
            RouteValues =
                {
                    {  "name", "value" },
                },
            ViewContext = viewContext,
        };

        // Act
        await tagHelper.ProcessAsync(tagHelperContext, output);

        // Assert
        Assert.Collection(
            output.Attributes,
            attribute =>
            {
                Assert.Equal("id", attribute.Name, StringComparer.Ordinal);
                Assert.Equal("my-id", attribute.Value as string, StringComparer.Ordinal);
            },
            attribute =>
            {
                Assert.Equal("formaction", attribute.Name, StringComparer.Ordinal);
                Assert.Equal("routine/value", attribute.Value as string, StringComparer.Ordinal);
            });
        Assert.False(output.IsContentModified);
        Assert.False(output.PostContent.IsModified);
        Assert.False(output.PostElement.IsModified);
        Assert.False(output.PreContent.IsModified);
        Assert.False(output.PreElement.IsModified);
        Assert.Equal(TagMode.StartTagAndEndTag, output.TagMode);
        Assert.Equal(expectedTagName, output.TagName);
    }

    // RouteValues property value, expected RouteValuesDictionary content.
    public static TheoryData<IDictionary<string, string>, IDictionary<string, object>> RouteValuesData
    {
        get
        {
            return new TheoryData<IDictionary<string, string>, IDictionary<string, object>>
                {
                    { null, null },
                    // FormActionTagHelper ignores an empty route values dictionary.
                    { new Dictionary<string, string>(), null },
                    {
                        new Dictionary<string, string> { { "name", "value" } },
                        new Dictionary<string, object> { { "name", "value" } }
                    },
                    {
                        new SortedDictionary<string, string>(StringComparer.Ordinal)
                        {
                            { "name1", "value1" },
                            { "name2", "value2" },
                        },
                        new SortedDictionary<string, object>(StringComparer.Ordinal)
                        {
                            { "name1", "value1" },
                            { "name2", "value2" },
                        }
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(RouteValuesData))]
    public async Task ProcessAsync_CallsActionWithExpectedParameters(
        IDictionary<string, string> routeValues,
        IDictionary<string, object> expectedRouteValues)
    {
        // Arrange
        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            "button",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
            });

        var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelper
            .Setup(mock => mock.Action(It.IsAny<UrlActionContext>()))
            .Callback<UrlActionContext>(param =>
            {
                Assert.Equal("delete", param.Action, StringComparer.Ordinal);
                Assert.Equal("books", param.Controller, StringComparer.Ordinal);
                Assert.Equal("test", param.Fragment, StringComparer.Ordinal);
                Assert.Null(param.Host);
                Assert.Null(param.Protocol);
                Assert.Equal<KeyValuePair<string, object>>(expectedRouteValues, param.Values as RouteValueDictionary);
            })
            .Returns("home/index");

        var viewContext = new ViewContext();
        var urlHelperFactory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(viewContext))
            .Returns(urlHelper.Object);

        var tagHelper = new FormActionTagHelper(urlHelperFactory.Object)
        {
            Action = "delete",
            Controller = "books",
            Fragment = "test",
            RouteValues = routeValues,
            ViewContext = viewContext,
        };

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("button", output.TagName);
        var attribute = Assert.Single(output.Attributes);
        Assert.Equal("formaction", attribute.Name);
        Assert.Equal("home/index", attribute.Value);
        Assert.Empty(output.Content.GetContent());
    }

    [Theory]
    [MemberData(nameof(RouteValuesData))]
    public async Task ProcessAsync_CallsRouteUrlWithExpectedParameters(
        IDictionary<string, string> routeValues,
        IDictionary<string, object> expectedRouteValues)
    {
        // Arrange
        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            "button",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
            });

        var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelper
            .Setup(mock => mock.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback<UrlRouteContext>(param =>
            {
                Assert.Null(param.Host);
                Assert.Null(param.Protocol);
                Assert.Equal("test", param.Fragment, StringComparer.Ordinal);
                Assert.Equal("Default", param.RouteName, StringComparer.Ordinal);
                Assert.Equal<KeyValuePair<string, object>>(expectedRouteValues, param.Values as RouteValueDictionary);
            })
            .Returns("home/index");

        var viewContext = new ViewContext();
        var urlHelperFactory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(viewContext))
            .Returns(urlHelper.Object);

        var tagHelper = new FormActionTagHelper(urlHelperFactory.Object)
        {
            Route = "Default",
            Fragment = "test",
            RouteValues = routeValues,
            ViewContext = viewContext,
        };

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("button", output.TagName);
        var attribute = Assert.Single(output.Attributes);
        Assert.Equal("formaction", attribute.Name);
        Assert.Equal("home/index", attribute.Value);
        Assert.Empty(output.Content.GetContent());
    }

    // Area property value, RouteValues property value, expected "area" in final RouteValuesDictionary.
    public static TheoryData<string, Dictionary<string, string>, string> AreaRouteValuesData
    {
        get
        {
            return new TheoryData<string, Dictionary<string, string>, string>
                {
                    { "Area", null, "Area" },
                    // Explicit Area overrides value in the dictionary.
                    { "Area", new Dictionary<string, string> { { "area", "Home" } }, "Area" },
                    // Empty string is also passed through to the helper.
                    { string.Empty, null, string.Empty },
                    { string.Empty, new Dictionary<string, string> { { "area", "Home" } }, string.Empty },
                    // Fall back "area" entry in the provided route values if Area is null.
                    { null, new Dictionary<string, string> { { "area", "Admin" } }, "Admin" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(AreaRouteValuesData))]
    public async Task ProcessAsync_CallsActionWithExpectedRouteValues(
        string area,
        Dictionary<string, string> routeValues,
        string expectedArea)
    {
        // Arrange
        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            "submit",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
            });

        var expectedRouteValues = new Dictionary<string, object> { { "area", expectedArea } };
        var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelper
            .Setup(mock => mock.Action(It.IsAny<UrlActionContext>()))
            .Callback<UrlActionContext>(param => Assert.Equal(expectedRouteValues, param.Values as RouteValueDictionary))
            .Returns("admin/dashboard/index");

        var viewContext = new ViewContext();
        var urlHelperFactory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(viewContext))
            .Returns(urlHelper.Object);

        var tagHelper = new FormActionTagHelper(urlHelperFactory.Object)
        {
            Action = "Index",
            Area = area,
            Controller = "Dashboard",
            RouteValues = routeValues,
            ViewContext = viewContext,
        };

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("submit", output.TagName);
        var attribute = Assert.Single(output.Attributes);
        Assert.Equal("formaction", attribute.Name);
        Assert.Equal("admin/dashboard/index", attribute.Value);
        Assert.Empty(output.Content.GetContent());
    }

    [Theory]
    [MemberData(nameof(AreaRouteValuesData))]
    public async Task ProcessAsync_CallsRouteUrlWithExpectedRouteValues(
        string area,
        Dictionary<string, string> routeValues,
        string expectedArea)
    {
        // Arrange
        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            "submit",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
            });

        var expectedRouteValues = new Dictionary<string, object> { { "area", expectedArea } };
        var urlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        urlHelper
            .Setup(mock => mock.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback<UrlRouteContext>(param => Assert.Equal(expectedRouteValues, param.Values as RouteValueDictionary))
            .Returns("admin/dashboard/index");

        var viewContext = new ViewContext();
        var urlHelperFactory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(viewContext))
            .Returns(urlHelper.Object);

        var tagHelper = new FormActionTagHelper(urlHelperFactory.Object)
        {
            Area = area,
            Route = "routine",
            RouteValues = routeValues,
            ViewContext = viewContext,
        };

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        Assert.Equal("submit", output.TagName);
        var attribute = Assert.Single(output.Attributes);
        Assert.Equal("formaction", attribute.Name);
        Assert.Equal("admin/dashboard/index", attribute.Value);
        Assert.Empty(output.Content.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_WithPageAndArea_CallsUrlHelperWithExpectedValues()
    {
        // Arrange
        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");
        var output = new TagHelperOutput(
            "submit",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) =>
            {
                return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
            });

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(mock => mock.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Callback<UrlRouteContext>(routeContext =>
            {
                var rvd = Assert.IsType<RouteValueDictionary>(routeContext.Values);
                Assert.Collection(
                    rvd.OrderBy(item => item.Key),
                    item =>
                    {
                        Assert.Equal("area", item.Key);
                        Assert.Equal("test-area", item.Value);
                    },
                    item =>
                    {
                        Assert.Equal("page", item.Key);
                        Assert.Equal("/my-page", item.Value);
                    });
            })
            .Returns("admin/dashboard/index")
            .Verifiable();

        var viewContext = new ViewContext
        {
            RouteData = new RouteData(),
        };

        urlHelper.SetupGet(h => h.ActionContext)
            .Returns(viewContext);
        var urlHelperFactory = new Mock<IUrlHelperFactory>(MockBehavior.Strict);
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(viewContext))
            .Returns(urlHelper.Object);

        var tagHelper = new FormActionTagHelper(urlHelperFactory.Object)
        {
            Area = "test-area",
            Page = "/my-page",
            ViewContext = viewContext,
        };

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        urlHelper.Verify();
    }

    [Theory]
    [InlineData("button", "Action")]
    [InlineData("button", "Controller")]
    [InlineData("button", "Route")]
    [InlineData("button", "asp-route-")]
    [InlineData("submit", "Action")]
    [InlineData("submit", "Controller")]
    [InlineData("submit", "Route")]
    [InlineData("submit", "asp-route-")]
    public async Task ProcessAsync_ThrowsIfFormActionConflictsWithBoundAttributes(string tagName, string propertyName)
    {
        // Arrange
        var urlHelperFactory = new Mock<IUrlHelperFactory>().Object;

        var tagHelper = new FormActionTagHelper(urlHelperFactory);

        var output = new TagHelperOutput(
            tagName,
            attributes: new TagHelperAttributeList
            {
                    { "formaction", "my-action" }
            },
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        if (propertyName == "asp-route-")
        {
            tagHelper.RouteValues.Add("name", "value");
        }
        else
        {
            typeof(FormActionTagHelper).GetProperty(propertyName).SetValue(tagHelper, "Home");
        }

        var expectedErrorMessage = $"Cannot override the 'formaction' attribute for <{tagName}>. <{tagName}> " +
            "elements with a specified 'formaction' must not have attributes starting with 'asp-route-' or an " +
            "'asp-action', 'asp-controller', 'asp-area', 'asp-fragment', 'asp-route', 'asp-page' or 'asp-page-handler' attribute.";

        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tagHelper.ProcessAsync(context, output));

        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Theory]
    [InlineData("button", "Action")]
    [InlineData("button", "Controller")]
    [InlineData("submit", "Action")]
    [InlineData("submit", "Controller")]
    public async Task ProcessAsync_ThrowsIfRouteAndActionOrControllerProvided(string tagName, string propertyName)
    {
        // Arrange
        var urlHelperFactory = new Mock<IUrlHelperFactory>().Object;

        var tagHelper = new FormActionTagHelper(urlHelperFactory)
        {
            Route = "Default",
        };

        typeof(FormActionTagHelper).GetProperty(propertyName).SetValue(tagHelper, "Home");
        var output = new TagHelperOutput(
            tagName,
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var expectedErrorMessage = string.Join(
            Environment.NewLine,
            $"Cannot determine the 'formaction' attribute for <{tagName}>. The following attributes are mutually exclusive:",
            "asp-route",
            "asp-controller, asp-action",
            "asp-page, asp-page-handler");

        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tagHelper.ProcessAsync(context, output));

        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Theory]
    [InlineData("button")]
    [InlineData("submit")]
    public async Task ProcessAsync_ThrowsIfRouteAndPageProvided(string tagName)
    {
        // Arrange
        var urlHelperFactory = new Mock<IUrlHelperFactory>().Object;

        var tagHelper = new FormActionTagHelper(urlHelperFactory)
        {
            Route = "Default",
            Page = "Page",
        };

        var output = new TagHelperOutput(
            tagName,
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var expectedErrorMessage = string.Join(
            Environment.NewLine,
            $"Cannot determine the 'formaction' attribute for <{tagName}>. The following attributes are mutually exclusive:",
            "asp-route",
            "asp-controller, asp-action",
            "asp-page, asp-page-handler");

        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tagHelper.ProcessAsync(context, output));

        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Theory]
    [InlineData("button")]
    [InlineData("submit")]
    public async Task ProcessAsync_ThrowsIfRouteAndPageHandlerProvided(string tagName)
    {
        // Arrange
        var urlHelperFactory = new Mock<IUrlHelperFactory>().Object;

        var tagHelper = new FormActionTagHelper(urlHelperFactory)
        {
            Route = "Default",
            PageHandler = "PageHandler",
        };

        var output = new TagHelperOutput(
            tagName,
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var expectedErrorMessage = string.Join(
            Environment.NewLine,
            $"Cannot determine the 'formaction' attribute for <{tagName}>. The following attributes are mutually exclusive:",
            "asp-route",
            "asp-controller, asp-action",
            "asp-page, asp-page-handler");

        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tagHelper.ProcessAsync(context, output));

        Assert.Equal(expectedErrorMessage, ex.Message);
    }

    [Theory]
    [InlineData("button")]
    [InlineData("submit")]
    public async Task ProcessAsync_ThrowsIfActionAndPageProvided(string tagName)
    {
        // Arrange
        var urlHelperFactory = new Mock<IUrlHelperFactory>().Object;

        var tagHelper = new FormActionTagHelper(urlHelperFactory)
        {
            Action = "Default",
            Page = "Page",
        };

        var output = new TagHelperOutput(
            tagName,
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(null));
        var expectedErrorMessage = string.Join(
            Environment.NewLine,
            $"Cannot determine the 'formaction' attribute for <{tagName}>. The following attributes are mutually exclusive:",
            "asp-route",
            "asp-controller, asp-action",
            "asp-page, asp-page-handler");

        var context = new TagHelperContext(
            tagName: "form-action",
            allAttributes: new TagHelperAttributeList(
                Enumerable.Empty<TagHelperAttribute>()),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tagHelper.ProcessAsync(context, output));

        Assert.Equal(expectedErrorMessage, ex.Message);
    }
}
