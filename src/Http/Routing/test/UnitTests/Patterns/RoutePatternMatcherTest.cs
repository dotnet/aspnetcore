// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing;

public class RoutePatternMatcherTest
{
    [Fact]
    public void TryMatch_Success()
    {
        // Arrange
        var matcher = CreateMatcher("{controller}/{action}/{id}");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/Bank/DoAction/123", values);

        // Assert
        Assert.True(match);
        Assert.Equal("Bank", values["controller"]);
        Assert.Equal("DoAction", values["action"]);
        Assert.Equal("123", values["id"]);
    }

    [Fact]
    public void TryMatch_Fails()
    {
        // Arrange
        var matcher = CreateMatcher("{controller}/{action}/{id}");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/Bank/DoAction", values);

        // Assert
        Assert.False(match);
    }

    [Fact]
    public void TryMatch_WithDefaults_Success()
    {
        // Arrange
        var matcher = CreateMatcher("{controller}/{action}/{id}", new { id = "default id" });

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/Bank/DoAction", values);

        // Assert
        Assert.True(match);
        Assert.Equal("Bank", values["controller"]);
        Assert.Equal("DoAction", values["action"]);
        Assert.Equal("default id", values["id"]);
    }

    [Fact]
    public void TryMatch_WithDefaults_Fails()
    {
        // Arrange
        var matcher = CreateMatcher("{controller}/{action}/{id}", new { id = "default id" });

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/Bank", values);

        // Assert
        Assert.False(match);
    }

    [Fact]
    public void TryMatch_WithLiterals_Success()
    {
        // Arrange
        var matcher = CreateMatcher("moo/{p1}/bar/{p2}", new { p2 = "default p2" });

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/111/bar/222", values);

        // Assert
        Assert.True(match);
        Assert.Equal("111", values["p1"]);
        Assert.Equal("222", values["p2"]);
    }

    [Fact]
    public void TryMatch_RouteWithLiteralsAndDefaults_Success()
    {
        // Arrange
        var matcher = CreateMatcher("moo/{p1}/bar/{p2}", new { p2 = "default p2" });

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/111/bar/", values);

        // Assert
        Assert.True(match);
        Assert.Equal("111", values["p1"]);
        Assert.Equal("default p2", values["p2"]);
    }

    [Theory]
    [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}", "/123-456-7890")] // ssn
    [InlineData(@"{p1:regex(^\w+\@\w+\.\w+)}", "/asd@assds.com")] // email
    [InlineData(@"{p1:regex(([}}])\w+)}", "/}sda")] // Not balanced }
    [InlineData(@"{p1:regex(([{{)])\w+)}", "/})sda")] // Not balanced {
    public void TryMatch_RegularExpressionConstraint_Valid(
        string template,
        string path)
    {
        // Arrange
        var matcher = CreateMatcher(template);

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch(path, values);

        // Assert
        Assert.True(match);
    }

    [Theory]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo.bar", true, "foo", "bar")]
    [InlineData("moo/{p1?}", "/moo/foo", true, "foo", null)]
    [InlineData("moo/{p1?}", "/moo", true, null, null)]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo", true, "foo", null)]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo..bar", true, "foo.", "bar")]
    [InlineData("moo/{p1}.{p2?}", "/moo/foo.moo.bar", true, "foo.moo", "bar")]
    [InlineData("moo/{p1}.{p2}", "/moo/foo.bar", true, "foo", "bar")]
    [InlineData("moo/foo.{p1}.{p2?}", "/moo/foo.moo.bar", true, "moo", "bar")]
    [InlineData("moo/foo.{p1}.{p2?}", "/moo/foo.moo", true, "moo", null)]
    [InlineData("moo/.{p2?}", "/moo/.foo", true, null, "foo")]
    [InlineData("moo/.{p2?}", "/moo", false, null, null)]
    [InlineData("moo/{p1}.{p2?}", "/moo/....", true, "..", ".")]
    [InlineData("moo/{p1}.{p2?}", "/moo/.bar", true, ".bar", null)]
    public void TryMatch_OptionalParameter_FollowedByPeriod_Valid(
        string template,
        string path,
        bool expectedMatch,
        string p1,
        string p2)
    {
        // Arrange
        var matcher = CreateMatcher(template);

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch(path, values);

        // Assert
        Assert.Equal(expectedMatch, match);
        if (p1 != null)
        {
            Assert.Equal(p1, values["p1"]);
        }
        if (p2 != null)
        {
            Assert.Equal(p2, values["p2"]);
        }
    }

    [Theory]
    [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo.bar", "foo", "moo", "bar")]
    [InlineData("moo/{p1}.{p2}.{p3?}", "/moo/foo.moo", "foo", "moo", null)]
    [InlineData("moo/{p1}.{p2}.{p3}.{p4?}", "/moo/foo.moo.bar", "foo", "moo", "bar")]
    [InlineData("{p1}.{p2?}/{p3}", "/foo.moo/bar", "foo", "moo", "bar")]
    [InlineData("{p1}.{p2?}/{p3}", "/foo/bar", "foo", null, "bar")]
    [InlineData("{p1}.{p2?}/{p3}", "/.foo/bar", ".foo", null, "bar")]
    [InlineData("{p1}/{p2}/{p3?}", "/foo/bar/baz", "foo", "bar", "baz")]
    public void TryMatch_OptionalParameter_FollowedByPeriod_3Parameters_Valid(
        string template,
        string path,
        string p1,
        string p2,
        string p3)
    {
        // Arrange
        var matcher = CreateMatcher(template);

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch(path, values);

        // Assert
        Assert.True(match);
        Assert.Equal(p1, values["p1"]);

        if (p2 != null)
        {
            Assert.Equal(p2, values["p2"]);
        }

        if (p3 != null)
        {
            Assert.Equal(p3, values["p3"]);
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
    public void TryMatch_OptionalParameter_FollowedByPeriod_Invalid(string template, string path)
    {
        // Arrange
        var matcher = CreateMatcher(template);

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch(path, values);

        // Assert
        Assert.False(match);
    }

    [Fact]
    public void TryMatch_RouteWithOnlyLiterals_Success()
    {
        // Arrange
        var matcher = CreateMatcher("moo/bar");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/bar", values);

        // Assert
        Assert.True(match);
        Assert.Empty(values);
    }

    [Fact]
    public void TryMatch_RouteWithOnlyLiterals_Fails()
    {
        // Arrange
        var matcher = CreateMatcher("moo/bars");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/bar", values);

        // Assert
        Assert.False(match);
    }

    [Fact]
    public void TryMatch_RouteWithExtraSeparators_Success()
    {
        // Arrange
        var matcher = CreateMatcher("moo/bar");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/bar/", values);

        // Assert
        Assert.True(match);
        Assert.Empty(values);
    }

    [Fact]
    public void TryMatch_UrlWithExtraSeparators_Success()
    {
        // Arrange
        var matcher = CreateMatcher("moo/bar/");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/bar", values);

        // Assert
        Assert.True(match);
        Assert.Empty(values);
    }

    [Fact]
    public void TryMatch_RouteWithParametersAndExtraSeparators_Success()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{p2}/");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/bar", values);

        // Assert
        Assert.True(match);
        Assert.Equal("moo", values["p1"]);
        Assert.Equal("bar", values["p2"]);
    }

    [Fact]
    public void TryMatch_RouteWithDifferentLiterals_Fails()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{p2}/baz");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/bar/boo", values);

        // Assert
        Assert.False(match);
    }

    [Fact]
    public void TryMatch_LongerUrl_Fails()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/moo/bar", values);

        // Assert
        Assert.False(match);
    }

    [Fact]
    public void TryMatch_SimpleFilename_Success()
    {
        // Arrange
        var matcher = CreateMatcher("DEFAULT.ASPX");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/default.aspx", values);

        // Assert
        Assert.True(match);
    }

    [Theory]
    [InlineData("{prefix}x{suffix}", "/xxxxxxxxxx")]
    [InlineData("{prefix}xyz{suffix}", "/xxxxyzxyzxxxxxxyz")]
    [InlineData("{prefix}xyz{suffix}", "/abcxxxxyzxyzxxxxxxyzxx")]
    [InlineData("{prefix}xyz{suffix}", "/xyzxyzxyzxyzxyz")]
    [InlineData("{prefix}xyz{suffix}", "/xyzxyzxyzxyzxyz1")]
    [InlineData("{prefix}xyz{suffix}", "/xyzxyzxyz")]
    [InlineData("{prefix}aa{suffix}", "/aaaaa")]
    [InlineData("{prefix}aaa{suffix}", "/aaaaa")]
    public void TryMatch_RouteWithComplexSegment_Success(string template, string path)
    {
        var matcher = CreateMatcher(template);

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch(path, values);

        // Assert
        Assert.True(match);
    }

    [Fact]
    public void TryMatch_RouteWithExtraDefaultValues_Success()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{p2}", new { p2 = (string)null, foo = "bar" });

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/v1", values);

        // Assert
        Assert.True(match);
        Assert.Equal<int>(3, values.Count);
        Assert.Equal("v1", values["p1"]);
        Assert.Null(values["p2"]);
        Assert.Equal("bar", values["foo"]);
    }

    [Fact]
    public void TryMatch_PrettyRouteWithExtraDefaultValues_Success()
    {
        // Arrange
        var matcher = CreateMatcher(
            "date/{y}/{m}/{d}",
            new { controller = "blog", action = "showpost", m = (string)null, d = (string)null });

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/date/2007/08", values);

        // Assert
        Assert.True(match);
        Assert.Equal<int>(5, values.Count);
        Assert.Equal("blog", values["controller"]);
        Assert.Equal("showpost", values["action"]);
        Assert.Equal("2007", values["y"]);
        Assert.Equal("08", values["m"]);
        Assert.Null(values["d"]);
    }

    [Fact]
    public void TryMatch_WithMultiSegmentParamsOnBothEndsMatches()
    {
        RunTest(
            "language/{lang}-{region}",
            "/language/en-US",
            null,
            new RouteValueDictionary(new { lang = "en", region = "US" }));
    }

    [Fact]
    public void TryMatch_WithMultiSegmentParamsOnLeftEndMatches()
    {
        RunTest(
            "language/{lang}-{region}a",
            "/language/en-USa",
            null,
            new RouteValueDictionary(new { lang = "en", region = "US" }));
    }

    [Fact]
    public void TryMatch_WithMultiSegmentParamsOnRightEndMatches()
    {
        RunTest(
            "language/a{lang}-{region}",
            "/language/aen-US",
            null,
            new RouteValueDictionary(new { lang = "en", region = "US" }));
    }

    [Fact]
    public void TryMatch_WithMultiSegmentParamsOnNeitherEndMatches()
    {
        RunTest(
            "language/a{lang}-{region}a",
            "/language/aen-USa",
            null,
            new RouteValueDictionary(new { lang = "en", region = "US" }));
    }

    [Fact]
    public void TryMatch_WithMultiSegmentParamsOnNeitherEndDoesNotMatch()
    {
        RunTest(
            "language/a{lang}-{region}a",
            "/language/a-USa",
            null,
            null);
    }

    [Fact]
    public void TryMatch_WithMultiSegmentParamsOnNeitherEndDoesNotMatch2()
    {
        RunTest(
            "language/a{lang}-{region}a",
            "/language/aen-a",
            null,
            null);
    }

    [Fact]
    public void TryMatch_WithSimpleMultiSegmentParamsOnBothEndsMatches()
    {
        RunTest(
            "language/{lang}",
            "/language/en",
            null,
            new RouteValueDictionary(new { lang = "en" }));
    }

    [Fact]
    public void TryMatch_WithSimpleMultiSegmentParamsOnBothEndsTrailingSlashDoesNotMatch()
    {
        RunTest(
            "language/{lang}",
            "/language/",
            null,
            null);
    }

    [Fact]
    public void TryMatch_WithSimpleMultiSegmentParamsOnBothEndsDoesNotMatch()
    {
        RunTest(
            "language/{lang}",
            "/language",
            null,
            null);
    }

    [Fact]
    public void TryMatch_WithSimpleMultiSegmentParamsOnLeftEndMatches()
    {
        RunTest(
            "language/{lang}-",
            "/language/en-",
            null,
            new RouteValueDictionary(new { lang = "en" }));
    }

    [Fact]
    public void TryMatch_WithSimpleMultiSegmentParamsOnRightEndMatches()
    {
        RunTest(
            "language/a{lang}",
            "/language/aen",
            null,
            new RouteValueDictionary(new { lang = "en" }));
    }

    [Fact]
    public void TryMatch_WithSimpleMultiSegmentParamsOnNeitherEndMatches()
    {
        RunTest(
            "language/a{lang}a",
            "/language/aena",
            null,
            new RouteValueDictionary(new { lang = "en" }));
    }

    [Fact]
    public void TryMatch_WithMultiSegmentStandamatchMvcRouteMatches()
    {
        RunTest(
            "{controller}.mvc/{action}/{id}",
            "/home.mvc/index",
            new RouteValueDictionary(new { action = "Index", id = (string)null }),
            new RouteValueDictionary(new { controller = "home", action = "index", id = (string)null }));
    }

    [Fact]
    public void TryMatch_WithMultiSegmentParamsOnBothEndsWithDefaultValuesMatches()
    {
        RunTest(
            "language/{lang}-{region}",
            "/language/-",
            new RouteValueDictionary(new { lang = "xx", region = "yy" }),
            null);
    }

    [Fact]
    public void TryMatch_WithUrlWithMultiSegmentWithRepeatedDots()
    {
        RunTest(
            "{Controller}..mvc/{id}/{Param1}",
            "/Home..mvc/123/p1",
            null,
            new RouteValueDictionary(new { Controller = "Home", id = "123", Param1 = "p1" }));
    }

    [Fact]
    public void TryMatch_WithUrlWithTwoRepeatedDots()
    {
        RunTest(
            "{Controller}.mvc/../{action}",
            "/Home.mvc/../index",
            null,
            new RouteValueDictionary(new { Controller = "Home", action = "index" }));
    }

    [Fact]
    public void TryMatch_WithUrlWithThreeRepeatedDots()
    {
        RunTest(
            "{Controller}.mvc/.../{action}",
            "/Home.mvc/.../index",
            null,
            new RouteValueDictionary(new { Controller = "Home", action = "index" }));
    }

    [Fact]
    public void TryMatch_WithUrlWithManyRepeatedDots()
    {
        RunTest(
            "{Controller}.mvc/../../../{action}",
            "/Home.mvc/../../../index",
            null,
            new RouteValueDictionary(new { Controller = "Home", action = "index" }));
    }

    [Fact]
    public void TryMatch_WithUrlWithExclamationPoint()
    {
        RunTest(
            "{Controller}.mvc!/{action}",
            "/Home.mvc!/index",
            null,
            new RouteValueDictionary(new { Controller = "Home", action = "index" }));
    }

    [Fact]
    public void TryMatch_WithUrlWithStartingDotDotSlash()
    {
        RunTest(
            "../{Controller}.mvc",
            "/../Home.mvc",
            null,
            new RouteValueDictionary(new { Controller = "Home" }));
    }

    [Fact]
    public void TryMatch_WithUrlWithStartingBackslash()
    {
        RunTest(
            @"\{Controller}.mvc",
            @"/\Home.mvc",
            null,
            new RouteValueDictionary(new { Controller = "Home" }));
    }

    [Fact]
    public void TryMatch_WithUrlWithBackslashSeparators()
    {
        RunTest(
            @"{Controller}.mvc\{id}\{Param1}",
            @"/Home.mvc\123\p1",
            null,
            new RouteValueDictionary(new { Controller = "Home", id = "123", Param1 = "p1" }));
    }

    [Fact]
    public void TryMatch_WithUrlWithParenthesesLiterals()
    {
        RunTest(
            @"(Controller).mvc",
            @"/(Controller).mvc",
            null,
            new RouteValueDictionary());
    }

    [Fact]
    public void TryMatch_WithUrlWithTrailingSlashSpace()
    {
        RunTest(
            @"Controller.mvc/ ",
            @"/Controller.mvc/ ",
            null,
            new RouteValueDictionary());
    }

    [Fact]
    public void TryMatch_WithUrlWithTrailingSpace()
    {
        RunTest(
            @"Controller.mvc ",
            @"/Controller.mvc ",
            null,
            new RouteValueDictionary());
    }

    [Fact]
    public void TryMatch_WithCatchAllCapturesDots()
    {
        // DevDiv Bugs 189892: UrlRouting: Catch all parameter cannot capture url segments that contain the "."
        RunTest(
            "Home/ShowPilot/{missionId}/{*name}",
            "/Home/ShowPilot/777/12345./foobar",
            new RouteValueDictionary(new
            {
                controller = "Home",
                action = "ShowPilot",
                missionId = (string)null,
                name = (string)null
            }),
            new RouteValueDictionary(new { controller = "Home", action = "ShowPilot", missionId = "777", name = "12345./foobar" }));
    }

    [Fact]
    public void TryMatch_RouteWithCatchAll_MatchesMultiplePathSegments()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{*p2}");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/v1/v2/v3", values);

        // Assert
        Assert.True(match);
        Assert.Equal<int>(2, values.Count);
        Assert.Equal("v1", values["p1"]);
        Assert.Equal("v2/v3", values["p2"]);
    }

    [Fact]
    public void TryMatch_RouteWithCatchAll_MatchesTrailingSlash()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{*p2}");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/v1/", values);

        // Assert
        Assert.True(match);
        Assert.Equal<int>(2, values.Count);
        Assert.Equal("v1", values["p1"]);
        Assert.Null(values["p2"]);
    }

    [Fact]
    public void TryMatch_RouteWithCatchAll_MatchesEmptyContent()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{*p2}");

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch("/v1", values);

        // Assert
        Assert.True(match);
        Assert.Equal<int>(2, values.Count);
        Assert.Equal("v1", values["p1"]);
        Assert.Null(values["p2"]);
    }

    [Fact]
    public void TryMatch_RouteWithCatchAll_MatchesEmptyContent_DoesNotReplaceExistingRouteValue()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{*p2}");

        var values = new RouteValueDictionary(new { p2 = "hello" });

        // Act
        var match = matcher.TryMatch("/v1", values);

        // Assert
        Assert.True(match);
        Assert.Equal<int>(2, values.Count);
        Assert.Equal("v1", values["p1"]);
        Assert.Equal("hello", values["p2"]);
    }

    [Fact]
    public void TryMatch_RouteWithCatchAll_UsesDefaultValueForEmptyContent()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{*p2}", new { p2 = "catchall" });

        var values = new RouteValueDictionary(new { p2 = "overridden" });

        // Act
        var match = matcher.TryMatch("/v1", values);

        // Assert
        Assert.True(match);
        Assert.Equal<int>(2, values.Count);
        Assert.Equal("v1", values["p1"]);
        Assert.Equal("catchall", values["p2"]);
    }

    [Fact]
    public void TryMatch_RouteWithCatchAll_IgnoresDefaultValueForNonEmptyContent()
    {
        // Arrange
        var matcher = CreateMatcher("{p1}/{*p2}", new { p2 = "catchall" });

        var values = new RouteValueDictionary(new { p2 = "overridden" });

        // Act
        var match = matcher.TryMatch("/v1/hello/whatever", values);

        // Assert
        Assert.True(match);
        Assert.Equal<int>(2, values.Count);
        Assert.Equal("v1", values["p1"]);
        Assert.Equal("hello/whatever", values["p2"]);
    }

    [Fact]
    public void TryMatch_DoesNotMatchOnlyLeftLiteralMatch()
    {
        // DevDiv Bugs 191180: UrlRouting: Wrong template getting matched if a url segment is a substring of the requested url
        RunTest(
            "foo",
            "/fooBAR",
            null,
            null);
    }

    [Fact]
    public void TryMatch_DoesNotMatchOnlyRightLiteralMatch()
    {
        // DevDiv Bugs 191180: UrlRouting: Wrong template getting matched if a url segment is a substring of the requested url
        RunTest(
            "foo",
            "/BARfoo",
            null,
            null);
    }

    [Fact]
    public void TryMatch_DoesNotMatchMiddleLiteralMatch()
    {
        // DevDiv Bugs 191180: UrlRouting: Wrong template getting matched if a url segment is a substring of the requested url
        RunTest(
            "foo",
            "/BARfooBAR",
            null,
            null);
    }

    [Fact]
    public void TryMatch_DoesMatchesExactLiteralMatch()
    {
        // DevDiv Bugs 191180: UrlRouting: Wrong template getting matched if a url segment is a substring of the requested url
        RunTest(
            "foo",
            "/foo",
            null,
            new RouteValueDictionary());
    }

    [Fact]
    public void TryMatch_WithWeimatchParameterNames()
    {
        RunTest(
            "foo/{ }/{.!$%}/{dynamic.data}/{op.tional}",
            "/foo/space/weimatch/omatcherid",
            new RouteValueDictionary() { { " ", "not a space" }, { "op.tional", "default value" }, { "ran!dom", "va@lue" } },
            new RouteValueDictionary() { { " ", "space" }, { ".!$%", "weimatch" }, { "dynamic.data", "omatcherid" }, { "op.tional", "default value" }, { "ran!dom", "va@lue" } });
    }

    [Fact]
    public void TryMatch_DoesNotMatchRouteWithLiteralSeparatomatchefaultsButNoValue()
    {
        RunTest(
            "{controller}/{language}-{locale}",
            "/foo",
            new RouteValueDictionary(new { language = "en", locale = "US" }),
            null);
    }

    [Fact]
    public void TryMatch_DoesNotMatchesRouteWithLiteralSeparatomatchefaultsAndLeftValue()
    {
        RunTest(
            "{controller}/{language}-{locale}",
            "/foo/xx-",
            new RouteValueDictionary(new { language = "en", locale = "US" }),
            null);
    }

    [Fact]
    public void TryMatch_DoesNotMatchesRouteWithLiteralSeparatomatchefaultsAndRightValue()
    {
        RunTest(
            "{controller}/{language}-{locale}",
            "/foo/-yy",
            new RouteValueDictionary(new { language = "en", locale = "US" }),
            null);
    }

    [Fact]
    public void TryMatch_MatchesRouteWithLiteralSeparatomatchefaultsAndValue()
    {
        RunTest(
            "{controller}/{language}-{locale}",
            "/foo/xx-yy",
            new RouteValueDictionary(new { language = "en", locale = "US" }),
            new RouteValueDictionary { { "language", "xx" }, { "locale", "yy" }, { "controller", "foo" } });
    }

    [Fact]
    public void TryMatch_SetsOptionalParameter()
    {
        // Arrange
        var route = CreateMatcher("{controller}/{action?}");
        var url = "/Home/Index";

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.True(match);
        Assert.Equal(2, values.Count);
        Assert.Equal("Home", values["controller"]);
        Assert.Equal("Index", values["action"]);
    }

    [Fact]
    public void TryMatch_DoesNotSetOptionalParameter()
    {
        // Arrange
        var route = CreateMatcher("{controller}/{action?}");
        var url = "/Home";

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.True(match);
        Assert.Single(values);
        Assert.Equal("Home", values["controller"]);
        Assert.False(values.ContainsKey("action"));
    }

    [Fact]
    public void TryMatch_DoesNotSetOptionalParameter_EmptyString()
    {
        // Arrange
        var route = CreateMatcher("{controller?}");
        var url = "";

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.True(match);
        Assert.Empty(values);
        Assert.False(values.ContainsKey("controller"));
    }

    [Fact]
    public void TryMatch__EmptyRouteWith_EmptyString()
    {
        // Arrange
        var route = CreateMatcher("");
        var url = "";

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.True(match);
        Assert.Empty(values);
    }

    [Fact]
    public void TryMatch_MultipleOptionalParameters()
    {
        // Arrange
        var route = CreateMatcher("{controller}/{action?}/{id?}");
        var url = "/Home/Index";

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.True(match);
        Assert.Equal(2, values.Count);
        Assert.Equal("Home", values["controller"]);
        Assert.Equal("Index", values["action"]);
        Assert.False(values.ContainsKey("id"));
    }

    [Theory]
    [InlineData("///")]
    [InlineData("/a//")]
    [InlineData("/a/b//")]
    [InlineData("//b//")]
    [InlineData("///c")]
    [InlineData("///c/")]
    public void TryMatch_MultipleOptionalParameters_WithEmptyIntermediateSegmentsDoesNotMatch(string url)
    {
        // Arrange
        var route = CreateMatcher("{controller?}/{action?}/{id?}");

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.False(match);
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
    public void TryMatch_MultipleOptionalParameters_WithIncrementalOptionalValues(string url)
    {
        // Arrange
        var route = CreateMatcher("{controller?}/{action?}/{id?}");

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.True(match);
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
    public void TryMatch_MultipleParameters_WithEmptyValues(string url)
    {
        // Arrange
        var route = CreateMatcher("{controller}/{action}/{id}");

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.False(match);
    }

    [Theory]
    [InlineData("/a/b/c//")]
    [InlineData("/a/b/c/////")]
    public void TryMatch_CatchAllParameters_WithEmptyValuesAtTheEnd(string url)
    {
        // Arrange
        var route = CreateMatcher("{controller}/{action}/{*id}");

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.True(match);
    }

    [Theory]
    [InlineData("/a/b//")]
    [InlineData("/a/b///c")]
    public void TryMatch_CatchAllParameters_WithEmptyValues(string url)
    {
        // Arrange
        var route = CreateMatcher("{controller}/{action}/{*id}");

        var values = new RouteValueDictionary();

        // Act
        var match = route.TryMatch(url, values);

        // Assert
        Assert.False(match);
    }

    private RoutePatternMatcher CreateMatcher(string template, object defaults = null)
    {
        return new RoutePatternMatcher(
            RoutePatternParser.Parse(template),
            new RouteValueDictionary(defaults));
    }

    private static void RunTest(
        string template,
        string path,
        RouteValueDictionary defaults,
        IDictionary<string, object> expected)
    {
        // Arrange
        var matcher = new RoutePatternMatcher(
            RoutePatternParser.Parse(template),
            defaults ?? new RouteValueDictionary());

        var values = new RouteValueDictionary();

        // Act
        var match = matcher.TryMatch(new PathString(path), values);

        // Assert
        if (expected == null)
        {
            Assert.False(match);
        }
        else
        {
            Assert.True(match);
            Assert.Equal(expected.Count, values.Count);
            foreach (string key in values.Keys)
            {
                Assert.Equal(expected[key], values[key]);
            }
        }
    }
}
