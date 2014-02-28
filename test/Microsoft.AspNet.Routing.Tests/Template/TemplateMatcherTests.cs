// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    public class TemplateMatcherTests
    {
        [Fact]
        public void MatchSingleRoute()
        {
            // Arrange
            var matcher = CreateMatcher("{controller}/{action}/{id}");

            // Act
            var match = matcher.Match("Bank/DoAction/123", null);

            // Assert
            Assert.NotNull(match);
            Assert.Equal("Bank", match["controller"]);
            Assert.Equal("DoAction", match["action"]);
            Assert.Equal("123", match["id"]);
        }

        [Fact]
        public void NoMatchSingleRoute()
        {
            // Arrange
            var matcher = CreateMatcher("{controller}/{action}/{id}");

            // Act
            var match = matcher.Match("Bank/DoAction", null);

            // Assert
            Assert.Null(match);
        }

        [Fact]
        public void MatchSingleRouteWithDefaults()
        {
            // Arrange
            var matcher = CreateMatcher("{controller}/{action}/{id}");

            // Act
            var rd = matcher.Match("Bank/DoAction", new RouteValueDictionary(new { id = "default id" }));

            // Assert
            Assert.Equal("Bank", rd["controller"]);
            Assert.Equal("DoAction", rd["action"]);
            Assert.Equal("default id", rd["id"]);
        }

        [Fact]
        public void NoMatchSingleRouteWithDefaults()
        {
            // Arrange
            var matcher = CreateMatcher("{controller}/{action}/{id}");

            // Act
            var rd = matcher.Match("Bank", new RouteValueDictionary(new { id = "default id" }));

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchRouteWithLiterals()
        {
            // Arrange
            var matcher = CreateMatcher("moo/{p1}/bar/{p2}");

            // Act
            var rd = matcher.Match("moo/111/bar/222", new RouteValueDictionary(new { p2 = "default p2" }));

            // Assert
            Assert.Equal("111", rd["p1"]);
            Assert.Equal("222", rd["p2"]);
        }

        [Fact]
        public void MatchRouteWithLiteralsAndDefaults()
        {
            // Arrange
            var matcher = CreateMatcher("moo/{p1}/bar/{p2}");

            // Act
            var rd = matcher.Match("moo/111/bar/", new RouteValueDictionary(new { p2 = "default p2" }));

            // Assert
            Assert.Equal("111", rd["p1"]);
            Assert.Equal("default p2", rd["p2"]);
        }

        [Fact]
        public void MatchRouteWithOnlyLiterals()
        {
            // Arrange
            var matcher = CreateMatcher("moo/bar");

            // Act
            var rd = matcher.Match("moo/bar", null);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(0, rd.Count);
        }

        [Fact]
        public void NoMatchRouteWithOnlyLiterals()
        {
            // Arrange
            var matcher = CreateMatcher("moo/bars");

            // Act
            var rd = matcher.Match("moo/bar", null);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchRouteWithExtraSeparators()
        {
            // Arrange
            var matcher = CreateMatcher("moo/bar");

            // Act
            var rd = matcher.Match("moo/bar/", null);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(0, rd.Count);
        }

        [Fact]
        public void MatchRouteUrlWithExtraSeparators()
        {
            // Arrange
            var matcher = CreateMatcher("moo/bar/");

            // Act
            var rd = matcher.Match("moo/bar", null);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(0, rd.Count);
        }

        [Fact]
        public void MatchRouteUrlWithParametersAndExtraSeparators()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}/{p2}/");

            // Act
            var rd = matcher.Match("moo/bar", null);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal("moo", rd["p1"]);
            Assert.Equal("bar", rd["p2"]);
        }

        [Fact]
        public void NoMatchRouteUrlWithDifferentLiterals()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}/{p2}/baz");

            // Act
            var rd = matcher.Match("moo/bar/boo", null);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void NoMatchLongerUrl()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}");

            // Act
            var rd = matcher.Match("moo/bar", null);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchSimpleFilename()
        {
            // Arrange
            var matcher = CreateMatcher("DEFAULT.ASPX");

            // Act
            var rd = matcher.Match("default.aspx", null);

            // Assert
            Assert.NotNull(rd);
        }

        [Theory]
        [InlineData("{prefix}x{suffix}", "xxxxxxxxxx")]
        [InlineData("{prefix}xyz{suffix}", "xxxxyzxyzxxxxxxyz")]
        [InlineData("{prefix}xyz{suffix}", "abcxxxxyzxyzxxxxxxyzxx")]
        [InlineData("{prefix}xyz{suffix}", "xyzxyzxyzxyzxyz")]
        [InlineData("{prefix}xyz{suffix}", "xyzxyzxyzxyzxyz1")]
        [InlineData("{prefix}xyz{suffix}", "xyzxyzxyz")]
        [InlineData("{prefix}aa{suffix}", "aaaaa")]
        [InlineData("{prefix}aaa{suffix}", "aaaaa")]
        public void VerifyRouteMatchesWithContext(string template, string path)
        {
            var matcher = CreateMatcher(template);

            // Act
            var rd = matcher.Match(path, null);

            // Assert
            Assert.NotNull(rd);
        }

        [Fact]
        public void MatchRouteWithExtraDefaultValues()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}/{p2}");

            // Act
            var rd = matcher.Match("v1", new RouteValueDictionary(new { p2 = (string)null, foo = "bar" }));

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(3, rd.Count);
            Assert.Equal("v1", rd["p1"]);
            Assert.Null(rd["p2"]);
            Assert.Equal("bar", rd["foo"]);
        }

        [Fact]
        public void MatchPrettyRouteWithExtraDefaultValues()
        {
            // Arrange
            var matcher = CreateMatcher("date/{y}/{m}/{d}");

            // Act
            var rd = matcher.Match("date/2007/08", new RouteValueDictionary(new { controller = "blog", action = "showpost", m = (string)null, d = (string)null }));

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(5, rd.Count);
            Assert.Equal("blog", rd["controller"]);
            Assert.Equal("showpost", rd["action"]);
            Assert.Equal("2007", rd["y"]);
            Assert.Equal("08", rd["m"]);
            Assert.Null(rd["d"]);
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnBothEndsMatches()
        {
            RunTest(
                "language/{lang}-{region}",
                "language/en-US",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnLeftEndMatches()
        {
            RunTest(
                "language/{lang}-{region}a",
                "language/en-USa",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnRightEndMatches()
        {
            RunTest(
                "language/a{lang}-{region}",
                "language/aen-US",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnNeitherEndMatches()
        {
            RunTest(
                "language/a{lang}-{region}a",
                "language/aen-USa",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnNeitherEndDoesNotMatch()
        {
            RunTest(
                "language/a{lang}-{region}a",
                "language/a-USa",
                null,
                null);
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnNeitherEndDoesNotMatch2()
        {
            RunTest(
                "language/a{lang}-{region}a",
                "language/aen-a",
                null,
                null);
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnBothEndsMatches()
        {
            RunTest(
                "language/{lang}",
                "language/en",
                null,
                new RouteValueDictionary(new { lang = "en" }));
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnBothEndsTrailingSlashDoesNotMatch()
        {
            RunTest(
                "language/{lang}",
                "language/",
                null,
                null);
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnBothEndsDoesNotMatch()
        {
            RunTest(
                "language/{lang}",
                "language",
                null,
                null);
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnLeftEndMatches()
        {
            RunTest(
                "language/{lang}-",
                "language/en-",
                null,
                new RouteValueDictionary(new { lang = "en" }));
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnRightEndMatches()
        {
            RunTest(
                "language/a{lang}",
                "language/aen",
                null,
                new RouteValueDictionary(new { lang = "en" }));
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnNeitherEndMatches()
        {
            RunTest(
                "language/a{lang}a",
                "language/aena",
                null,
                new RouteValueDictionary(new { lang = "en" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentStandardMvcRouteMatches()
        {
            RunTest(
                "{controller}.mvc/{action}/{id}",
                "home.mvc/index",
                new RouteValueDictionary(new { action = "Index", id = (string)null }),
                new RouteValueDictionary(new { controller = "home", action = "index", id = (string)null }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnBothEndsWithDefaultValuesMatches()
        {
            RunTest(
                "language/{lang}-{region}",
                "language/-",
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                null);
        }

        [Fact]
        public void GetRouteDataWithUrlWithMultiSegmentWithRepeatedDots()
        {
            RunTest(
                "{Controller}..mvc/{id}/{Param1}",
                "Home..mvc/123/p1",
                null,
                new RouteValueDictionary(new { Controller = "Home", id = "123", Param1 = "p1" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithTwoRepeatedDots()
        {
            RunTest(
                "{Controller}.mvc/../{action}",
                "Home.mvc/../index",
                null,
                new RouteValueDictionary(new { Controller = "Home", action = "index" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithThreeRepeatedDots()
        {
            RunTest(
                "{Controller}.mvc/.../{action}",
                "Home.mvc/.../index",
                null,
                new RouteValueDictionary(new { Controller = "Home", action = "index" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithManyRepeatedDots()
        {
            RunTest(
                "{Controller}.mvc/../../../{action}",
                "Home.mvc/../../../index",
                null,
                new RouteValueDictionary(new { Controller = "Home", action = "index" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithExclamationPoint()
        {
            RunTest(
                "{Controller}.mvc!/{action}",
                "Home.mvc!/index",
                null,
                new RouteValueDictionary(new { Controller = "Home", action = "index" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithStartingDotDotSlash()
        {
            RunTest(
                "../{Controller}.mvc",
                "../Home.mvc",
                null,
                new RouteValueDictionary(new { Controller = "Home" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithStartingBackslash()
        {
            RunTest(
                @"\{Controller}.mvc",
                @"\Home.mvc",
                null,
                new RouteValueDictionary(new { Controller = "Home" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithBackslashSeparators()
        {
            RunTest(
                @"{Controller}.mvc\{id}\{Param1}",
                @"Home.mvc\123\p1",
                null,
                new RouteValueDictionary(new { Controller = "Home", id = "123", Param1 = "p1" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithParenthesesLiterals()
        {
            RunTest(
                @"(Controller).mvc",
                @"(Controller).mvc",
                null,
                new RouteValueDictionary());
        }

        [Fact]
        public void GetRouteDataWithUrlWithTrailingSlashSpace()
        {
            RunTest(
                @"Controller.mvc/ ",
                @"Controller.mvc/ ",
                null,
                new RouteValueDictionary());
        }

        [Fact]
        public void GetRouteDataWithUrlWithTrailingSpace()
        {
            RunTest(
                @"Controller.mvc ",
                @"Controller.mvc ",
                null,
                new RouteValueDictionary());
        }

        [Fact]
        public void GetRouteDataWithCatchAllCapturesDots()
        {
            // DevDiv Bugs 189892: UrlRouting: Catch all parameter cannot capture url segments that contain the "."
            RunTest(
                "Home/ShowPilot/{missionId}/{*name}",
                "Home/ShowPilot/777/12345./foobar",
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
        public void RouteWithCatchAllClauseCapturesManySlashes()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}/{*p2}");

            // Act
            var rd = matcher.Match("v1/v2/v3", null);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Count);
            Assert.Equal("v1", rd["p1"]);
            Assert.Equal("v2/v3", rd["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllClauseCapturesTrailingSlash()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}/{*p2}");

            // Act
            var rd = matcher.Match("v1/", null);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Count);
            Assert.Equal("v1", rd["p1"]);
            Assert.Null(rd["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllClauseCapturesEmptyContent()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}/{*p2}");

            // Act
            var rd = matcher.Match("v1", null);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Count);
            Assert.Equal("v1", rd["p1"]);
            Assert.Null(rd["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllClauseUsesDefaultValueForEmptyContent()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}/{*p2}");

            // Act
            var rd = matcher.Match("v1", new RouteValueDictionary(new { p2 = "catchall" }));

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Count);
            Assert.Equal("v1", rd["p1"]);
            Assert.Equal("catchall", rd["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllClauseIgnoresDefaultValueForNonEmptyContent()
        {
            // Arrange
            var matcher = CreateMatcher("{p1}/{*p2}");

            // Act
            var rd = matcher.Match("v1/hello/whatever", new RouteValueDictionary(new { p2 = "catchall" }));

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Count);
            Assert.Equal("v1", rd["p1"]);
            Assert.Equal("hello/whatever", rd["p2"]);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchOnlyLeftLiteralMatch()
        {
            // DevDiv Bugs 191180: UrlRouting: Wrong template getting matched if a url segment is a substring of the requested url
            RunTest(
                "foo",
                "fooBAR",
                null,
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchOnlyRightLiteralMatch()
        {
            // DevDiv Bugs 191180: UrlRouting: Wrong template getting matched if a url segment is a substring of the requested url
            RunTest(
                "foo",
                "BARfoo",
                null,
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchMiddleLiteralMatch()
        {
            // DevDiv Bugs 191180: UrlRouting: Wrong template getting matched if a url segment is a substring of the requested url
            RunTest(
                "foo",
                "BARfooBAR",
                null,
                null);
        }

        [Fact]
        public void GetRouteDataDoesMatchesExactLiteralMatch()
        {
            // DevDiv Bugs 191180: UrlRouting: Wrong template getting matched if a url segment is a substring of the requested url
            RunTest(
                "foo",
                "foo",
                null,
                new RouteValueDictionary());
        }

        [Fact]
        public void GetRouteDataWithWeirdParameterNames()
        {
            RunTest(
                "foo/{ }/{.!$%}/{dynamic.data}/{op.tional}",
                "foo/space/weird/orderid",
                new RouteValueDictionary() { { " ", "not a space" }, { "op.tional", "default value" }, { "ran!dom", "va@lue" } },
                new RouteValueDictionary() { { " ", "space" }, { ".!$%", "weird" }, { "dynamic.data", "orderid" }, { "op.tional", "default value" }, { "ran!dom", "va@lue" } });
        }

        [Fact]
        public void GetRouteDataDoesNotMatchRouteWithLiteralSeparatorDefaultsButNoValue()
        {
            RunTest(
                "{controller}/{language}-{locale}",
                "foo",
                new RouteValueDictionary(new { language = "en", locale = "US" }),
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchesRouteWithLiteralSeparatorDefaultsAndLeftValue()
        {
            RunTest(
                "{controller}/{language}-{locale}",
                "foo/xx-",
                new RouteValueDictionary(new { language = "en", locale = "US" }),
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchesRouteWithLiteralSeparatorDefaultsAndRightValue()
        {
            RunTest(
                "{controller}/{language}-{locale}",
                "foo/-yy",
                new RouteValueDictionary(new { language = "en", locale = "US" }),
                null);
        }

        [Fact]
        public void GetRouteDataMatchesRouteWithLiteralSeparatorDefaultsAndValue()
        {
            RunTest(
                "{controller}/{language}-{locale}",
                "foo/xx-yy",
                new RouteValueDictionary(new { language = "en", locale = "US" }),
                new RouteValueDictionary { { "language", "xx" }, { "locale", "yy" }, { "controller", "foo" } });
        }

        [Fact]
        public void MatchSetsOptionalParameter()
        {
            // Arrange
            var route = CreateMatcher("{controller}/{action?}");
            var url = "Home/Index";

            // Act
            var match = route.Match(url, null);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(2, match.Values.Count);
            Assert.Equal("Home", match["controller"]);
            Assert.Equal("Index", match["action"]);
        }

        [Fact]
        public void MatchDoesNotSetOptionalParameter()
        {
            // Arrange
            var route = CreateMatcher("{controller}/{action?}");
            var url = "Home";

            // Act
            var match = route.Match(url, null);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(1, match.Values.Count);
            Assert.Equal("Home", match["controller"]);
            Assert.False(match.ContainsKey("action"));
        }

        [Fact]
        public void MatchDoesNotSetOptionalParameter_EmptyString()
        {
            // Arrange
            var route = CreateMatcher("{controller?}");
            var url = "";

            // Act
            var match = route.Match(url, null);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(0, match.Values.Count);
            Assert.False(match.ContainsKey("controller"));
        }

        [Fact]
        public void Match_EmptyRouteWith_EmptyString()
        {
            // Arrange
            var route = CreateMatcher("");
            var url = "";

            // Act
            var match = route.Match(url, null);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(0, match.Values.Count);
        }

        [Fact]
        public void MatchMultipleOptionalParameters()
        {
            // Arrange
            var route = CreateMatcher("{controller}/{action?}/{id?}");
            var url = "Home/Index";

            // Act
            var match = route.Match(url, null);

            // Assert
            Assert.NotNull(match);
            Assert.Equal(2, match.Values.Count);
            Assert.Equal("Home", match["controller"]);
            Assert.Equal("Index", match["action"]);
            Assert.False(match.ContainsKey("id"));
        }

        private TemplateMatcher CreateMatcher(string template)
        {
            return new TemplateMatcher(TemplateParser.Parse(template));
        }

        private static void RunTest(string template, string path, IDictionary<string, object> defaults, IDictionary<string, object> expected)
        {
            // Arrange
            var matcher = new TemplateMatcher(TemplateParser.Parse(template));

            // Act
            var match = matcher.Match(path, defaults);

            // Assert
            if (expected == null)
            {
                Assert.Null(match);
            }
            else
            {
                Assert.NotNull(match);
                Assert.Equal(expected.Count, match.Values.Count);
                foreach (string key in match.Keys)
                {
                    Assert.Equal(expected[key], match[key]);
                }
            }
        }
    }
}
