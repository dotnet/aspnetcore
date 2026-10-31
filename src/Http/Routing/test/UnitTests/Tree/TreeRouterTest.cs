// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing.Tree;

public class TreeRouterTest
{
    private static readonly RequestDelegate NullHandler = (c) => Task.CompletedTask;

    [Theory]
    [InlineData("template/5", "template/{parameter:int}")]
    [InlineData("template/5", "template/{parameter}")]
    [InlineData("template/5", "template/{*parameter:int}")]
    [InlineData("template/5", "template/{*parameter}")]
    [InlineData("template/{parameter}", "template/{parameter:alpha}")] // constraint doesn't match
    [InlineData("template/{parameter:int}", "template/{parameter}")]
    [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
    [InlineData("template/{parameter:int}", "template/{*parameter}")]
    [InlineData("template/{parameter}", "template/{*parameter:int}")]
    [InlineData("template/{parameter}", "template/{*parameter}")]
    [InlineData("template/{*parameter:int}", "template/{*parameter}")]
    public async Task TreeRouter_RouteAsync_RespectsPrecedence(
        string firstTemplate,
        string secondTemplate)
    {
        // Arrange
        var expectedRouteGroup = CreateRouteGroup(0, firstTemplate);

        var builder = CreateBuilder();

        // We setup the route entries in reverse order of precedence to ensure that when we
        // try to route the request, the route with a higher precedence gets tried first.
        MapInboundEntry(builder, secondTemplate);
        MapInboundEntry(builder, firstTemplate);

        var route = builder.Build();

        var context = CreateRouteContext("/template/5");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
    }

    [Theory]
    [InlineData("/", "")]
    [InlineData("/Literal1", "Literal1")]
    [InlineData("/Literal1/Literal2", "Literal1/Literal2")]
    [InlineData("/Literal1/Literal2/Literal3", "Literal1/Literal2/Literal3")]
    [InlineData("/Literal1/Literal2/Literal3/4", "Literal1/Literal2/Literal3/{*constrainedCatchAll:int}")]
    [InlineData("/Literal1/Literal2/Literal3/Literal4", "Literal1/Literal2/Literal3/{*catchAll}")]
    [InlineData("/1", "{constrained1:int}")]
    [InlineData("/1/2", "{constrained1:int}/{constrained2:int}")]
    [InlineData("/1/2/3", "{constrained1:int}/{constrained2:int}/{constrained3:int}")]
    [InlineData("/1/2/3/4", "{constrained1:int}/{constrained2:int}/{constrained3:int}/{*constrainedCatchAll:int}")]
    [InlineData("/1/2/3/CatchAll4", "{constrained1:int}/{constrained2:int}/{constrained3:int}/{*catchAll}")]
    [InlineData("/parameter1", "{parameter1}")]
    [InlineData("/parameter1/parameter2", "{parameter1}/{parameter2}")]
    [InlineData("/parameter1/parameter2/parameter3", "{parameter1}/{parameter2}/{parameter3}")]
    [InlineData("/parameter1/parameter2/parameter3/4", "{parameter1}/{parameter2}/{parameter3}/{*constrainedCatchAll:int}")]
    [InlineData("/parameter1/parameter2/parameter3/CatchAll4", "{parameter1}/{parameter2}/{parameter3}/{*catchAll}")]
    public async Task TreeRouter_RouteAsync_MatchesRouteWithTheRightLength(string url, string expected)
    {
        // Arrange
        var routes = new[] {
                "",
                "Literal1",
                "Literal1/Literal2",
                "Literal1/Literal2/Literal3",
                "Literal1/Literal2/Literal3/{*constrainedCatchAll:int}",
                "Literal1/Literal2/Literal3/{*catchAll}",
                "{constrained1:int}",
                "{constrained1:int}/{constrained2:int}",
                "{constrained1:int}/{constrained2:int}/{constrained3:int}",
                "{constrained1:int}/{constrained2:int}/{constrained3:int}/{*constrainedCatchAll:int}",
                "{constrained1:int}/{constrained2:int}/{constrained3:int}/{*catchAll}",
                "{parameter1}",
                "{parameter1}/{parameter2}",
                "{parameter1}/{parameter2}/{parameter3}",
                "{parameter1}/{parameter2}/{parameter3}/{*constrainedCatchAll:int}",
                "{parameter1}/{parameter2}/{parameter3}/{*catchAll}",
            };

        var expectedRouteGroup = CreateRouteGroup(0, expected);

        var builder = CreateBuilder();

        // We setup the route entries in reverse order of precedence to ensure that when we
        // try to route the request, the route with a higher precedence gets tried first.
        foreach (var template in Enumerable.Reverse(routes))
        {
            MapInboundEntry(builder, template);
        }

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
    }

    public static TheoryData<string, object[]> MatchesRoutesWithDefaultsData =>
        new TheoryData<string, object[]>
        {
                { "/", new object[] { "1", "2", "3", "4" } },
                { "/a", new object[] { "a", "2", "3", "4" } },
                { "/a/b", new object[] { "a", "b", "3", "4" } },
                { "/a/b/c", new object[] { "a", "b", "c", "4" } },
                { "/a/b/c/d", new object[] { "a", "b", "c", "d" } }
        };

    [Theory]
    [MemberData(nameof(MatchesRoutesWithDefaultsData))]
    public async Task TreeRouter_RouteAsync_MatchesRoutesWithDefaults(string url, object[] routeValues)
    {
        // Arrange
        var routes = new[] {
                "{parameter1=1}/{parameter2=2}/{parameter3=3}/{parameter4=4}",
            };

        var expectedRouteGroup = CreateRouteGroup(0, "{parameter1=1}/{parameter2=2}/{parameter3=3}/{parameter4=4}");
        var routeValueKeys = new[] { "parameter1", "parameter2", "parameter3", "parameter4" };
        var expectedRouteValues = new RouteValueDictionary();
        for (var i = 0; i < routeValueKeys.Length; i++)
        {
            expectedRouteValues.Add(routeValueKeys[i], routeValues[i]);
        }

        var builder = CreateBuilder();

        // We setup the route entries in reverse order of precedence to ensure that when we
        // try to route the request, the route with a higher precedence gets tried first.
        foreach (var template in Enumerable.Reverse(routes))
        {
            MapInboundEntry(builder, template);
        }

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
        foreach (var entry in expectedRouteValues)
        {
            var data = Assert.Single(context.RouteData.Values, v => v.Key == entry.Key);
            Assert.Equal(entry.Value, data.Value);
        }
    }

    public static TheoryData<string, object[]> MatchesConstrainedRoutesWithDefaultsData =>
        new TheoryData<string, object[]>
        {
                { "/", new object[] { "1", "2", "3", "4" } },
                { "/10", new object[] { "10", "2", "3", "4" } },
                { "/10/11", new object[] { "10", "11", "3", "4" } },
                { "/10/11/12", new object[] { "10", "11", "12", "4" } },
                { "/10/11/12/13", new object[] { "10", "11", "12", "13" } }
        };

    [Theory]
    [MemberData(nameof(MatchesConstrainedRoutesWithDefaultsData))]
    public async Task TreeRouter_RouteAsync_MatchesConstrainedRoutesWithDefaults(string url, object[] routeValues)
    {
        // Arrange
        var routes = new[] {
                "{parameter1:int=1}/{parameter2:int=2}/{parameter3:int=3}/{parameter4:int=4}",
            };

        var expectedRouteGroup = CreateRouteGroup(0, "{parameter1:int=1}/{parameter2:int=2}/{parameter3:int=3}/{parameter4:int=4}");
        var routeValueKeys = new[] { "parameter1", "parameter2", "parameter3", "parameter4" };
        var expectedRouteValues = new RouteValueDictionary();
        for (var i = 0; i < routeValueKeys.Length; i++)
        {
            expectedRouteValues.Add(routeValueKeys[i], routeValues[i]);
        }

        var builder = CreateBuilder();

        // We setup the route entries in reverse order of precedence to ensure that when we
        // try to route the request, the route with a higher precedence gets tried first.
        foreach (var template in Enumerable.Reverse(routes))
        {
            MapInboundEntry(builder, template);
        }

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
        foreach (var entry in expectedRouteValues)
        {
            var data = Assert.Single(context.RouteData.Values, v => v.Key == entry.Key);
            Assert.Equal(entry.Value, data.Value);
        }
    }

    [Fact]
    public async Task TreeRouter_RouteAsync_MatchesCatchAllRoutesWithDefaults()
    {
        // Arrange
        var routes = new[] {
                "{parameter1=1}/{parameter2=2}/{parameter3=3}/{*parameter4=4}",
            };
        var url = "/a/b/c";
        var routeValues = new[] { "a", "b", "c", "4" };

        var expectedRouteGroup = CreateRouteGroup(0, "{parameter1=1}/{parameter2=2}/{parameter3=3}/{*parameter4=4}");
        var routeValueKeys = new[] { "parameter1", "parameter2", "parameter3", "parameter4" };
        var expectedRouteValues = new RouteValueDictionary();
        for (var i = 0; i < routeValueKeys.Length; i++)
        {
            expectedRouteValues.Add(routeValueKeys[i], routeValues[i]);
        }

        var builder = CreateBuilder();

        // We setup the route entries in reverse order of precedence to ensure that when we
        // try to route the request, the route with a higher precedence gets tried first.
        foreach (var template in Enumerable.Reverse(routes))
        {
            MapInboundEntry(builder, template);
        }

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
        foreach (var entry in expectedRouteValues)
        {
            var data = Assert.Single(context.RouteData.Values, v => v.Key == entry.Key);
            Assert.Equal(entry.Value, data.Value);
        }
    }

    [Fact]
    public async Task TreeRouter_RouteAsync_DoesNotMatchRoutesWithIntermediateDefaultRouteValues()
    {
        // Arrange
        var url = "/a/b";

        var builder = CreateBuilder();

        MapInboundEntry(builder, "a/b/{parameter3=3}/d");

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a")]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b")]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c")]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d")]
    public async Task TreeRouter_RouteAsync_DoesNotMatchRoutesWithMultipleIntermediateDefaultOrOptionalRouteValues(string template, string url)
    {
        // Arrange
        var builder = CreateBuilder();

        MapInboundEntry(builder, template);

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d/e")]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d/e/f")]
    public async Task RouteAsync_MatchRoutesWithMultipleIntermediateDefaultOrOptionalRouteValues_WhenAllIntermediateValuesAreProvided(string template, string url)
    {
        // Arrange
        var builder = CreateBuilder();

        MapInboundEntry(builder, template);

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(context.Handler);
    }

    [Fact]
    public async Task TreeRouter_RouteAsync_DoesNotMatchShorterUrl()
    {
        // Arrange
        var routes = new[] {
                "Literal1/Literal2/Literal3",
            };

        var builder = CreateBuilder();

        // We setup the route entries in reverse order of precedence to ensure that when we
        // try to route the request, the route with a higher precedence gets tried first.
        foreach (var template in Enumerable.Reverse(routes))
        {
            MapInboundEntry(builder, template);
        }

        var route = builder.Build();

        var context = CreateRouteContext("/Literal1");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("template/5", "template/{parameter:int}")]
    [InlineData("template/5", "template/{parameter}")]
    [InlineData("template/5", "template/{*parameter:int}")]
    [InlineData("template/5", "template/{*parameter}")]
    [InlineData("template/{parameter:int}", "template/{parameter}")]
    [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
    [InlineData("template/{parameter:int}", "template/{*parameter}")]
    [InlineData("template/{parameter}", "template/{*parameter:int}")]
    [InlineData("template/{parameter}", "template/{*parameter}")]
    [InlineData("template/{*parameter:int}", "template/{*parameter}")]
    public async Task TreeRouter_RouteAsync_RespectsOrderOverPrecedence(
        string firstTemplate,
        string secondTemplate)
    {
        // Arrange
        var expectedRouteGroup = CreateRouteGroup(0, secondTemplate);

        var builder = CreateBuilder();

        // We setup the route entries with a lower relative order and higher relative precedence
        // first to ensure that when we try to route the request, the route with the higher
        // relative order gets tried first.
        MapInboundEntry(builder, firstTemplate, order: 1);
        MapInboundEntry(builder, secondTemplate, order: 0);

        var route = builder.Build();

        var context = CreateRouteContext("/template/5");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
    }

    [Theory]
    [InlineData("///")]
    [InlineData("/a//")]
    [InlineData("/a/b//")]
    [InlineData("//b//")]
    [InlineData("///c")]
    [InlineData("///c/")]
    public async Task TryMatch_MultipleOptionalParameters_WithEmptyIntermediateSegmentsDoesNotMatch(string url)
    {
        // Arrange
        var builder = CreateBuilder();

        MapInboundEntry(builder, "{controller?}/{action?}/{id?}");

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/a")]
    [InlineData("/a/")]
    [InlineData("/a/b")]
    [InlineData("/a/b/")]
    [InlineData("/a/b/c")]
    [InlineData("/a/b/c/")]
    public async Task TryMatch_MultipleOptionalParameters_WithIncrementalOptionalValues(string url)
    {
        // Arrange
        var builder = CreateBuilder();

        MapInboundEntry(builder, "{controller?}/{action?}/{id?}");

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(context.Handler);
    }

    [Theory]
    [InlineData("///")]
    [InlineData("////")]
    [InlineData("/a//")]
    [InlineData("/a///")]
    [InlineData("//b/")]
    [InlineData("//b//")]
    [InlineData("///c")]
    [InlineData("///c/")]
    public async Task TryMatch_MultipleParameters_WithEmptyValues(string url)
    {
        // Arrange
        var builder = CreateBuilder();

        MapInboundEntry(builder, "{controller}/{action}/{id}");

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("/a/b/c//")]
    [InlineData("/a/b/c/////")]
    public async Task TryMatch_CatchAllParameters_WithEmptyValuesAtTheEnd(string url)
    {
        // Arrange
        var builder = CreateBuilder();

        MapInboundEntry(builder, "{controller}/{action}/{*id}");

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(context.Handler);
    }

    [Theory]
    [InlineData("/a/b//")]
    [InlineData("/a/b///c")]
    public async Task TryMatch_CatchAllParameters_WithEmptyValues(string url)
    {
        // Arrange
        var builder = CreateBuilder();

        MapInboundEntry(builder, "{controller}/{action}/{*id}");

        var route = builder.Build();

        var context = CreateRouteContext(url);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("{*path}", "/a", "a")]
    [InlineData("{*path}", "/a/b/c", "a/b/c")]
    [InlineData("a/{*path}", "/a/b", "b")]
    [InlineData("a/{*path}", "/a/b/c/d", "b/c/d")]
    [InlineData("a/{*path:regex(10/20/30)}", "/a/10/20/30", "10/20/30")]
    public async Task TreeRouter_RouteAsync_MatchesWildCard_ForLargerPathSegments(
        string template,
        string requestPath,
        string expectedResult)
    {
        // Arrange
        var builder = CreateBuilder();
        MapInboundEntry(builder, template);
        var route = builder.Build();

        var context = CreateRouteContext(requestPath);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Equal(expectedResult, context.RouteData.Values["path"]);
    }

    [Theory]
    [InlineData("a/{*path}", "/a")]
    [InlineData("a/{*path}", "/a/")]
    public async Task TreeRouter_RouteAsync_MatchesCatchAll_NullValue(
        string template,
        string requestPath)
    {
        // Arrange
        var builder = CreateBuilder();
        MapInboundEntry(builder, template);
        var route = builder.Build();

        var context = CreateRouteContext(requestPath);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Null(context.RouteData.Values["path"]);
    }

    [Theory]
    [InlineData("a/{*path}", "/a")]
    [InlineData("a/{*path}", "/a/")]
    public async Task TreeRouter_RouteAsync_MatchesCatchAll_NullValue_DoesNotReplaceExistingValue(
        string template,
        string requestPath)
    {
        // Arrange
        var builder = CreateBuilder();
        MapInboundEntry(builder, template);
        var route = builder.Build();

        var context = CreateRouteContext(requestPath);
        context.RouteData.Values["path"] = "existing-value";

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Equal("existing-value", context.RouteData.Values["path"]);
    }

    [Theory]
    [InlineData("a/{*path=default}", "/a")]
    [InlineData("a/{*path=default}", "/a/")]
    public async Task TreeRouter_RouteAsync_MatchesCatchAll_UsesDefaultValue(
        string template,
        string requestPath)
    {
        // Arrange
        var builder = CreateBuilder();
        MapInboundEntry(builder, template);
        var route = builder.Build();

        var context = CreateRouteContext(requestPath);
        context.RouteData.Values["path"] = "existing-value";

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(context.Handler);
        Assert.Equal("default", context.RouteData.Values["path"]);
    }

    [Theory]
    [InlineData("template/5")]
    [InlineData("template/{parameter:int}")]
    [InlineData("template/{parameter}")]
    [InlineData("template/{*parameter:int}")]
    [InlineData("template/{*parameter}")]
    public async Task TreeRouter_RouteAsync_RespectsOrder(string template)
    {
        // Arrange
        var expectedRouteGroup = CreateRouteGroup(0, template);

        var builder = CreateBuilder();

        // We setup the route entries with a lower relative order first to ensure that when
        // we try to route the request, the route with the higher relative order gets tried first.
        MapInboundEntry(builder, template, order: 1);
        MapInboundEntry(builder, template, order: 0);

        var route = builder.Build();

        var context = CreateRouteContext("/template/5");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
    }

    [Theory]
    [InlineData("template/{first:int}", "template/{second:int}")]
    [InlineData("template/{first}", "template/{second}")]
    [InlineData("template/{*first:int}", "template/{*second:int}")]
    [InlineData("template/{*first}", "template/{*second}")]
    public async Task TreeRouter_RouteAsync_EnsuresStableOrdering(string first, string second)
    {
        // Arrange
        var expectedRouteGroup = CreateRouteGroup(0, first);

        var builder = CreateBuilder();

        // We setup the route entries with a lower relative template order first to ensure that when
        // we try to route the request, the route with the higher template order gets tried first.
        MapInboundEntry(builder, first);
        MapInboundEntry(builder, second);

        var route = builder.Build();

        var context = CreateRouteContext("/template/5");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
    }

    [Theory]
    [InlineData("template/{parameter:int}", "/template/5", true)]
    [InlineData("template/{parameter:int?}", "/template/5", true)]
    [InlineData("template/{parameter:int?}", "/template", true)]
    [InlineData("template/{parameter:int?}", "/template/qwer", false)]
    public async Task TreeRouter_WithOptionalInlineConstraint(
        string template,
        string request,
        bool expectedResult)
    {
        // Arrange
        var expectedRouteGroup = CreateRouteGroup(0, template);

        var builder = CreateBuilder();
        MapInboundEntry(builder, template);
        var route = builder.Build();

        var context = CreateRouteContext(request);

        // Act
        await route.RouteAsync(context);

        // Assert
        if (expectedResult)
        {
            Assert.NotNull(context.Handler);
            Assert.Equal(expectedRouteGroup, context.RouteData.Values["test_route_group"]);
        }
        else
        {
            Assert.Null(context.Handler);
        }
    }

    [Theory]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo.bar", "foo", "bar", null)]
    [InlineData("moo/{p1?}", "/moo/foo", "foo", null, null)]
    [InlineData("moo/{p1?}", "/moo", null, null, null)]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo", "foo", null, null)]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo..bar", "foo.", "bar", null)]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo.moo.bar", "foo.moo", "bar", null)]
    [InlineData("moo/{p1}.{p2}", "/moo/foo.bar", "foo", "bar", null)]
    [InlineData("moo/foo.{p1}.{p2?}", "/moo/foo.moo.bar", "moo", "bar", null)]
    [InlineData("moo/foo.{p1}.{p2?}", "/moo/foo.moo", "moo", null, null)]
    [InlineData("moo/.{p2?}", "/moo/.foo", null, "foo", null)]
    [InlineData("moo/{p1}.{p2?}", "/moo/....", "..", ".", null)]
    [InlineData("moo/{p1}.{p2?}", "/moo/.bar", ".bar", null, null)]
    [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo.bar", "foo", "moo", "bar")]
    [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo", "foo", "moo", null)]
    [InlineData("moo/{p1}.{p2}.{p3}.{p4?}", "/moo/foo.moo.bar", "foo", "moo", "bar")]
    [InlineData("{p1}.{p2?}/{p3}", "/foo.moo/bar", "foo", "moo", "bar")]
    [InlineData("{p1}.{p2?}/{p3}", "/foo/bar", "foo", null, "bar")]
    [InlineData("{p1}.{p2?}/{p3}", "/.foo/bar", ".foo", null, "bar")]
    public async Task TreeRouter_WithOptionalCompositeParameter_Valid(
        string template,
        string request,
        string p1,
        string p2,
        string p3)
    {
        // Arrange
        var expectedRouteGroup = CreateRouteGroup(0, template);

        var builder = CreateBuilder();
        MapInboundEntry(builder, template);
        var route = builder.Build();

        var context = CreateRouteContext(request);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(context.Handler);
        if (p1 != null)
        {
            Assert.Equal(p1, context.RouteData.Values["p1"]);
        }
        if (p2 != null)
        {
            Assert.Equal(p2, context.RouteData.Values["p2"]);
        }
        if (p3 != null)
        {
            Assert.Equal(p3, context.RouteData.Values["p3"]);
        }
    }

    [Theory]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo.")]
    [InlineData("moo/{p1}.{p2?}", "/moo/.")]
    [InlineData("moo/{p1}.{p2}", "/foo.")]
    [InlineData("moo/{p1}.{p2}", "/foo")]
    [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo.")]
    [InlineData("moo/foo.{p2}.{p3?}", "/moo/bar.foo.moo")]
    [InlineData("moo/foo.{p2}.{p3?}", "/moo/kungfoo.moo.bar")]
    [InlineData("moo/foo.{p2}.{p3?}", "/moo/kungfoo.moo")]
    [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo")]
    [InlineData("{p1}.{p2?}/{p3}", "/foo./bar")]
    [InlineData("moo/.{p2?}", "/moo/.")]
    [InlineData("{p1}.{p2}/{p3}", "/.foo/bar")]
    public async Task TreeRouter_WithOptionalCompositeParameter_Invalid(
        string template,
        string request)
    {
        // Arrange
        var expectedRouteGroup = CreateRouteGroup(0, template);

        var builder = CreateBuilder();
        MapInboundEntry(builder, template);
        var route = builder.Build();

        var context = CreateRouteContext(request);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Null(context.Handler);
    }

    [Theory]
    [InlineData("template", "{*url:alpha}", "/template?url=dingo&id=5")]
    [InlineData("{*url:alpha}", "{*url}", "/dingo?id=5")]
    [InlineData("{id}", "{*url}", "/5?url=dingo")]
    [InlineData("{id}", "{*url:alpha}", "/5?url=dingo")]
    [InlineData("{id:int}", "{id}", "/5?url=dingo")]
    [InlineData("{id}", "{id:alpha}/{url}", "/5?url=dingo")] // constraint doesn't match
    [InlineData("template/api/{*url}", "template/api", "/template/api/dingo?id=5")]
    [InlineData("template/api", "template/{*url}", "/template/api?url=dingo&id=5")]
    [InlineData("template/api", "template/api{id}location", "/template/api?url=dingo&id=5")]
    [InlineData("template/api{id}location", "template/{id:int}", "/template/api5location?url=dingo")]
    public void TreeRouter_GenerateLink(string firstTemplate, string secondTemplate, string expectedPath)
    {
        // Arrange
        var values = new Dictionary<string, object>
            {
                {"url", "dingo" },
                {"id", 5 }
            };

        var route = CreateTreeRouter(firstTemplate, secondTemplate);
        var context = CreateVirtualPathContext(
            values: values,
            ambientValues: null);

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPath, result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_LongerTemplateWithDefaultIsMoreSpecific()
    {
        // Arrange
        var firstTemplate = "template";
        var secondTemplate = "template/{parameter:int=1003}";

        var route = CreateTreeRouter(firstTemplate, secondTemplate);
        var context = CreateVirtualPathContext(
            values: null,
            ambientValues: null);

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        // The Binder binds to /template
        Assert.Equal("/template", result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Theory]
    [InlineData("template/{parameter:int=5}", "template", "/template/5")]
    [InlineData("template/{parameter}", "template", "/template/5")]
    [InlineData("template/{parameter}/{id}", "template/{parameter}", "/template/5/1234")]
    public void TreeRouter_GenerateLink_OrderingAgnostic(
        string firstTemplate,
        string secondTemplate,
        string expectedPath)
    {
        // Arrange
        var route = CreateTreeRouter(firstTemplate, secondTemplate);
        var parameter = 5;
        var id = 1234;
        var values = new Dictionary<string, object>
            {
                { nameof(parameter) , parameter},
                { nameof(id), id }
            };
        var context = CreateVirtualPathContext(
            values: null,
            ambientValues: values);

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPath, result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Theory]
    [InlineData("template", "template/{parameter}", "/template/5")]
    [InlineData("template/{parameter}", "template/{parameter}/{id}", "/template/5/1234")]
    [InlineData("template", "template/{parameter:int=5}", "/template/5")]
    public void TreeRouter_GenerateLink_UseAvailableVariables(
        string firstTemplate,
        string secondTemplate,
        string expectedPath)
    {
        // Arrange
        var route = CreateTreeRouter(firstTemplate, secondTemplate);
        var parameter = 5;
        var id = 1234;
        var values = new Dictionary<string, object>
            {
                { nameof(parameter) , parameter},
                { nameof(id), id }
            };
        var context = CreateVirtualPathContext(
            values: null,
            ambientValues: values);

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPath, result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Theory]
    [InlineData("template/5", "template/{parameter:int}")]
    [InlineData("template/5", "template/{parameter}")]
    [InlineData("template/5", "template/{*parameter:int}")]
    [InlineData("template/5", "template/{*parameter}")]
    [InlineData("template/{parameter:int}", "template/{parameter}")]
    [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
    [InlineData("template/{parameter:int}", "template/{*parameter}")]
    [InlineData("template/{parameter}", "template/{*parameter:int}")]
    [InlineData("template/{parameter}", "template/{*parameter}")]
    [InlineData("template/{*parameter:int}", "template/{*parameter}")]
    public void TreeRouter_GenerateLink_RespectsPrecedence(string firstTemplate, string secondTemplate)
    {
        // Arrange
        var builder = CreateBuilder();

        // We setup the route entries in reverse order of precedence to ensure that when we
        // try to generate a link, the route with a higher precedence gets tried first.
        MapOutboundEntry(builder, secondTemplate);
        MapOutboundEntry(builder, firstTemplate);

        var route = builder.Build();

        var context = CreateVirtualPathContext(values: null, ambientValues: new { parameter = 5 });

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/template/5", result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Theory]
    [InlineData("template/{parameter:int}", "/template/5", 5)]
    [InlineData("template/{parameter:int?}", "/template/5", 5)]
    [InlineData("template/{parameter:int?}", "/template", null)]
    [InlineData("template/{parameter:int?}", null, "asdf")]
    [InlineData("template/{parameter:alpha?}", "/template/asdf", "asdf")]
    [InlineData("template/{parameter:alpha?}", "/template", null)]
    [InlineData("template/{parameter:int:range(1,20)?}", "/template", null)]
    [InlineData("template/{parameter:int:range(1,20)?}", "/template/5", 5)]
    [InlineData("template/{parameter:int:range(1,20)?}", null, 21)]
    public void TreeRouter_GenerateLink_OptionalInlineParameter(
        string template,
        string expectedPath,
        object parameter)
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, template);
        var route = builder.Build();

        VirtualPathContext context;
        if (parameter != null)
        {
            context = CreateVirtualPathContext(values: null, ambientValues: new { parameter = parameter });
        }
        else
        {
            context = CreateVirtualPathContext(values: null, ambientValues: null);
        }

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        if (expectedPath == null)
        {
            Assert.Null(result);
        }
        else
        {
            Assert.NotNull(result);
            Assert.Equal(expectedPath, result.VirtualPath);
            Assert.Same(route, result.Router);
            Assert.Empty(result.DataTokens);
        }
    }

    [Theory]
    [InlineData("template/5", "template/{parameter:int}")]
    [InlineData("template/5", "template/{parameter}")]
    [InlineData("template/5", "template/{*parameter:int}")]
    [InlineData("template/5", "template/{*parameter}")]
    [InlineData("template/{parameter:int}", "template/{parameter}")]
    [InlineData("template/{parameter:int}", "template/{*parameter:int}")]
    [InlineData("template/{parameter:int}", "template/{*parameter}")]
    [InlineData("template/{parameter}", "template/{*parameter:int}")]
    [InlineData("template/{parameter}", "template/{*parameter}")]
    [InlineData("template/{*parameter:int}", "template/{*parameter}")]
    public void TreeRouter_GenerateLink_RespectsOrderOverPrecedence(string firstTemplate, string secondTemplate)
    {
        // Arrange
        var builder = CreateBuilder();

        // We setup the route entries with a lower relative order and higher relative precedence
        // first to ensure that when we try to generate a link, the route with the higher
        // relative order gets tried first.
        MapOutboundEntry(builder, firstTemplate, order: 1);
        MapOutboundEntry(builder, secondTemplate, order: 0);

        var route = builder.Build();

        var context = CreateVirtualPathContext(null, ambientValues: new { parameter = 5 });

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/template/5", result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Theory]
    [InlineData("template/5", "template/5")]
    [InlineData("template/{first:int}", "template/{second:int}")]
    [InlineData("template/{first}", "template/{second}")]
    [InlineData("template/{*first:int}", "template/{*second:int}")]
    [InlineData("template/{*first}", "template/{*second}")]
    public void TreeRouter_GenerateLink_RespectsOrder(string firstTemplate, string secondTemplate)
    {
        // Arrange
        var builder = CreateBuilder();

        // We setup the route entries with a lower relative order first to ensure that when
        // we try to generate a link, the route with the higher relative order gets tried first.
        MapOutboundEntry(builder, firstTemplate, requiredValues: null, order: 1);
        MapOutboundEntry(builder, secondTemplate, requiredValues: null, order: 0);

        var route = builder.Build();

        var context = CreateVirtualPathContext(values: null, ambientValues: new { first = 5, second = 5 });

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/template/5", result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Theory]
    [InlineData("first/5", "second/5")]
    [InlineData("first/{first:int}", "second/{second:int}")]
    [InlineData("first/{first}", "second/{second}")]
    [InlineData("first/{*first:int}", "second/{*second:int}")]
    [InlineData("first/{*first}", "second/{*second}")]
    public void TreeRouter_GenerateLink_EnsuresStableOrder(string firstTemplate, string secondTemplate)
    {
        // Arrange
        var builder = CreateBuilder();

        // We setup the route entries with a lower relative template order first to ensure that when
        // we try to generate a link, the route with the higher template order gets tried first.
        MapOutboundEntry(builder, secondTemplate, requiredValues: null, order: 0);
        MapOutboundEntry(builder, firstTemplate, requiredValues: null, order: 0);

        var route = builder.Build();

        var context = CreateVirtualPathContext(values: null, ambientValues: new { first = 5, second = 5 });

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/first/5", result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_CreatesLinksForRoutesWithIntermediateDefaultRouteValues()
    {
        // Arrange
        var builder = CreateBuilder();

        MapOutboundEntry(builder, template: "a/b/{parameter3=3}/d", requiredValues: null, order: 0);

        var route = builder.Build();

        var context = CreateVirtualPathContext(values: null, ambientValues: null);

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/a/b/3/d", result.VirtualPath);
    }

    [Fact]
    public void TreeRouter_GeneratesLink_ForMultipleNamedEntriesWithTheSameTemplate()
    {
        // Arrange
        var builder = CreateBuilder();

        MapOutboundEntry(builder, "Template", name: "NamedEntry", order: 1);
        MapOutboundEntry(builder, "TEMPLATE", name: "NamedEntry", order: 2);

        // Act & Assert (does not throw)
        builder.Build();
    }

    [Fact]
    public void TreeRouter_GenerateLink_WithName()
    {
        // Arrange
        var builder = CreateBuilder();

        // The named route has a lower order which will ensure that we aren't trying the route as
        // if it were an unnamed route.
        MapOutboundEntry(builder, "named", requiredValues: null, order: 1, name: "NamedRoute");
        MapOutboundEntry(builder, "unnamed", requiredValues: null, order: 0);

        var route = builder.Build();

        var context = CreateVirtualPathContext(values: null, ambientValues: null, name: "NamedRoute");

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/named", result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Fact]
    public void TreeRouter_DoesNotGenerateLink_IfThereIsNoRouteForAGivenName()
    {
        // Arrange
        var builder = CreateBuilder();

        // The named route has a lower order which will ensure that we aren't trying the route as
        // if it were an unnamed route.
        MapOutboundEntry(builder, "named", requiredValues: null, order: 1, name: "NamedRoute");

        // Add an unnamed entry to ensure we don't fall back to generating a link for an unnamed route.
        MapOutboundEntry(builder, "unnamed", requiredValues: null, order: 0);

        var route = builder.Build();

        var context = CreateVirtualPathContext(values: null, ambientValues: null, name: "NonExistingNamedRoute");

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("template/{parameter:int}", null)]
    [InlineData("template/{parameter:int}", "NaN")]
    [InlineData("template/{parameter}", null)]
    [InlineData("template/{*parameter:int}", null)]
    [InlineData("template/{*parameter:int}", "NaN")]
    public void TreeRouter_DoesNotGenerateLink_IfValuesDoNotMatchNamedEntry(string template, string value)
    {
        // Arrange
        var builder = CreateBuilder();

        // The named route has a lower order which will ensure that we aren't trying the route as
        // if it were an unnamed route.
        MapOutboundEntry(builder, template, requiredValues: null, order: 1, name: "NamedRoute");

        // Add an unnamed entry to ensure we don't fall back to generating a link for an unnamed route.
        MapOutboundEntry(builder, "unnamed", requiredValues: null, order: 0);

        var route = builder.Build();

        var ambientValues = value == null ? null : new { parameter = value };
        var context = CreateVirtualPathContext(values: null, ambientValues: ambientValues, name: "NamedRoute");

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("template/{parameter:int}", "5")]
    [InlineData("template/{parameter}", "5")]
    [InlineData("template/{*parameter:int}", "5")]
    [InlineData("template/{*parameter}", "5")]
    public void TreeRouter_GeneratesLink_IfValuesMatchNamedEntry(string template, string value)
    {
        // Arrange
        var builder = CreateBuilder();

        // The named route has a lower order which will ensure that we aren't trying the route as
        // if it were an unnamed route.
        MapOutboundEntry(builder, template, requiredValues: null, order: 1, name: "NamedRoute");

        // Add an unnamed entry to ensure we don't fall back to generating a link for an unnamed route.
        MapOutboundEntry(builder, "unnamed", requiredValues: null, order: 0);

        var route = builder.Build();

        var ambientValues = value == null ? null : new { parameter = value };
        var context = CreateVirtualPathContext(values: null, ambientValues: ambientValues, name: "NamedRoute");

        // Act
        var result = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/template/5", result.VirtualPath);
        Assert.Same(route, result.Router);
        Assert.Empty(result.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_NoRequiredValues()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store", new { });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store", new { action = "Index", controller = "Store" });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_NoMatch()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store", new { action = "Details", controller = "Store" });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

        // Act
        var path = route.GetVirtualPath(context);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match_WithAmbientValues()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store", new { action = "Index", controller = "Store" });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { }, new { action = "Index", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match_HasTwoOptionalParametersWithoutValues()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "Customers/SeparatePageModels/{handler?}/{id?}", new { page = "/Customers/SeparatePageModels/Index" });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { page = "/Customers/SeparatePageModels/Index" }, new { page = "/Customers/SeparatePageModels/Edit", id = "17" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Customers/SeparatePageModels", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match_WithParameters()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store/{action}", new { action = "Index", controller = "Store" });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/Store/Index", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match_WithMoreParameters()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder,
            "api/{area}/dosomething/{controller}/{action}",
            new { action = "Index", controller = "Store", area = "AwesomeCo" });

        var route = builder.Build();

        var context = CreateVirtualPathContext(
            new { action = "Index", controller = "Store" },
            new { area = "AwesomeCo" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/AwesomeCo/dosomething/Store/Index", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match_WithDefault()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store/{action=Index}", new { action = "Index", controller = "Store" });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { action = "Index", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match_WithConstraint()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store/{action}/{id:int}", new { action = "Index", controller = "Store" });

        var route = builder.Build();

        var context = CreateVirtualPathContext(new { action = "Index", controller = "Store", id = 5 });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/Store/Index/5", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_NoMatch_WithConstraint()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store/{action}/{id:int}", new { action = "Index", controller = "Store" });
        var route = builder.Build();

        var next = new StubRouter();
        var context = CreateVirtualPathContext(new { action = "Index", controller = "Store", id = "heyyyy" });

        // Act
        var path = route.GetVirtualPath(context);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match_WithMixedAmbientValues()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store", new { action = "Index", controller = "Store" });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { action = "Index" }, new { controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_Match_WithQueryString()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store", new { action = "Index", controller = "Store" });
        var route = builder.Build();

        var context = CreateVirtualPathContext(new { action = "Index", id = 5 }, new { controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api/Store?id=5", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_RejectedByFirstRoute()
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, "api/Store", new { action = "Index", controller = "Store" });
        MapOutboundEntry(builder, "api2/{controller}", new { action = "Index", controller = "Blog" });

        var route = builder.Build();

        var context = CreateVirtualPathContext(new { action = "Index", controller = "Blog" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/api2/Blog", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_ToArea()
    {
        // Arrange
        var builder = CreateBuilder();
        var entry1 = MapOutboundEntry(builder, "Help/Store", new { area = "Help", action = "Edit", controller = "Store" });
        entry1.Precedence = 2;

        var entry2 = MapOutboundEntry(builder, "Store", new { area = (string)null, action = "Edit", controller = "Store" });
        entry2.Precedence = 1;

        var route = builder.Build();

        var context = CreateVirtualPathContext(new { area = "Help", action = "Edit", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Help/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_ToArea_PredecedenceReversed()
    {
        // Arrange
        var builder = CreateBuilder();
        var entry1 = MapOutboundEntry(builder, "Help/Store", new { area = "Help", action = "Edit", controller = "Store" });
        entry1.Precedence = 1;

        var entry2 = MapOutboundEntry(builder, "Store", new { area = (string)null, action = "Edit", controller = "Store" });
        entry2.Precedence = 2;

        var route = builder.Build();

        var context = CreateVirtualPathContext(new { area = "Help", action = "Edit", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Help/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_ToArea_WithAmbientValues()
    {
        // Arrange
        var builder = CreateBuilder();
        var entry1 = MapOutboundEntry(builder, "Help/Store", new { area = "Help", action = "Edit", controller = "Store" });
        entry1.Precedence = 2;

        var entry2 = MapOutboundEntry(builder, "Store", new { area = (string)null, action = "Edit", controller = "Store" });
        entry2.Precedence = 1;

        var route = builder.Build();

        var context = CreateVirtualPathContext(
            values: new { action = "Edit", controller = "Store" },
            ambientValues: new { area = "Help" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Help/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_OutOfArea_IgnoresAmbientValue()
    {
        // Arrange
        var builder = CreateBuilder();
        var entry1 = MapOutboundEntry(builder, "Help/Store", new { area = "Help", action = "Edit", controller = "Store" });
        entry1.Precedence = 2;

        var entry2 = MapOutboundEntry(builder, "Store", new { area = (string)null, action = "Edit", controller = "Store" });
        entry2.Precedence = 1;

        var route = builder.Build();

        var context = CreateVirtualPathContext(
            values: new { action = "Edit", controller = "Store" },
            ambientValues: new { area = "Blog" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    public static IEnumerable<object[]> OptionalParamValues
    {
        get
        {
            return new object[][]
            {
                    // defaults
                    // ambient values
                    // values
                    new object[]
                    {
                        "Test/{val1}/{val2}.{val3?}",
                        new {val1 = "someval1", val2 = "someval2", val3 = "someval3a"},
                        new {val3 = "someval3v"},
                        "/Test/someval1/someval2.someval3v",
                    },
                    new object[]
                    {
                        "Test/{val1}/{val2}.{val3?}",
                        new {val3 = "someval3a"},
                        new {val1 = "someval1", val2 = "someval2", val3 = "someval3v" },
                        "/Test/someval1/someval2.someval3v",
                    },
                    new object[]
                    {
                        "Test/{val1}/{val2}.{val3?}",
                        null,
                        new {val1 = "someval1", val2 = "someval2" },
                        "/Test/someval1/someval2",
                    },
                    new object[]
                    {
                        "Test/{val1}.{val2}.{val3}.{val4?}",
                        new {val1 = "someval1", val2 = "someval2" },
                        new {val4 = "someval4", val3 = "someval3" },
                        "/Test/someval1.someval2.someval3.someval4",
                    },
                    new object[]
                    {
                        "Test/{val1}.{val2}.{val3}.{val4?}",
                        new {val1 = "someval1", val2 = "someval2" },
                        new {val3 = "someval3" },
                        "/Test/someval1.someval2.someval3",
                    },
                    new object[]
                    {
                        "Test/.{val2?}",
                        null,
                        new {val2 = "someval2" },
                        "/Test/.someval2",
                    },
                    new object[]
                    {
                        "Test/.{val2?}",
                        null,
                        null,
                        "/Test/",
                    },
                    new object[]
                    {
                        "Test/{val1}.{val2}",
                        new {val1 = "someval1", val2 = "someval2" },
                        new {val3 = "someval3" },
                        "/Test/someval1.someval2?val3=someval3",
                    },
            };
        }
    }

    [Theory]
    [MemberData(nameof(OptionalParamValues))]
    public void TreeRouter_GenerateLink_Match_WithOptionalParameters(
        string template,
        object ambientValues,
        object values,
        string expected)
    {
        // Arrange
        var builder = CreateBuilder();
        MapOutboundEntry(builder, template);
        var route = builder.Build();

        var context = CreateVirtualPathContext(values, ambientValues);

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal(expected, pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public async Task TreeRouter_ReplacesExistingRouteValues_IfNotNull()
    {
        // Arrange
        var builder = CreateBuilder();
        MapInboundEntry(builder, "Foo/{*path}");
        var route = builder.Build();

        var context = CreateRouteContext("/Foo/Bar");

        var originalRouteData = context.RouteData;
        originalRouteData.Values.Add("path", "default");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal("Bar", context.RouteData.Values["path"]);
    }

    [Fact]
    public async Task TreeRouter_DoesNotReplaceExistingRouteValues_IfNull()
    {
        // Arrange
        var builder = CreateBuilder();
        MapInboundEntry(builder, "Foo/{*path}");
        var route = builder.Build();

        var context = CreateRouteContext("/Foo/");

        var originalRouteData = context.RouteData;
        originalRouteData.Values.Add("path", "default");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal("default", context.RouteData.Values["path"]);
    }

    [Fact]
    public async Task TreeRouter_SnapshotsRouteData()
    {
        // Arrange
        RouteValueDictionary nestedValues = null;
        List<IRouter> nestedRouters = null;

        var next = new Mock<IRouter>();
        next
            .Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
            .Callback<RouteContext>(c =>
            {
                nestedValues = new RouteValueDictionary(c.RouteData.Values);
                nestedRouters = new List<IRouter>(c.RouteData.Routers);
                c.Handler = null; // Not a match
            })
            .Returns(Task.CompletedTask);

        var builder = CreateBuilder();
        MapInboundEntry(builder, "api/Store", handler: next.Object);
        var route = builder.Build();

        var context = CreateRouteContext("/api/Store");

        var routeData = context.RouteData;
        routeData.Values.Add("action", "Index");

        var originalValues = new RouteValueDictionary(context.RouteData.Values);

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.Equal(originalValues, context.RouteData.Values);
        Assert.NotEqual(nestedValues, context.RouteData.Values);
    }

    [Fact]
    public async Task TreeRouter_SnapshotsRouteData_ResetsWhenNotMatched()
    {
        // Arrange
        RouteValueDictionary nestedValues = null;
        List<IRouter> nestedRouters = null;

        var next = new Mock<IRouter>();
        next
            .Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
            .Callback<RouteContext>(c =>
            {
                nestedValues = new RouteValueDictionary(c.RouteData.Values);
                nestedRouters = new List<IRouter>(c.RouteData.Routers);
                c.Handler = null; // Not a match
            })
            .Returns(Task.CompletedTask);

        var builder = CreateBuilder();
        MapInboundEntry(builder, "api/Store", handler: next.Object);
        var route = builder.Build();

        var context = CreateRouteContext("/api/Store");

        context.RouteData.Values.Add("action", "Index");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotEqual(nestedValues, context.RouteData.Values);

        // The new routedata is a copy
        Assert.Equal("Index", context.RouteData.Values["action"]);
        Assert.Equal("Index", nestedValues["action"]);
        Assert.DoesNotContain(context.RouteData.Values, kvp => kvp.Key == "test_route_group");
        Assert.Single(nestedValues, kvp => kvp.Key == "test_route_group");

        Assert.Empty(context.RouteData.Routers);

        Assert.Single(nestedRouters);
        Assert.Equal(next.Object.GetType(), nestedRouters[0].GetType());
    }

    [Fact]
    public async Task TreeRouter_SnapshotsRouteData_ResetsWhenThrows()
    {
        // Arrange
        RouteValueDictionary nestedValues = null;
        List<IRouter> nestedRouters = null;

        var next = new Mock<IRouter>();
        next
            .Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
            .Callback<RouteContext>(c =>
            {
                nestedValues = new RouteValueDictionary(c.RouteData.Values);
                nestedRouters = new List<IRouter>(c.RouteData.Routers);
                throw new Exception();
            })
            .Returns(Task.CompletedTask);

        var builder = CreateBuilder();
        MapInboundEntry(builder, "api/Store", handler: next.Object);
        var route = builder.Build();

        var context = CreateRouteContext("/api/Store");
        context.RouteData.Values.Add("action", "Index");

        // Act
        await Assert.ThrowsAsync<Exception>(() => route.RouteAsync(context));

        // Assert
        Assert.NotEqual(nestedValues, context.RouteData.Values);

        Assert.Equal("Index", context.RouteData.Values["action"]);
        Assert.Equal("Index", nestedValues["action"]);
        Assert.DoesNotContain(context.RouteData.Values, kvp => kvp.Key == "test_route_group");
        Assert.Single(nestedValues, kvp => kvp.Key == "test_route_group");

        Assert.Empty(context.RouteData.Routers);

        Assert.Single(nestedRouters);
        Assert.Equal(next.Object.GetType(), nestedRouters[0].GetType());
    }

    [Fact]
    public async Task TreeRouter_SnapshotsRouteData_ResetsBeforeMatchingEachRouteEntry()
    {
        // This test replicates a scenario raised as issue https://github.com/aspnet/Routing/issues/394
        // The RouteValueDictionary entries populated while matching route entries should not be left
        // in place if the route entry turns out not to match, because that would leak unwanted state
        // to subsequent route entries and might cause "An element with the key ... already exists"
        // exceptions.

        // Arrange
        RouteValueDictionary nestedValues = null;
        var next = new Mock<IRouter>();
        next
            .Setup(r => r.RouteAsync(It.IsAny<RouteContext>()))
            .Callback<RouteContext>(c =>
            {
                nestedValues = new RouteValueDictionary(c.RouteData.Values);
                c.Handler = NullHandler;
            })
            .Returns(Task.CompletedTask);

        var builder = CreateBuilder();
        MapInboundEntry(builder, "cat_{category1}/prod1_{product}"); // Matches on first segment but not on second
        MapInboundEntry(builder, "cat_{category2}/prod2_{product}", handler: next.Object);
        var route = builder.Build();

        var context = CreateRouteContext("/cat_examplecategory/prod2_exampleproduct");

        // Act
        await route.RouteAsync(context);

        // Assert
        Assert.NotNull(nestedValues);
        Assert.Equal("examplecategory", nestedValues["category2"]);
        Assert.Equal("exampleproduct", nestedValues["product"]);
        Assert.DoesNotContain(nestedValues, kvp => kvp.Key == "category1");
    }

    [Fact]
    public void TreeRouter_GenerateLink_MatchesNullRequiredValue_WithNullRequestValueString()
    {
        // Arrange
        var builder = CreateBuilder();
        var entry = MapOutboundEntry(
            builder,
            "Help/Store",
            requiredValues: new { area = (string)null, action = "Edit", controller = "Store" });
        var route = builder.Build();
        var context = CreateVirtualPathContext(new { area = (string)null, action = "Edit", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Help/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_MatchesNullRequiredValue_WithEmptyRequestValueString()
    {
        // Arrange
        var builder = CreateBuilder();
        var entry = MapOutboundEntry(
            builder,
            "Help/Store",
            requiredValues: new { area = (string)null, action = "Edit", controller = "Store" });
        var route = builder.Build();
        var context = CreateVirtualPathContext(new { area = "", action = "Edit", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Help/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_MatchesEmptyStringRequiredValue_WithNullRequestValueString()
    {
        // Arrange
        var builder = CreateBuilder();
        var entry = MapOutboundEntry(
            builder,
            "Help/Store",
            requiredValues: new { foo = "", action = "Edit", controller = "Store" });
        var route = builder.Build();
        var context = CreateVirtualPathContext(new { foo = (string)null, action = "Edit", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Help/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    [Fact]
    public void TreeRouter_GenerateLink_MatchesEmptyStringRequiredValue_WithEmptyRequestValueString()
    {
        // Arrange
        var builder = CreateBuilder();
        var entry = MapOutboundEntry(
            builder,
            "Help/Store",
            requiredValues: new { foo = "", action = "Edit", controller = "Store" });
        var route = builder.Build();
        var context = CreateVirtualPathContext(new { foo = "", action = "Edit", controller = "Store" });

        // Act
        var pathData = route.GetVirtualPath(context);

        // Assert
        Assert.NotNull(pathData);
        Assert.Equal("/Help/Store", pathData.VirtualPath);
        Assert.Same(route, pathData.Router);
        Assert.Empty(pathData.DataTokens);
    }

    private static RouteContext CreateRouteContext(string requestPath)
    {
        var request = new Mock<HttpRequest>(MockBehavior.Strict);
        request.SetupGet(r => r.Path).Returns(new PathString(requestPath));

        var context = new Mock<HttpContext>(MockBehavior.Strict);
        context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
            .Returns(NullLoggerFactory.Instance);

        context.SetupGet(c => c.Request).Returns(request.Object);

        return new RouteContext(context.Object);
    }

    private static VirtualPathContext CreateVirtualPathContext(
        object values,
        object ambientValues = null,
        string name = null)
    {
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(h => h.RequestServices.GetService(typeof(ILoggerFactory)))
            .Returns(NullLoggerFactory.Instance);

        return new VirtualPathContext(
            mockHttpContext.Object,
            new RouteValueDictionary(ambientValues),
            new RouteValueDictionary(values),
            name);
    }

    private static InboundRouteEntry MapInboundEntry(
        TreeRouteBuilder builder,
        string template,
        int order = 0,
        IRouter handler = null)
    {
        var entry = builder.MapInbound(
            handler ?? new StubRouter(),
            TemplateParser.Parse(template),
            routeName: null,
            order: order);

        // Add a generated 'route group' so we can identify later which entry matched.
        entry.Defaults["test_route_group"] = CreateRouteGroup(order, template);

        return entry;
    }

    private static OutboundRouteEntry MapOutboundEntry(
        TreeRouteBuilder builder,
        string template,
        object requiredValues = null,
        int order = 0,
        string name = null,
        IRouter handler = null)
    {
        var entry = builder.MapOutbound(
            handler ?? new StubRouter(),
            TemplateParser.Parse(template),
            requiredLinkValues: new RouteValueDictionary(requiredValues),
            routeName: name,
            order: order);

        // Add a generated 'route group' so we can identify later which entry matched.
        entry.Defaults["test_route_group"] = CreateRouteGroup(order, template);

        return entry;
    }

    private static string CreateRouteGroup(int order, string template)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}&{1}", order, template);
    }

    private static DefaultInlineConstraintResolver CreateConstraintResolver()
    {
        var options = new RouteOptions();
        options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");

        var optionsMock = new Mock<IOptions<RouteOptions>>();
        optionsMock.SetupGet(o => o.Value).Returns(options);

        return new DefaultInlineConstraintResolver(optionsMock.Object, new TestServiceProvider());
    }

    private static TreeRouteBuilder CreateBuilder()
    {
        var objectPoolProvider = new DefaultObjectPoolProvider();
        var objectPolicy = new UriBuilderContextPooledObjectPolicy();
        var objectPool = objectPoolProvider.Create<UriBuildingContext>(objectPolicy);

        var constraintResolver = CreateConstraintResolver();
        var builder = new TreeRouteBuilder(
            NullLoggerFactory.Instance,
            objectPool,
            constraintResolver);
        return builder;
    }

    private static TreeRouter CreateTreeRouter(
        string firstTemplate,
        string secondTemplate)
    {
        var builder = CreateBuilder();
        MapOutboundEntry(builder, firstTemplate);
        MapOutboundEntry(builder, secondTemplate);
        return builder.Build();
    }

    private class StubRouter : IRouter
    {
        public VirtualPathContext GenerationContext { get; set; }

        public RouteContext MatchingContext { get; set; }

        public Func<RouteContext, bool> MatchingDelegate { get; set; }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            GenerationContext = context;
            return null;
        }

        public Task RouteAsync(RouteContext context)
        {
            if (MatchingDelegate == null)
            {
                context.Handler = NullHandler;
            }
            else
            {
                context.Handler = MatchingDelegate(context) ? NullHandler : null;
            }

            return Task.FromResult(true);
        }
    }
}
