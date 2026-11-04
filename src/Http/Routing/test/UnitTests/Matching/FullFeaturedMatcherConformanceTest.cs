// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

// This class includes features that we have not yet implemented in the DFA
// and instruction matchers.
//
// As those matchers add features we can move tests from this class into
// MatcherConformanceTest and delete this.
public abstract class FullFeaturedMatcherConformanceTest : MatcherConformanceTest
{
    [Theory]
    [InlineData("/a/{b=15}", "/a/b", new string[] { "b", }, new string[] { "b", })]
    [InlineData("/a/{b=15}", "/a/", new string[] { "b", }, new string[] { "15", })]
    [InlineData("/a/{b=15}", "/a", new string[] { "b", }, new string[] { "15", })]
    [InlineData("/{a}/{b=15}", "/54/b", new string[] { "a", "b", }, new string[] { "54", "b", })]
    [InlineData("/{a=19}/{b=15}", "/54/b", new string[] { "a", "b", }, new string[] { "54", "b", })]
    [InlineData("/{a=19}/{b=15}", "/54/", new string[] { "a", "b", }, new string[] { "54", "15", })]
    [InlineData("/{a=19}/{b=15}", "/54", new string[] { "a", "b", }, new string[] { "54", "15", })]
    [InlineData("/{a=19}/{b=15}", "/", new string[] { "a", "b", }, new string[] { "19", "15", })]
    public virtual async Task Match_DefaultValues(string template, string path, string[] keys, string[] values)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, keys, values);
    }

    [Fact]
    public virtual async Task Match_NonInlineDefaultValues()
    {
        // Arrange
        var endpoint = CreateEndpoint("/a/{b}/{c}", new { b = "17", c = "18", });
        var matcher = CreateMatcher(endpoint);
        var httpContext = CreateContext("/a");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, new { b = "17", c = "18", });
    }

    [Fact]
    public virtual async Task Match_ExtraDefaultValues()
    {
        // Arrange
        var endpoint = CreateEndpoint("/a/{b}/{c}", new { b = "17", c = "18", d = "19" });
        var matcher = CreateMatcher(endpoint);
        var httpContext = CreateContext("/a");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, new { b = "17", c = "18", d = "19" });
    }

    [Theory]
    [InlineData("/a/{b=15}", "/54/b")]
    [InlineData("/a/{b=15}", "/54/")]
    [InlineData("/a/{b=15}", "/54")]
    [InlineData("/a/{b=15}", "/a//")]
    [InlineData("/a/{b=15}", "/54/43/23")]
    [InlineData("/{a=19}/{b=15}", "/54/b/c")]
    [InlineData("/a/{b=15}/c", "/a/b")] // Intermediate default values don't act like optional segments
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a")]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b")]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c")]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d")]
    public virtual async Task NotMatch_DefaultValues(string template, string path)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertNotMatch(httpContext);
    }

    [Theory]
    [InlineData("/{a?}/{b?}/{c?}", "/", null, null)]
    [InlineData("/{a?}/{b?}/{c?}", "/a", new[] { "a", }, new[] { "a", })]
    [InlineData("/{a?}/{b?}/{c?}", "/a/", new[] { "a", }, new[] { "a", })]
    [InlineData("/{a?}/{b?}/{c?}", "/a/b", new[] { "a", "b", }, new[] { "a", "b", })]
    [InlineData("/{a?}/{b?}/{c?}", "/a/b/", new[] { "a", "b", }, new[] { "a", "b", })]
    [InlineData("/{a?}/{b?}/{c?}", "/a/b/c", new[] { "a", "b", "c", }, new[] { "a", "b", "c", })]
    [InlineData("/{a?}/{b?}/{c?}", "/a/b/c/", new[] { "a", "b", "c", }, new[] { "a", "b", "c", })]
    [InlineData("/{c}/{a?}", "/h/i", new[] { "c", "a", }, new[] { "h", "i", })]
    [InlineData("/{c}/{a?}", "/h/", new[] { "c", }, new[] { "h", })]
    [InlineData("/{c}/{a?}", "/h", new[] { "c", }, new[] { "h", })]
    [InlineData("/{c?}/{a?}", "/", null, null)]
    [InlineData("/{c}/{a?}/{id?}", "/h/i/18", new[] { "c", "a", "id", }, new[] { "h", "i", "18", })]
    [InlineData("/{c}/{a?}/{id?}", "/h/i", new[] { "c", "a", }, new[] { "h", "i", })]
    [InlineData("/{c}/{a?}/{id?}", "/h", new[] { "c", }, new[] { "h", })]
    [InlineData("template/{p:int?}", "/template/5", new[] { "p", }, new[] { "5", })]
    [InlineData("template/{p:int?}", "/template", null, null)]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d/e", new[] { "b", "d", "f" }, new[] { "b", "d", null, })]
    [InlineData("a/{b=3}/c/{d?}/e/{*f}", "/a/b/c/d/e/f", new[] { "b", "d", "f", }, new[] { "b", "d", "f", })]
    public virtual async Task Match_OptionalParameter(string template, string path, string[] keys, string[] values)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, keys, values);
    }

    [Theory]
    [InlineData("/{a?}/{b?}/{c?}", "///")]
    [InlineData("/{a?}/{b?}/{c?}", "/a//")]
    [InlineData("/{a?}/{b?}/{c?}", "/a/b//")]
    [InlineData("/{a?}/{b?}/{c?}", "//b//")]
    [InlineData("/{a?}/{b?}/{c?}", "///c")]
    [InlineData("/{a?}/{b?}/{c?}", "///c/")]
    [InlineData("/{a?}/{b?}/{c?}", "/a/b/c/d")]
    [InlineData("/a/{b?}/{c?}", "/")]
    [InlineData("template/{parameter:int?}", "/template/qwer")]
    public virtual async Task NotMatch_OptionalParameter(string template, string path)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertNotMatch(httpContext);
    }

    [Theory]
    [InlineData("/{a}/{*b}", "/a", new[] { "a", "b", }, new[] { "a", null, })]
    [InlineData("/{a}/{*b}", "/a/", new[] { "a", "b", }, new[] { "a", null, })]
    [InlineData("/{a}/{*b=b}", "/a", new[] { "a", "b", }, new[] { "a", "b", })]
    [InlineData("/{a}/{*b=b}", "/a/", new[] { "a", "b", }, new[] { "a", "b", })]
    [InlineData("/{a}/{*b=b}", "/a/hello", new[] { "a", "b", }, new[] { "a", "hello", })]
    [InlineData("/{a}/{*b=b}", "/a/hello/goodbye", new[] { "a", "b", }, new[] { "a", "hello/goodbye", })]
    [InlineData("/{a}/{*b=b}", "/a/b//", new[] { "a", "b", }, new[] { "a", "b//", })]
    [InlineData("/{a}/{*b=b}", "/a/b/c/", new[] { "a", "b", }, new[] { "a", "b/c/", })]
    [InlineData("/{a=1}/{b=2}/{c=3}/{d=4}", "/a/b/c", new[] { "a", "b", "c", "d", }, new[] { "a", "b", "c", "4", })]
    [InlineData("a/{*path:regex(10/20/30)}", "/a/10/20/30", new[] { "path", }, new[] { "10/20/30" })]
    public virtual async Task Match_CatchAllParameter(string template, string path, string[] keys, string[] values)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, keys, values);
    }

    // Historically catchall segments don't match an empty segment, but only if it's
    // the first one. So `/a/b//` would match, but `/a//` would not. This is pretty
    // weird and inconsistent with the intent of using a catch all. The DfaMatcher
    // fixes this issue.
    [Theory]
    [InlineData("/{a}/{*b=b}", "/a///", new[] { "a", "b", }, new[] { "a", "//" })]
    [InlineData("/{a}/{*b=b}", "/a//c/", new[] { "a", "b", }, new[] { "a", "/c/" })]
    public virtual async Task Quirks_CatchAllParameter(string template, string path, string[] keys, string[] values)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertNotMatch(httpContext);

        // Need to access these to prevent a warning from the xUnit analyzer.
        // Some of these tests will match (and process the values) and some will not.
        GC.KeepAlive(keys);
        GC.KeepAlive(values);
    }

    [Theory]
    [InlineData("{p}x{s}", "/xxxxxxxxxx", new[] { "p", "s" }, new[] { "xxxxxxxx", "x", })]
    [InlineData("{p}xyz{s}", "/xxxxyzxyzxxxxxxyz", new[] { "p", "s" }, new[] { "xxxxyz", "xxxxxxyz", })]
    [InlineData("{p}xyz{s}", "/abcxxxxyzxyzxxxxxxyzxx", new[] { "p", "s" }, new[] { "abcxxxxyzxyzxxxxx", "xx", })]
    [InlineData("{p}xyz{s}", "/xyzxyzxyzxyzxyz", new[] { "p", "s" }, new[] { "xyzxyzxyz", "xyz", })]
    [InlineData("{p}xyz{s}", "/xyzxyzxyzxyzxyz1", new[] { "p", "s" }, new[] { "xyzxyzxyzxyz", "1", })]
    [InlineData("{p}xyz{s}", "/xyzxyzxyz", new[] { "p", "s" }, new[] { "xyz", "xyz", })]
    [InlineData("{p}aa{s}", "/aaaaa", new[] { "p", "s" }, new[] { "aa", "a", })]
    [InlineData("{p}aaa{s}", "/aaaaa", new[] { "p", "s" }, new[] { "a", "a", })]
    [InlineData("language/{lang=en}-{region=US}", "/language/xx-yy", new[] { "lang", "region" }, new[] { "xx", "yy", })]
    [InlineData("language/{lang}-{region}", "/language/en-US", new[] { "lang", "region" }, new[] { "en", "US", })]
    [InlineData("language/{lang}-{region}a", "/language/en-USa", new[] { "lang", "region" }, new[] { "en", "US", })]
    [InlineData("language/a{lang}-{region}", "/language/aen-US", new[] { "lang", "region" }, new[] { "en", "US", })]
    [InlineData("language/a{lang}-{region}a", "/language/aen-USa", new[] { "lang", "region" }, new[] { "en", "US", })]
    [InlineData("language/{lang}-", "/language/en-", new[] { "lang", }, new[] { "en", })]
    [InlineData("language/a{lang}", "/language/aen", new[] { "lang", }, new[] { "en", })]
    [InlineData("language/a{lang}a", "/language/aena", new[] { "lang", }, new[] { "en", })]
    public virtual async Task Match_ComplexSegment(string template, string path, string[] keys, string[] values)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, keys, values);
    }

    [Theory]
    [InlineData("language/a{lang}-{region}a", "/language/a-USa")]
    [InlineData("language/a{lang}-{region}a", "/language/aen-a")]
    [InlineData("language/{lang=en}-{region=US}", "/language")]
    [InlineData("language/{lang=en}-{region=US}", "/language/-")]
    [InlineData("language/{lang=en}-{region=US}", "/language/xx-")]
    [InlineData("language/{lang=en}-{region=US}", "/language/-xx")]
    public virtual async Task NotMatch_ComplexSegment(string template, string path)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertNotMatch(httpContext);
    }

    [Theory]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo.bar", new[] { "p1", "p2", }, new[] { "foo", "bar" })]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo", new[] { "p1", }, new[] { "foo", })]
    [InlineData("moo/{p1}.{p2?}", "/moo/.foo", new[] { "p1", }, new[] { ".foo", })]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo..bar", new[] { "p1", "p2", }, new[] { "foo.", "bar" })]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo.moo.bar", new[] { "p1", "p2", }, new[] { "foo.moo", "bar" })]
    [InlineData("moo/foo.{p1}.{p2?}", "/moo/foo.moo", new[] { "p1", }, new[] { "moo", })]
    [InlineData("moo/foo.{p1}.{p2?}", "/moo/foo.foo.bar", new[] { "p1", "p2", }, new[] { "foo", "bar" })]
    [InlineData("moo/.{p2?}", "/moo/.foo", new[] { "p2", }, new[] { "foo", })]
    [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo.bar", new[] { "p1", "p2", "p3" }, new[] { "foo", "moo", "bar" })]
    [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo", new[] { "p1", "p2", }, new[] { "foo", "moo" })]
    [InlineData("moo/{p1}.{p2}.{p3}.{p4?}", "/moo/foo.moo.bar", new[] { "p1", "p2", "p3" }, new[] { "foo", "moo", "bar" })]
    [InlineData("{p1}.{p2?}/{p3}", "/foo.moo/bar", new[] { "p1", "p2", "p3" }, new[] { "foo", "moo", "bar" })]
    [InlineData("{p1}.{p2?}/{p3}", "/foo/bar", new[] { "p1", "p3" }, new[] { "foo", "bar" })]
    [InlineData("{p1}.{p2?}/{p3}", "/.foo/bar", new[] { "p1", "p3" }, new[] { ".foo", "bar" })]
    [InlineData("{p1}/{p2}/{p3?}", "/foo/bar/baz", new[] { "p1", "p2", "p3" }, new[] { "foo", "bar", "baz" })]
    public virtual async Task Match_OptionalSeparator(string template, string path, string[] keys, string[] values)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, keys, values);
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
    public virtual async Task NotMatch_OptionalSeparator(string template, string path)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertNotMatch(httpContext);
    }

    // Most of are copied from old routing tests that date back to the VS 2010 era. Enjoy!
    [Theory]
    [InlineData("{Controller}.mvc/../{action}", "/Home.mvc/../index", new string[] { "Controller", "action" }, new string[] { "Home", "index" })]
    [InlineData("{Controller}.mvc/.../{action}", "/Home.mvc/.../index", new string[] { "Controller", "action" }, new string[] { "Home", "index" })]
    [InlineData("{Controller}.mvc/../../../{action}", "/Home.mvc/../../../index", new string[] { "Controller", "action" }, new string[] { "Home", "index" })]
    [InlineData("{Controller}.mvc!/{action}", "/Home.mvc!/index", new string[] { "Controller", "action" }, new string[] { "Home", "index" })]
    [InlineData("../{Controller}.mvc", "/../Home.mvc", new string[] { "Controller", }, new string[] { "Home", })]
    [InlineData(@"\{Controller}.mvc", @"/\Home.mvc", new string[] { "Controller", }, new string[] { "Home", })]
    [InlineData(@"{Controller}.mvc\{id}\{Param1}", @"/Home.mvc\123\p1", new string[] { "Controller", "id", "Param1" }, new string[] { "Home", "123", "p1" })]
    [InlineData("(Controller).mvc", "/(Controller).mvc", new string[] { }, new string[] { })]
    [InlineData("Controller.mvc/ ", "/Controller.mvc/ ", new string[] { }, new string[] { })]
    [InlineData("Controller.mvc ", "/Controller.mvc ", new string[] { }, new string[] { })]
    public virtual async Task Match_WeirdCharacterCases(string template, string path, string[] keys, string[] values)
    {
        // Arrange
        var (matcher, endpoint) = CreateMatcher(template);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, endpoint, keys, values);
    }

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
    public virtual async Task Match_SelectEndpoint_BasedOnPrecedence(string template1, string template2)
    {
        // Arrange
        var expected = CreateEndpoint(template1);
        var other = CreateEndpoint(template2);
        var path = "/template/5";

        // Arrange
        var matcher = CreateMatcher(other, expected);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, expected, ignoreValues: true);
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
    [InlineData("template/5", "template/5")]
    [InlineData("template/{parameter:int}", "template/{parameter:int}")]
    [InlineData("template/{parameter}", "template/{parameter}")]
    [InlineData("template/{*parameter:int}", "template/{*parameter:int}")]
    [InlineData("template/{*parameter}", "template/{*parameter}")]
    public virtual async Task Match_SelectEndpoint_BasedOnOrder(string template1, string template2)
    {
        // Arrange
        var expected = CreateEndpoint(template1, order: 0);
        var other = CreateEndpoint(template2, order: 1);
        var path = "/template/5";

        // Arrange
        var matcher = CreateMatcher(other, expected);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, expected, ignoreValues: true);
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
    public virtual async Task Match_IntegrationTest_MultipleEndpoints(string path, string expectedTemplate)
    {
        // Arrange
        var templates = new[]
        {
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

        var endpoints = templates.Select((t) => CreateEndpoint(t)).ToArray();
        var expected = endpoints[Array.IndexOf(templates, expectedTemplate)];

        var matcher = CreateMatcher(endpoints);
        var httpContext = CreateContext(path);

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, expected, ignoreValues: true);
    }

    // https://github.com/dotnet/aspnetcore/issues/16579
    [Fact]
    public virtual async Task Match_Regression_16579_Order1()
    {
        var endpoints = new RouteEndpoint[]
        {
                EndpointFactory.CreateRouteEndpoint(
                    "{controller}/folder/{*path}",
                    order: 0,
                    defaults: new { controller = "File", action = "Folder", },
                    requiredValues: new { controller = "File", }),
                EndpointFactory.CreateRouteEndpoint(
                    "{controller}/{action}/{filename}",
                    order: 1,
                    defaults: new { controller = "File", action = "Index", },
                    requiredValues: new { controller = "File", action = "Index", }),
        };

        var expected = endpoints[0];

        var matcher = CreateMatcher(endpoints);
        var httpContext = CreateContext("/file/folder/abc/abc");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, expected, ignoreValues: true);
    }

    // https://github.com/dotnet/aspnetcore/issues/16579
    [Fact]
    public virtual async Task Match_Regression_16579_Order2()
    {
        var endpoints = new RouteEndpoint[]
        {
                EndpointFactory.CreateRouteEndpoint(
                    "{controller}/{action}/{filename}",
                    order: 0,
                    defaults: new { controller = "File", action = "Index", },
                    requiredValues: new { controller = "File", action = "Index", }),

                EndpointFactory.CreateRouteEndpoint(
                    "{controller}/folder/{*path}",
                    order: 1,
                    defaults: new { controller = "File", action = "Folder", },
                    requiredValues: new { controller = "File", }),
        };

        var expected = endpoints[1];

        var matcher = CreateMatcher(endpoints);
        var httpContext = CreateContext("/file/folder/abc/abc");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, expected, ignoreValues: true);
    }
}
