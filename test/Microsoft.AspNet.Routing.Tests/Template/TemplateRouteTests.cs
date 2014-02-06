// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.AspNet.Abstractions;
using Xunit;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    public class TemplateRouteTests
    {
        [Fact]
        public void GetRouteDataWithConstraintsThatIsNotStringThrows()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category/33");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = 5 }),
                null);

            // Act
            Assert.Throws<InvalidOperationException>(() => r.GetRouteData(context),
                "The constraint entry 'category' on the route with route template 'category/{category}' must have a string value or " +
                 "be of a type which implements 'ITemplateRouteConstraint'.");
        }


        [Fact]
        public void MatchSingleRoute()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/Bank/DoAction/123");
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Equal("Bank", rd.Values["controller"]);
            Assert.Equal("DoAction", rd.Values["action"]);
            Assert.Equal("123", rd.Values["id"]);
        }

        [Fact]
        public void NoMatchSingleRoute()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/Bank/DoAction");
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchSingleRouteWithDefaults()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/Bank/DoAction");
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", new RouteValueDictionary(new { id = "default id" }), null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Equal("Bank", rd.Values["controller"]);
            Assert.Equal("DoAction", rd.Values["action"]);
            Assert.Equal("default id", rd.Values["id"]);
        }

#if URLGENERATION

        [Fact]
        public void MatchSingleRouteWithEmptyDefaults()
        {
            IHttpVirtualPathData data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}", new RouteValueDictionary(new { val1 = "", val2 = "" }), new RouteValueDictionary(new { val2 = "SomeVal2" }));
            Assert.Null(data);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}", new RouteValueDictionary(new { val1 = "", val2 = "" }), new RouteValueDictionary(new { val1 = "a" }));
            Assert.Equal("Test/a", data.VirtualPath);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}", new RouteValueDictionary(new { val1 = "", val3 = "" }), new RouteValueDictionary(new { val2 = "a" }));
            Assert.Null(data);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}", new RouteValueDictionary(new { val1 = "", val2 = "" }), new RouteValueDictionary(new { val1 = "a", val2 = "b" }));
            Assert.Equal("Test/a/b", data.VirtualPath);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}", new RouteValueDictionary(new { val1 = "", val2 = "", val3 = "" }), new RouteValueDictionary(new { val1 = "a", val2 = "b", val3 = "c" }));
            Assert.Equal("Test/a/b/c", data.VirtualPath);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}", new RouteValueDictionary(new { val1 = "", val2 = "", val3 = "" }), new RouteValueDictionary(new { val1 = "a", val2 = "b" }));
            Assert.Equal("Test/a/b", data.VirtualPath);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}", new RouteValueDictionary(new { val1 = "", val2 = "", val3 = "" }), new RouteValueDictionary(new { val1 = "a" }));
            Assert.Equal("Test/a", data.VirtualPath);

        }

        private IHttpVirtualPathData GetVirtualPathFromRoute(string path, string template, RouteValueDictionary defaults, RouteValueDictionary values)
        {
            TemplateRoute r = CreateRoute(template, defaults, null);

            HttpContext context = GetHttpContext(path);
            return r.GetVirtualPath(context, values);   
        }
#endif

        [Fact]
        public void NoMatchSingleRouteWithDefaults()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/Bank");
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", new RouteValueDictionary(new { id = "default id" }), null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchRouteWithLiterals()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/111/bar/222");
            TemplateRoute r = CreateRoute("moo/{p1}/bar/{p2}", new RouteValueDictionary(new { p2 = "default p2" }), null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Equal("111", rd.Values["p1"]);
            Assert.Equal("222", rd.Values["p2"]);
        }

        [Fact]
        public void MatchRouteWithLiteralsAndDefaults()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/111/bar/");
            TemplateRoute r = CreateRoute("moo/{p1}/bar/{p2}", new RouteValueDictionary(new { p2 = "default p2" }), null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Equal("111", rd.Values["p1"]);
            Assert.Equal("default p2", rd.Values["p2"]);
        }

        [Fact]
        public void MatchRouteWithOnlyLiterals()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/bar");
            TemplateRoute r = CreateRoute("moo/bar", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(0, rd.Values.Count);
        }

        [Fact]
        public void NoMatchRouteWithOnlyLiterals()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/bar");
            TemplateRoute r = CreateRoute("moo/bars", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchRouteWithExtraSeparators()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/bar/");
            TemplateRoute r = CreateRoute("moo/bar", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(0, rd.Values.Count);
        }

        [Fact]
        public void MatchRouteUrlWithExtraSeparators()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/bar");
            TemplateRoute r = CreateRoute("moo/bar/", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(0, rd.Values.Count);
        }

        [Fact]
        public void MatchRouteUrlWithParametersAndExtraSeparators()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/bar");
            TemplateRoute r = CreateRoute("{p1}/{p2}/", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal("moo", rd.Values["p1"]);
            Assert.Equal("bar", rd.Values["p2"]);
        }

        [Fact]
        public void NoMatchRouteUrlWithDifferentLiterals()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/bar/boo");
            TemplateRoute r = CreateRoute("{p1}/{p2}/baz", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void NoMatchLongerUrl()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/bar");
            TemplateRoute r = CreateRoute("{p1}", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchSimpleFilename()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/default.aspx");
            TemplateRoute r = CreateRoute("DEFAULT.ASPX", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
        }

        private void VerifyRouteMatchesWithContext(string route, string requestUrl)
        {
            HttpContext context = GetHttpContext(requestUrl);
            TemplateRoute r = CreateRoute(route, null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
        }

        [Fact]
        public void MatchEvilRoute()
        {
            VerifyRouteMatchesWithContext("{prefix}x{suffix}", "~/xxxxxxxxxx");
            VerifyRouteMatchesWithContext("{prefix}xyz{suffix}", "~/xxxxyzxyzxxxxxxyz");
            VerifyRouteMatchesWithContext("{prefix}xyz{suffix}", "~/abcxxxxyzxyzxxxxxxyzxx");
            VerifyRouteMatchesWithContext("{prefix}xyz{suffix}", "~/xyzxyzxyzxyzxyz");
            VerifyRouteMatchesWithContext("{prefix}xyz{suffix}", "~/xyzxyzxyzxyzxyz1");
            VerifyRouteMatchesWithContext("{prefix}xyz{suffix}", "~/xyzxyzxyz");
            VerifyRouteMatchesWithContext("{prefix}aa{suffix}", "~/aaaaa");
            VerifyRouteMatchesWithContext("{prefix}aaa{suffix}", "~/aaaaa");
        }

        [Fact]
        public void MatchRouteWithExtraDefaultValues()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1");
            TemplateRoute r = CreateRoute("{p1}/{p2}", new RouteValueDictionary(new { p2 = (string)null, foo = "bar" }), null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(3, rd.Values.Count);
            Assert.Equal("v1", rd.Values["p1"]);
            Assert.Null(rd.Values["p2"]);
            Assert.Equal("bar", rd.Values["foo"]);
        }

        [Fact]
        public void MatchPrettyRouteWithExtraDefaultValues()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/date/2007/08");
            TemplateRoute r = CreateRoute(
                "date/{y}/{m}/{d}",
                new RouteValueDictionary(new { controller = "blog", action = "showpost", m = (string)null, d = (string)null }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(5, rd.Values.Count);
            Assert.Equal("blog", rd.Values["controller"]);
            Assert.Equal("showpost", rd.Values["action"]);
            Assert.Equal("2007", rd.Values["y"]);
            Assert.Equal("08", rd.Values["m"]);
            Assert.Null(rd.Values["d"]);
        }

        [Fact]
        public void GetRouteDataWhenConstraintsMatchesExactlyReturnsMatch()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category/12");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"\d\d" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(3, rd.Values.Count);
            Assert.Equal("store", rd.Values["controller"]);
            Assert.Equal("showcat", rd.Values["action"]);
            Assert.Equal("12", rd.Values["category"]);
        }

        [Fact]
        public void GetRouteDataShouldApplyRegExModifiersCorrectly1()
        {
            // DevDiv Bugs 173408: UrlRouting: Route validation doesn't handle ^ and $ correctly

            // Arrange
            HttpContext context = GetHttpContext("~/category/FooBar");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"Foo|Bar" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void GetRouteDataShouldApplyRegExModifiersCorrectly2()
        {
            // DevDiv Bugs 173408: UrlRouting: Route validation doesn't handle ^ and $ correctly

            // Arrange
            HttpContext context = GetHttpContext("~/category/Food");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"Foo|Bar" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void GetRouteDataShouldApplyRegExModifiersCorrectly3()
        {
            // DevDiv Bugs 173408: UrlRouting: Route validation doesn't handle ^ and $ correctly

            // Arrange
            HttpContext context = GetHttpContext("~/category/Bar");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"Foo|Bar" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(3, rd.Values.Count);
            Assert.Equal("store", rd.Values["controller"]);
            Assert.Equal("showcat", rd.Values["action"]);
            Assert.Equal("Bar", rd.Values["category"]);
        }

        [Fact]
        public void GetRouteDataWithCaseInsensitiveConstraintsMatches()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category/aBc");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"[a-z]{3}" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(3, rd.Values.Count);
            Assert.Equal("store", rd.Values["controller"]);
            Assert.Equal("showcat", rd.Values["action"]);
            Assert.Equal("aBc", rd.Values["category"]);
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnBothEndsMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/{lang}-{region}", null),
                "language/en-US",
                new RouteValueDictionary(new { lang = "en", region = "US" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnLeftEndMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/{lang}-{region}a", null),
                "language/en-USa",
                new RouteValueDictionary(new { lang = "en", region = "US" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnRightEndMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/a{lang}-{region}", null),
                "language/aen-US",
                new RouteValueDictionary(new { lang = "en", region = "US" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnNeitherEndMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/a{lang}-{region}a", null),
                "language/aen-USa",
                new RouteValueDictionary(new { lang = "en", region = "US" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnNeitherEndDoesNotMatch()
        {
            GetRouteDataHelper(
                CreateRoute("language/a{lang}-{region}a", null),
                "language/a-USa",
                null);
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnNeitherEndDoesNotMatch2()
        {
            GetRouteDataHelper(
                CreateRoute("language/a{lang}-{region}a", null),
                "language/aen-a",
                null);
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnBothEndsMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/{lang}", null),
                "language/en",
                new RouteValueDictionary(new { lang = "en" }));
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnBothEndsTrailingSlashDoesNotMatch()
        {
            GetRouteDataHelper(
                CreateRoute("language/{lang}", null),
                "language/",
                null);
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnBothEndsDoesNotMatch()
        {
            GetRouteDataHelper(
                CreateRoute("language/{lang}", null),
                "language",
                null);
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnLeftEndMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/{lang}-", null),
                "language/en-",
                new RouteValueDictionary(new { lang = "en" }));
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnRightEndMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/a{lang}", null),
                "language/aen",
                new RouteValueDictionary(new { lang = "en" }));
        }

        [Fact]
        public void GetRouteDataWithSimpleMultiSegmentParamsOnNeitherEndMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/a{lang}a", null),
                "language/aena",
                new RouteValueDictionary(new { lang = "en" }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentStandardMvcRouteMatches()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}.mvc/{action}/{id}", new RouteValueDictionary(new { action = "Index", id = (string)null })),
                "home.mvc/index",
                new RouteValueDictionary(new { controller = "home", action = "index", id = (string)null }));
        }

        [Fact]
        public void GetRouteDataWithMultiSegmentParamsOnBothEndsWithDefaultValuesMatches()
        {
            GetRouteDataHelper(
                CreateRoute("language/{lang}-{region}", new RouteValueDictionary(new { lang = "xx", region = "yy" }), null),
                "language/-",
                null);
        }

#if URLGENERATION

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnBothEndsMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/{lang}-{region}", null),
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                "language/xx-yy");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnLeftEndMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/{lang}-{region}a", null),
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                "language/xx-yya");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnRightEndMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/a{lang}-{region}", null),
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                "language/axx-yy");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnNeitherEndMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/a{lang}-{region}a", null),
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                "language/axx-yya");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnNeitherEndDoesNotMatch()
        {
            GetVirtualPathHelper(
                CreateRoute("language/a{lang}-{region}a", null),
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "", region = "yy" }),
                null);
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnNeitherEndDoesNotMatch2()
        {
            GetVirtualPathHelper(
                CreateRoute("language/a{lang}-{region}a", null),
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "" }),
                null);
        }

        [Fact]
        public void GetVirtualPathWithSimpleMultiSegmentParamsOnBothEndsMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/{lang}", null),
                new RouteValueDictionary(new { lang = "en" }),
                new RouteValueDictionary(new { lang = "xx" }),
                "language/xx");
        }

        [Fact]
        public void GetVirtualPathWithSimpleMultiSegmentParamsOnLeftEndMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/{lang}-", null),
                new RouteValueDictionary(new { lang = "en" }),
                new RouteValueDictionary(new { lang = "xx" }),
                "language/xx-");
        }

        [Fact]
        public void GetVirtualPathWithSimpleMultiSegmentParamsOnRightEndMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/a{lang}", null),
                new RouteValueDictionary(new { lang = "en" }),
                new RouteValueDictionary(new { lang = "xx" }),
                "language/axx");
        }

        [Fact]
        public void GetVirtualPathWithSimpleMultiSegmentParamsOnNeitherEndMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/a{lang}a", null),
                new RouteValueDictionary(new { lang = "en" }),
                new RouteValueDictionary(new { lang = "xx" }),
                "language/axxa");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentStandardMvcRouteMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("{controller}.mvc/{action}/{id}", new RouteValueDictionary(new { action = "Index", id = (string)null })),
                new RouteValueDictionary(new { controller = "home", action = "list", id = (string)null }),
                new RouteValueDictionary(new { controller = "products" }),
                "products.mvc");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnBothEndsWithDefaultValuesMatches()
        {
            GetVirtualPathHelper(
                CreateRoute("language/{lang}-{region}", new RouteValueDictionary(new { lang = "xx", region = "yy" }), null),
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "zz" }),
                "language/zz-yy");
        }

#endif

        [Fact]
        public void GetRouteDataWithUrlWithMultiSegmentWithRepeatedDots()
        {
            GetRouteDataHelper(
                CreateRoute("{Controller}..mvc/{id}/{Param1}", null),
                "Home..mvc/123/p1",
                new RouteValueDictionary(new { Controller = "Home", id = "123", Param1 = "p1" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithTwoRepeatedDots()
        {
            GetRouteDataHelper(
                CreateRoute("{Controller}.mvc/../{action}", null),
                "Home.mvc/../index",
                new RouteValueDictionary(new { Controller = "Home", action = "index" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithThreeRepeatedDots()
        {
            GetRouteDataHelper(
                CreateRoute("{Controller}.mvc/.../{action}", null),
                "Home.mvc/.../index",
                new RouteValueDictionary(new { Controller = "Home", action = "index" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithManyRepeatedDots()
        {
            GetRouteDataHelper(
                CreateRoute("{Controller}.mvc/../../../{action}", null),
                "Home.mvc/../../../index",
                new RouteValueDictionary(new { Controller = "Home", action = "index" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithExclamationPoint()
        {
            GetRouteDataHelper(
                CreateRoute("{Controller}.mvc!/{action}", null),
                "Home.mvc!/index",
                new RouteValueDictionary(new { Controller = "Home", action = "index" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithStartingDotDotSlash()
        {
            GetRouteDataHelper(
                CreateRoute("../{Controller}.mvc", null),
                "../Home.mvc",
                new RouteValueDictionary(new { Controller = "Home" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithStartingBackslash()
        {
            GetRouteDataHelper(
                CreateRoute(@"\{Controller}.mvc", null),
                @"\Home.mvc",
                new RouteValueDictionary(new { Controller = "Home" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithBackslashSeparators()
        {
            GetRouteDataHelper(
                CreateRoute(@"{Controller}.mvc\{id}\{Param1}", null),
                @"Home.mvc\123\p1",
                new RouteValueDictionary(new { Controller = "Home", id = "123", Param1 = "p1" }));
        }

        [Fact]
        public void GetRouteDataWithUrlWithParenthesesLiterals()
        {
            GetRouteDataHelper(
                CreateRoute(@"(Controller).mvc", null),
                @"(Controller).mvc",
                new RouteValueDictionary());
        }

        [Fact]
        public void GetRouteDataWithUrlWithTrailingSlashSpace()
        {
            GetRouteDataHelper(
                CreateRoute(@"Controller.mvc/ ", null),
                @"Controller.mvc/ ",
                new RouteValueDictionary());
        }

        [Fact]
        public void GetRouteDataWithUrlWithTrailingSpace()
        {
            GetRouteDataHelper(
                CreateRoute(@"Controller.mvc ", null),
                @"Controller.mvc ",
                new RouteValueDictionary());
        }

        [Fact]
        public void GetRouteDataWithCatchAllCapturesDots()
        {
            // DevDiv Bugs 189892: UrlRouting: Catch all parameter cannot capture url segments that contain the "."
            GetRouteDataHelper(
                CreateRoute(
                    "Home/ShowPilot/{missionId}/{*name}",
                    new RouteValueDictionary(new
                    {
                        controller = "Home",
                        action = "ShowPilot",
                        missionId = (string)null,
                        name = (string)null
                    }),
                    null),
                "Home/ShowPilot/777/12345./foobar",
                new RouteValueDictionary(new { controller = "Home", action = "ShowPilot", missionId = "777", name = "12345./foobar" }));
        }

        [Fact]
        public void GetRouteDataWhenConstraintsMatchesPartiallyDoesNotMatch()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category/a12");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"\d\d" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void GetRouteDataWhenConstraintsDoesNotMatch()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category/ab");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"\d\d" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void GetRouteDataWhenOneOfMultipleConstraintsDoesNotMatch()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category/01/q");
            TemplateRoute r = CreateRoute(
                "category/{category}/{sort}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"\d\d", sort = @"a|d" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void GetRouteDataWithNonStringValueReturnsTrueIfMatches()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category");
            TemplateRoute r = CreateRoute(
                "category/{foo}",
                new RouteValueDictionary(new { controller = "store", action = "showcat", foo = 123 }),
                new RouteValueDictionary(new { foo = @"\d{3}" }));

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
        }

        [Fact]
        public void GetRouteDataWithNonStringValueReturnsFalseIfUnmatched()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category");
            TemplateRoute r = CreateRoute(
                "category/{foo}",
                new RouteValueDictionary(new { controller = "store", action = "showcat", foo = 123 }),
                new RouteValueDictionary(new { foo = @"\d{2}" }));

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

#if URLGENERATION
        [Fact]
        public void GetUrlWithDefaultValue()
        {
            // URL should be found but excluding the 'id' parameter, which has only a default value.
            GetVirtualPathHelper(
               CreateRoute("{controller}/{action}/{id}", 
               new RouteValueDictionary(new { id = "defaultid" }), null),
               new RouteValueDictionary(new { controller = "home", action = "oldaction" }),
               new RouteValueDictionary(new { action = "newaction" }),
               "home/newaction");
        }

        [Fact]
        public void GetVirtualPathWithEmptyStringRequiredValueReturnsNull()
        {
            GetVirtualPathHelper(
               CreateRoute("foo/{controller}", null),
               new RouteValueDictionary(new { }),
               new RouteValueDictionary(new { controller = "" }),
               null);
        }

        [Fact]
        public void GetVirtualPathWithNullRequiredValueReturnsNull()
        {
            GetVirtualPathHelper(
               CreateRoute("foo/{controller}", null),
               new RouteValueDictionary(new { }),
               new RouteValueDictionary(new { controller = (string)null }),
               null);
        }

        [Fact]
        public void GetVirtualPathWithRequiredValueReturnsPath()
        {
            GetVirtualPathHelper(
               CreateRoute("foo/{controller}", null),
               new RouteValueDictionary(new { }),
               new RouteValueDictionary(new { controller = "home" }),
               "foo/home");
        }

        [Fact]
        public void GetUrlWithNullDefaultValue()
        {
            // URL should be found but excluding the 'id' parameter, which has only a default value.
            GetVirtualPathHelper(
               CreateRoute(
                   "{controller}/{action}/{id}", 
                   new RouteValueDictionary(new { id = (string)null }),
                   null),
               new RouteValueDictionary(new { controller = "home", action = "oldaction", id = (string)null }),
               new RouteValueDictionary(new { action = "newaction" }),
               "home/newaction");
        }

        [Fact]
        public void GetUrlWithMissingValuesDoesntMatch()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", null);

            var rd = CreateRouteData();
            rd.Values.Add("controller", "home");
            rd.Values.Add("action", "oldaction");
            var valuesDictionary = CreateRouteValueDictionary();
            valuesDictionary.Add("action", "newaction");

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.Null(vpd);
        }

        [Fact]
        public void GetUrlWithValuesThatAreCompletelyDifferentFromTheCurrenIRoute()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            IRouteCollection rt = new DefaultRouteCollection();
            rt.Add(CreateRoute("date/{y}/{m}/{d}", null));
            rt.Add(CreateRoute("{controller}/{action}/{id}", null));

            var rd = CreateRouteData();
            rd.Values.Add("controller", "home");
            rd.Values.Add("action", "dostuff");

            var values = CreateRouteValueDictionary();
            values.Add("y", "2007");
            values.Add("m", "08");
            values.Add("d", "12");

            // Act
            var vpd = rt.GetVirtualPath(context, values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("/app/date/2007/08/12", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithValuesThatAreCompletelyDifferentFromTheCurrentRouteAsSecondRoute()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);

            IRouteCollection rt = new DefaultRouteCollection();
            rt.Add(CreateRoute("{controller}/{action}/{id}"));
            rt.Add(CreateRoute("date/{y}/{m}/{d}"));

            var rd = CreateRouteData();
            rd.Values.Add("controller", "home");
            rd.Values.Add("action", "dostuff");

            var values = CreateRouteValueDictionary();
            values.Add("y", "2007");
            values.Add("m", "08");
            values.Add("d", "12");

            // Act
            var vpd = rt.GetVirtualPath(context, values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("/app/date/2007/08/12", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithEmptyRequiredValuesReturnsNull()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("{p1}/{p2}/{p3}", new RouteValueDictionary(), null);

            var rd = CreateRouteData();
            rd.Values.Add("p1", "v1");

            var valuesDictionary = CreateRouteValueDictionary();
            valuesDictionary.Add("p2", "");
            valuesDictionary.Add("p3", "");

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.Null(vpd);
        }

        [Fact]
        public void GetUrlWithEmptyOptionalValuesReturnsShortUrl()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("{p1}/{p2}/{p3}", new RouteValueDictionary(new { p2 = "d2", p3 = "d3", }), null);

            var rd = CreateRouteData();
            rd.Values.Add("p1", "v1");
            var valuesDictionary = CreateRouteValueDictionary();
            valuesDictionary.Add("p2", "");
            valuesDictionary.Add("p3", "");

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("v1", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlShouldIgnoreValuesAfterChangedParameter()
        {
            // DevDiv Bugs 157535

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "orig");
            rd.Values.Add("action", "init");
            rd.Values.Add("id", "123");

            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", new RouteValueDictionary(new { action = "Index", id = (string)null }), null);

            var valuesDictionary = CreateRouteValueDictionary();
            valuesDictionary.Add("action", "new");

            // Act
            var vpd = r.GetVirtualPath(GetHttpContext("/app1", "", ""), valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("orig/new", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithRouteThatHasExtensionWithSubsequentDefaultValueIncludesExtensionButNotDefaultValue()
        {
            // DevDiv Bugs 156606

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "Bank");
            rd.Values.Add("action", "MakeDeposit");
            rd.Values.Add("accountId", "7770");

            IRouteCollection rc = new DefaultRouteCollection();
            rc.Add(CreateRoute(
                "{controller}.mvc/Deposit/{accountId}",
                new RouteValueDictionary(new { Action = "DepositView" })));

            // Note: This route was in the original bug, but it turns out that this behavior is incorrect. With the
            // recent fix to Route (in this changelist) this route would have been selected since we have values for
            // all three required parameters.
            //rc.Add(new Route {
            //    Url = "{controller}.mvc/{action}/{accountId}",
            //    RouteHandler = new DummyRouteHandler()
            //});

            // This route should be chosen because the requested action is List. Since the default value of the action
            // is List then the Action should not be in the URL. However, the file extension should be included since
            // it is considered "safe."
            rc.Add(CreateRoute(
                "{controller}.mvc/{action}",
                new RouteValueDictionary(new { Action = "List" })));

            var values = CreateRouteValueDictionary();
            values.Add("Action", "List");

            // Act
            var vpd = rc.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("/app1/Bank.mvc", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithRouteThatHasDifferentControllerCaseShouldStillMatch()
        {
            // DevDiv Bugs 159099

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "Bar");
            rd.Values.Add("action", "bbb");
            rd.Values.Add("id", null);

            IRouteCollection rc = new DefaultRouteCollection();
            rc.Add(CreateRoute("PrettyFooUrl", new RouteValueDictionary(new { controller = "Foo", action = "aaa", id = (string)null })));

            rc.Add(CreateRoute("PrettyBarUrl", new RouteValueDictionary(new { controller = "Bar", action = "bbb", id = (string)null })));

            rc.Add(CreateRoute("{controller}/{action}/{id}", new RouteValueDictionary(new { action = "Index", id = (string)null })));

            var values = CreateRouteValueDictionary();
            values.Add("Action", "aaa");
            values.Add("Controller", "foo");

            // Act
            var vpd = rc.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("/app1/PrettyFooUrl", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithNoChangedValuesShouldProduceSameUrl()
        {
            // DevDiv Bugs 159469

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "Home");
            rd.Values.Add("action", "Index");
            rd.Values.Add("id", null);

            IRouteCollection rc = new DefaultRouteCollection();
            rc.Add(CreateRoute("{controller}.mvc/{action}/{id}", new RouteValueDictionary(new { action = "Index", id = (string)null })));

            rc.Add(CreateRoute("{controller}/{action}/{id}", new RouteValueDictionary(new { action = "Index", id = (string)null })));

            var values = CreateRouteValueDictionary();
            values.Add("Action", "Index");

            // Act
            var vpd = rc.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("/app1/Home.mvc", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlAppliesConstraintsRulesToChooseRoute()
        {
            // DevDiv Bugs 159678: MVC: URL generation chooses the wrong route for generating URLs when route validation is in place

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "Home");
            rd.Values.Add("action", "Index");
            rd.Values.Add("id", null);

            IRouteCollection rc = new DefaultRouteCollection();
            rc.Add(CreateRoute(
                "foo.mvc/{action}", 
                new RouteValueDictionary(new { controller = "Home" }), 
                new RouteValueDictionary(new { controller = "Home", action = "Contact", httpMethod = CreateHttpMethodConstraint("get") })));

            rc.Add(CreateRoute(
                "{controller}.mvc/{action}",
                new RouteValueDictionary(new { action = "Index" }),
                new RouteValueDictionary(new { controller = "Home", action = "(Index|About)", httpMethod = CreateHttpMethodConstraint("post") })));

            var values = CreateRouteValueDictionary();
            values.Add("Action", "Index");

            // Act
            var vpd = rc.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("/app1/Home.mvc", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithNullForMiddleParameterIgnoresRemainingParameters()
        {
            // DevDiv Bugs 170859: UrlRouting: Passing null or empty string for a parameter in the middle of a route generates the wrong Url

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "UrlRouting");
            rd.Values.Add("action", "Play");
            rd.Values.Add("category", "Photos");
            rd.Values.Add("year", "2008");
            rd.Values.Add("occasion", "Easter");
            rd.Values.Add("SafeParam", "SafeParamValue");

            TemplateRoute r = CreateRoute(
                "UrlGeneration1/{controller}.mvc/{action}/{category}/{year}/{occasion}/{SafeParam}",
                new RouteValueDictionary(new { year = 1995, occasion = "Christmas", action = "Play", SafeParam = "SafeParamValue" }));

            // Act
            RouteValueDictionary values = CreateRouteValueDictionary();
            values.Add("year", null);
            values.Add("occasion", "Hola");
            var vpd = r.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("UrlGeneration1/UrlRouting.mvc/Play/Photos/1995/Hola", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlShouldValidateOnlyAcceptedParametersAndUserDefaultValuesForInvalidatedParameters()
        {
            // DevDiv Bugs 172913: UrlRouting: Parameter validation should not run against current request values if a new value has been supplied at a previous position

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("Controller", "UrlRouting");
            rd.Values.Add("Name", "MissmatchedValidateParams");
            rd.Values.Add("action", "MissmatchedValidateParameters2");
            rd.Values.Add("ValidateParam1", "special1");
            rd.Values.Add("ValidateParam2", "special2");

            IRouteCollection rc = new DefaultRouteCollection();
            rc.Add(CreateRoute(
                "UrlConstraints/Validation.mvc/Input5/{action}/{ValidateParam1}/{ValidateParam2}",
                new RouteValueDictionary(new { Controller = "UrlRouting", Name = "MissmatchedValidateParams", ValidateParam2 = "valid" }),
                new RouteValueDictionary(new { ValidateParam1 = "valid.*", ValidateParam2 = "valid.*" })));

            rc.Add(CreateRoute(
                "UrlConstraints/Validation.mvc/Input5/{action}/{ValidateParam1}/{ValidateParam2}",
                new RouteValueDictionary(new { Controller = "UrlRouting", Name = "MissmatchedValidateParams" }),
                new RouteValueDictionary(new { ValidateParam1 = "special.*", ValidateParam2 = "special.*" })));

            var values = CreateRouteValueDictionary();
            values.Add("Name", "MissmatchedValidateParams");
            values.Add("ValidateParam1", "valid1");

            // Act
            var vpd = rc.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("/app1/UrlConstraints/Validation.mvc/Input5/MissmatchedValidateParameters2/valid1", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithEmptyStringForMiddleParameterIgnoresRemainingParameters()
        {
            // DevDiv Bugs 170859: UrlRouting: Passing null or empty string for a parameter in the middle of a route generates the wrong Url

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "UrlRouting");
            rd.Values.Add("action", "Play");
            rd.Values.Add("category", "Photos");
            rd.Values.Add("year", "2008");
            rd.Values.Add("occasion", "Easter");
            rd.Values.Add("SafeParam", "SafeParamValue");

            TemplateRoute r = CreateRoute(
                "UrlGeneration1/{controller}.mvc/{action}/{category}/{year}/{occasion}/{SafeParam}",
                new RouteValueDictionary(new { year = 1995, occasion = "Christmas", action = "Play", SafeParam = "SafeParamValue" }));

            // Act
            RouteValueDictionary values = CreateRouteValueDictionary();
            values.Add("year", String.Empty);
            values.Add("occasion", "Hola");
            var vpd = r.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("UrlGeneration1/UrlRouting.mvc/Play/Photos/1995/Hola", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithEmptyStringForMiddleParameterShouldUseDefaultValue()
        {
            // DevDiv Bugs 172084: UrlRouting: Route.GetUrl generates the wrong route of new values has a different controller and route has an action parameter with default

            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("Controller", "Test");
            rd.Values.Add("Action", "Fallback");
            rd.Values.Add("param1", "fallback1");
            rd.Values.Add("param2", "fallback2");
            rd.Values.Add("param3", "fallback3");

            TemplateRoute r = CreateRoute(
                "{controller}.mvc/{action}/{param1}", 
                new RouteValueDictionary(new { Controller = "Test", Action = "Default" }));

            // Act
            RouteValueDictionary values = CreateRouteValueDictionary();
            values.Add("controller", "subtest");
            values.Add("param1", "b");
            // The original bug for this included this value, but with the new support for
            // creating query string values it changes the behavior such that the URL is
            // not what was originally expected. To preserve the general behavior of this
            // unit test the 'param2' value is no longer being added.
            //values.Add("param2", "a");
            var vpd = r.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("subtest.mvc/Default/b", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlVerifyEncoding()
        {
            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "Home");
            rd.Values.Add("action", "Index");
            rd.Values.Add("id", null);

            TemplateRoute r = CreateRoute(
                "{controller}.mvc/{action}/{id}", 
                new RouteValueDictionary(new { controller = "Home" }));

            // Act
            RouteValueDictionary values = CreateRouteValueDictionary();
            values.Add("controller", "#;?:@&=+$,");
            values.Add("action", "showcategory");
            values.Add("id", 123);
            values.Add("so?rt", "de?sc");
            values.Add("maxPrice", 100);
            var vpd = r.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("%23%3b%3f%3a%40%26%3d%2b%24%2c.mvc/showcategory/123?so%3Frt=de%3Fsc&maxPrice=100", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlGeneratesQueryStringForNewValuesAndEscapesQueryString()
        {
            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "Home");
            rd.Values.Add("action", "Index");
            rd.Values.Add("id", null);

            TemplateRoute r = CreateRoute(
                "{controller}.mvc/{action}/{id}", 
                new RouteValueDictionary(new { controller = "Home" }));

            // Act
            RouteValueDictionary values = CreateRouteValueDictionary();
            values.Add("controller", "products");
            values.Add("action", "showcategory");
            values.Add("id", 123);
            values.Add("so?rt", "de?sc");
            values.Add("maxPrice", 100);
            var vpd = r.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("products.mvc/showcategory/123?so%3Frt=de%3Fsc&maxPrice=100", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlGeneratesQueryStringForNewValuesButIgnoresNewValuesThatMatchDefaults()
        {
            // Arrange
            var rd = CreateRouteData();
            rd.Values.Add("controller", "Home");
            rd.Values.Add("action", "Index");
            rd.Values.Add("id", null);

            TemplateRoute r = CreateRoute("{controller}.mvc/{action}/{id}", new RouteValueDictionary(new { controller = "Home", Custom = "customValue" }));

            // Act
            RouteValueDictionary values = CreateRouteValueDictionary();
            values.Add("controller", "products");
            values.Add("action", "showcategory");
            values.Add("id", 123);
            values.Add("sort", "desc");
            values.Add("maxPrice", 100);
            values.Add("custom", "customValue");
            var vpd = r.GetVirtualPath(GetHttpContext("/app1", "", ""), values);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("products.mvc/showcategory/123?sort=desc&maxPrice=100", vpd.VirtualPath);
        }

        [Fact]
        public void GetVirtualPathEncodesParametersAndLiterals()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("bl%og/{controller}/he llo/{action}", null);
            var rd = CreateRouteData();
            rd.Values.Add("controller", "ho%me");
            rd.Values.Add("action", "li st");
            var valuesDictionary = CreateRouteValueDictionary();

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("bl%25og/ho%25me/he%20llo/li%20st", vpd.VirtualPath);
            Assert.Equal(r, vpd.Route);
        }

        [Fact]
        public void GetVirtualPathUsesCurrentValuesNotInRouteToMatch()
        {
            // DevDiv Bugs 177401: UrlRouting: Incorrect route picked on urlgeneration if using controller from ambient values and route does not have a url parameter for controller

            // DevDiv Bugs 191162: UrlRouting: Route does not match when an ambient route value doesn't match a required default value in the target route
            // Because of this bug the test was split into two separate verifications since the original test was verifying slightly incorrect behavior

            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r1 = CreateRoute(
                "ParameterMatching.mvc/{Action}/{product}",
                new RouteValueDictionary(new { Controller = "ParameterMatching", product = (string)null }),
                null);

            TemplateRoute r2 = CreateRoute(
                "{controller}.mvc/{action}",
                new RouteValueDictionary(new { Action = "List" }),
                new RouteValueDictionary(new { Controller = "Action|Bank|Overridden|DerivedFromAction|OverrideInvokeActionAndExecute|InvalidControllerName|Store|HtmlHelpers|(T|t)est|UrlHelpers|Custom|Parent|Child|TempData|ViewFactory|LocatingViews|AccessingDataInViews|ViewOverrides|ViewMasterPage|InlineCompileError|CustomView" }),
                null);

            var rd = CreateRouteData();
            rd.Values.Add("controller", "Bank");
            rd.Values.Add("Action", "List");
            var valuesDictionary = CreateRouteValueDictionary();
            valuesDictionary.Add("action", "AttemptLogin");

            // Act for first route
            var vpd = r1.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("ParameterMatching.mvc/AttemptLogin", vpd.VirtualPath);

            // Act for second route
            vpd = r2.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("Bank.mvc/AttemptLogin", vpd.VirtualPath);
        }

#endif
        [Fact]
        public void RouteWithCatchAllClauseCapturesManySlashes()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1/v2/v3");
            TemplateRoute r = CreateRoute("{p1}/{*p2}", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Values.Count);
            Assert.Equal("v1", rd.Values["p1"]);
            Assert.Equal("v2/v3", rd.Values["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllClauseCapturesTrailingSlash()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1/");
            TemplateRoute r = CreateRoute("{p1}/{*p2}", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Values.Count);
            Assert.Equal("v1", rd.Values["p1"]);
            Assert.Null(rd.Values["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllClauseCapturesEmptyContent()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1");
            TemplateRoute r = CreateRoute("{p1}/{*p2}", null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Values.Count);
            Assert.Equal("v1", rd.Values["p1"]);
            Assert.Null(rd.Values["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllClauseUsesDefaultValueForEmptyContent()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1");
            TemplateRoute r = CreateRoute("{p1}/{*p2}", new RouteValueDictionary(new { p2 = "catchall" }), null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Values.Count);
            Assert.Equal("v1", rd.Values["p1"]);
            Assert.Equal("catchall", rd.Values["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllClauseIgnoresDefaultValueForNonEmptyContent()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1/hello/whatever");
            TemplateRoute r = CreateRoute("{p1}/{*p2}", new RouteValueDictionary(new { p2 = "catchall" }), null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Values.Count);
            Assert.Equal("v1", rd.Values["p1"]);
            Assert.Equal("hello/whatever", rd.Values["p2"]);
        }

        [Fact]
        public void RouteWithCatchAllRejectsConstraints()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1/abcd");
            TemplateRoute r = CreateRoute(
                "{p1}/{*p2}",
                new RouteValueDictionary(new { p2 = "catchall" }),
                new RouteValueDictionary(new { p2 = "\\d{4}" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void RouteWithCatchAllAcceptsConstraints()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1/1234");
            TemplateRoute r = CreateRoute(
                "{p1}/{*p2}",
                new RouteValueDictionary(new { p2 = "catchall" }),
                new RouteValueDictionary(new { p2 = "\\d{4}" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Values.Count);
            Assert.Equal("v1", rd.Values["p1"]);
            Assert.Equal("1234", rd.Values["p2"]);
        }

#if URLGENERATION

        [Fact]
        public void GetUrlWithCatchAllWithValue()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("{p1}/{*p2}", new RouteValueDictionary(new { id = "defaultid" }), null);

            var rd = CreateRouteData();
            rd.Values.Add("p1", "v1");
            var valuesDictionary = CreateRouteValueDictionary();
            valuesDictionary.Add("p2", "v2a/v2b");

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("v1/v2a/v2b", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithCatchAllWithEmptyValue()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("{p1}/{*p2}", new RouteValueDictionary(new { id = "defaultid" }), null);

            var rd = CreateRouteData();
            rd.Values.Add("p1", "v1");

            var valuesDictionary = CreateRouteValueDictionary();
            valuesDictionary.Add("p2", "");

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("v1", vpd.VirtualPath);
        }

        [Fact]
        public void GetUrlWithCatchAllWithNullValue()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("{p1}/{*p2}", new RouteValueDictionary(new { id = "defaultid" }), null);

            var rd = CreateRouteData();
            rd.Values.Add("p1", "v1");
            var valuesDictionary = CreateRouteValueDictionary();
            valuesDictionary.Add("p2", null);

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("v1", vpd.VirtualPath);
        }

        [Fact]
        public void GetVirtualPathWithDataTokensCopiesThemFromRouteToVirtualPathData()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("{controller}/{action}", null, null, new RouteValueDictionary(new { foo = "bar", qux = "quux" }));

            var rd = CreateRouteData();
            rd.Values.Add("controller", "home");
            rd.Values.Add("action", "index");
            var valuesDictionary = CreateRouteValueDictionary();

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("home/index", vpd.VirtualPath);
            Assert.Equal(r, vpd.Route);
            Assert.Equal<int>(2, vpd.DataTokens.Count);
            Assert.Equal("bar", vpd.DataTokens["foo"]);
            Assert.Equal("quux", vpd.DataTokens["qux"]);
        }

        [Fact]
        public void GetVirtualPathWithValidCustomConstraints()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            CustomConstraintTemplateRoute r = new CustomConstraintTemplateRoute("{controller}/{action}", null, new RouteValueDictionary(new { action = 5 }));

            var rd = CreateRouteData();
            rd.Values.Add("controller", "home");
            rd.Values.Add("action", "index");

            var valuesDictionary = CreateRouteValueDictionary();

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("home/index", vpd.VirtualPath);
            Assert.Equal(r, vpd.Route);
            Assert.NotNull(r.ConstraintData);
            Assert.Equal(5, r.ConstraintData.Constraint);
            Assert.Equal("action", r.ConstraintData.ParameterName);
            Assert.Equal("index", r.ConstraintData.ParameterValue);
        }

        [Fact]
        public void GetVirtualPathWithInvalidCustomConstraints()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            CustomConstraintTemplateRoute r = new CustomConstraintTemplateRoute("{controller}/{action}", null, new RouteValueDictionary(new { action = 5 }));

            var rd = CreateRouteData();
            rd.Values.Add("controller", "home");
            rd.Values.Add("action", "list");

            var valuesDictionary = CreateRouteValueDictionary();

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.Null(vpd);
            Assert.NotNull(r.ConstraintData);
            Assert.Equal(5, r.ConstraintData.Constraint);
            Assert.Equal("action", r.ConstraintData.ParameterName);
            Assert.Equal("list", r.ConstraintData.ParameterValue);
        }

#if DATATOKENS

        [Fact]
        public void GetUrlWithCatchAllWithAmbientValue()
        {
            // Arrange
            HttpContext context = GetHttpContext("/app", null, null);
            TemplateRoute r = CreateRoute("{p1}/{*p2}", new RouteValueDictionary(new { id = "defaultid"  }), null, null);

            var rd = CreateRouteData();
            rd.Values.Add("p1", "v1");
            rd.Values.Add("p2", "ambient-catch-all");
            var valuesDictionary = CreateRouteValueDictionary();

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.NotNull(vpd);
            Assert.Equal<string>("v1/ambient-catch-all", vpd.VirtualPath);
            Assert.Equal(r, vpd.Route);
            Assert.Equal<int>(0, vpd.DataTokens.Count);
        }
#endif
#endif

#if DATATOKENS

        [Fact]
        public void GetRouteDataWithDataTokensCopiesThemFromRouteToIRouteData()
        {
            // Arrange
            HttpContext context = GetHttpContext(null, "~/category/33", null);
            TemplateRoute r = CreateRoute("category/{category}", null, null, new RouteValueDictionary(new { foo = "bar", qux = "quux" }));

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(1, rd.Values.Count);
            Assert.Equal<int>(2, rd.DataTokens.Count);
            Assert.Equal("33", rd.Values["category"]);
            Assert.Equal("bar", rd.DataTokens["foo"]);
            Assert.Equal("quux", rd.DataTokens["qux"]);
        }

#endif

        [Fact]
        public void GetRouteDataWithValidCustomConstraints()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/home/index");
            CustomConstraintTemplateRoute r = new CustomConstraintTemplateRoute("{controller}/{action}", null, new RouteValueDictionary(new { action = 5 }));

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Values.Count);
            Assert.Equal("home", rd.Values["controller"]);
            Assert.Equal("index", rd.Values["action"]);
            Assert.NotNull(r.ConstraintData);
            Assert.Equal(5, r.ConstraintData.Constraint);
            Assert.Equal("action", r.ConstraintData.ParameterName);
            Assert.Equal("index", r.ConstraintData.ParameterValue);
        }

        [Fact]
        public void GetRouteDataWithInvalidCustomConstraints()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/home/list");
            CustomConstraintTemplateRoute r = new CustomConstraintTemplateRoute("{controller}/{action}", null, new RouteValueDictionary(new { action = 5 }));

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
            Assert.NotNull(r.ConstraintData);
            Assert.Equal(5, r.ConstraintData.Constraint);
            Assert.Equal("action", r.ConstraintData.ParameterName);
            Assert.Equal("list", r.ConstraintData.ParameterValue);
        }

        [Fact]
        public void GetRouteDataWithConstraintIsCultureInsensitive()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/category/\u0130"); // Turkish upper-case dotted I
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { category = @"[a-z]+" }),
                null);

            // Act
            Thread currentThread = Thread.CurrentThread;
            CultureInfo backupCulture = currentThread.CurrentCulture;
            RouteMatch rd;
            try
            {
                currentThread.CurrentCulture = new CultureInfo("tr-TR"); // Turkish culture
                rd = r.GetRouteData(context);
            }
            finally
            {
                currentThread.CurrentCulture = backupCulture;
            }

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void GetRouteDataWithConstraintThatHasNoValueDoesNotMatch()
        {
            // Arrange
            HttpContext context = GetHttpContext(null, "~/category/33");
            TemplateRoute r = CreateRoute(
                "category/{category}",
                new RouteValueDictionary(new { controller = "store", action = "showcat" }),
                new RouteValueDictionary(new { foo = @"\d\d\d" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void GetRouteDataWithCatchAllConstraintThatHasNoValueDoesNotMatch()
        {
            // Arrange
            HttpContext context = GetHttpContext(null, "~/category");
            TemplateRoute r = CreateRoute(
                "category/{*therest}",
                null,
                new RouteValueDictionary(new { therest = @"hola" }),
                null);

            // Act
            var rd = r.GetRouteData(context);

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void ProcessConstraintShouldGetCalledForCustomConstraintDuringUrlGeneration()
        {
            // DevDiv Bugs 178588: UrlRouting: ProcessConstraint is not invoked on a custom constraint that is not mapped to a url parameter during urlgeneration

            // Arrange
            HttpContext context = GetHttpContext("/app", null);

            DevDivBugs178588CustomRoute r = new DevDivBugs178588CustomRoute(
                "CustomPath.mvc/{action}/{param1}/{param2}",
                new RouteValueDictionary(new { Controller = "Test" }),
                new RouteValueDictionary(new { foo = new DevDivBugs178588CustomConstraint() }));

            var rd = CreateRouteData();
            rd.Values.Add("action", "Test");
            rd.Values.Add("param1", "111");
            rd.Values.Add("param2", "222");
            rd.Values.Add("Controller", "Test");

            var valuesDictionary = CreateRouteValueDictionary();

            // Act
            var vpd = r.GetVirtualPath(context, valuesDictionary);

            // Assert
            Assert.Null(vpd);
        }

        [Fact]
        public void GetRouteDataMatchesEntireLiteralSegmentScenario1a()
        {
            TemplateRoute r = CreateRoute(
                "CatchAllParamsWithDefaults/{Controller}.mvc/{Action}/{*therest}",
                new RouteValueDictionary(new { therest = "Hello" }),
                new RouteValueDictionary(new { Controller = "CatchAllParams" }),
                null);

            // DevDiv Bugs 191180: UrlRouting: Wrong route getting matched if a url segment is a substring of the requested url
            // Scenario 1.a.
            GetRouteDataHelper(
                r,
                "CatchAllParamsWithDefaults/CatchAllParams.mvc/TestCatchAllParamInIRouteData",
                new RouteValueDictionary(new { Controller = "CatchAllParams", Action = "TestCatchAllParamInIRouteData", therest = "Hello" }));
        }

        [Fact]
        public void GetRouteDataMatchesEntireLiteralSegmentScenario1b()
        {
            TemplateRoute r = CreateRoute(
                "CatchAllParams/{Controller}.mvc/{Action}/{*therest}",
                null,
                new RouteValueDictionary(new { Controller = "CatchAllParams" }),
                null);

            // DevDiv Bugs 191180: UrlRouting: Wrong route getting matched if a url segment is a substring of the requested url
            // Scenario 1.b.
            GetRouteDataHelper(
                r,
                "CatchAllParamsWithDefaults/CatchAllParams.mvc/TestCatchAllParamInIRouteData",
                null);
        }

        [Fact]
        public void GetRouteDataMatchesEntireLiteralSegmentScenario2()
        {
            TemplateRoute r = CreateRoute(
                "{controller}.mvc/Login",
                new RouteValueDictionary(new { Action = "LoginView" }),
                new RouteValueDictionary(new { Controller = "Bank" }),
                null);

            // DevDiv Bugs 191180: UrlRouting: Wrong route getting matched if a url segment is a substring of the requested url
            // Scenario 2
            GetRouteDataHelper(
                r,
                "Bank.mvc/AttemptLogin",
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchOnlyLeftLiteralMatch()
        {
            TemplateRoute r = CreateRoute("foo", null);

            // DevDiv Bugs 191180: UrlRouting: Wrong route getting matched if a url segment is a substring of the requested url
            GetRouteDataHelper(
                r,
                "fooBAR",
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchOnlyRightLiteralMatch()
        {
            TemplateRoute r = CreateRoute("foo", null);

            // DevDiv Bugs 191180: UrlRouting: Wrong route getting matched if a url segment is a substring of the requested url
            GetRouteDataHelper(
                r,
                "BARfoo",
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchMiddleLiteralMatch()
        {
            TemplateRoute r = CreateRoute("foo", null);

            // DevDiv Bugs 191180: UrlRouting: Wrong route getting matched if a url segment is a substring of the requested url
            GetRouteDataHelper(
                r,
                "BARfooBAR",
                null);
        }

        [Fact]
        public void GetRouteDataDoesMatchesExactLiteralMatch()
        {
            TemplateRoute r = CreateRoute("foo", null);

            // DevDiv Bugs 191180: UrlRouting: Wrong route getting matched if a url segment is a substring of the requested url
            GetRouteDataHelper(
                r,
                "foo",
                new RouteValueDictionary());
        }

        [Fact]
        public void GetRouteDataWithWeirdParameterNames()
        {
            TemplateRoute r = CreateRoute(
                "foo/{ }/{.!$%}/{dynamic.data}/{op.tional}",
                new RouteValueDictionary() { { " ", "not a space" }, { "op.tional", "default value" }, { "ran!dom", "va@lue" } },
                null);

            GetRouteDataHelper(
                r,
                "foo/space/weird/orderid",
                new RouteValueDictionary() { { " ", "space" }, { ".!$%", "weird" }, { "dynamic.data", "orderid" }, { "op.tional", "default value" }, { "ran!dom", "va@lue" } });
        }

#if URLGENERATION

        [Fact]
        public void UrlWithEscapedOpenCloseBraces()
        {
            RouteFormatHelper("foo/{{p1}}", "foo/{p1}");
        }

        private static void RouteFormatHelper(string routeUrl, string requestUrl)
        {
            RouteValueDictionary defaults = new RouteValueDictionary(new { route = "matched" });
            TemplateRoute r = CreateRoute(routeUrl, defaults, null);

            GetRouteDataHelper(r, requestUrl, defaults);
            GetVirtualPathHelper(r, new RouteValueDictionary(), null, Uri.EscapeUriString(requestUrl));
        }

        [Fact]
        public void UrlWithEscapedOpenBraceAtTheEnd()
        {
            RouteFormatHelper("bar{{", "bar{");
        }

        [Fact]
        public void UrlWithEscapedOpenBraceAtTheBeginning()
        {
            RouteFormatHelper("{{bar", "{bar");
        }

        [Fact]
        public void UrlWithRepeatedEscapedOpenBrace()
        {
            RouteFormatHelper("foo{{{{bar", "foo{{bar");
        }

        [Fact]
        public void UrlWithEscapedCloseBraceAtTheEnd()
        {
            RouteFormatHelper("bar}}", "bar}");
        }

        [Fact]
        public void UrlWithEscapedCloseBraceAtTheBeginning()
        {
            RouteFormatHelper("}}bar", "}bar");
        }

        [Fact]
        public void UrlWithRepeatedEscapedCloseBrace()
        {
            RouteFormatHelper("foo}}}}bar", "foo}}bar");
        }

        [Fact]
        public void GetVirtualPathWithUnusedNullValueShouldGenerateUrlAndIgnoreNullValue()
        {
            // DevDiv Bugs 194371: UrlRouting: Exception thrown when generating URL that has some null values
            GetVirtualPathHelper(
                CreateRoute(
                    "{controller}.mvc/{action}/{id}", 
                    new RouteValueDictionary(new { action = "Index", id = "" }),
                    null),
                new RouteValueDictionary(new { controller = "Home", action = "Index", id = "" }),
                new RouteValueDictionary(new { controller = "Home", action = "TestAction", id = "1", format = (string)null }),
                "Home.mvc/TestAction/1");
        }

        [Fact]
        public void GetVirtualPathCanFillInSeparatedParametersWithDefaultValues()
        {
            GetVirtualPathHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" }), null),
                new RouteValueDictionary(),
                new RouteValueDictionary(new { controller = "Orders" }),
                "Orders/en-US");
        }
#endif

        [Fact]
        public void GetRouteDataDoesNotMatchRouteWithLiteralSeparatorDefaultsButNoValue()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" }), null),
                "foo",
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchesRouteWithLiteralSeparatorDefaultsAndLeftValue()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" }), null),
                "foo/xx-",
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchesRouteWithLiteralSeparatorDefaultsAndRightValue()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" }), null),
                "foo/-yy",
                null);
        }

        [Fact]
        public void GetRouteDataMatchesRouteWithLiteralSeparatorDefaultsAndValue()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" }), null),
                "foo/xx-yy",
                new RouteValueDictionary { { "language", "xx" }, { "locale", "yy" }, { "controller", "foo" } });
        }

#if URLGENERATION

        [Fact]
        public void GetVirtualPathWithNonParameterConstraintReturnsUrlWithoutQueryString()
        {
            // DevDiv Bugs 199612: UrlRouting: UrlGeneration should not append parameter to query string if it is a Constraint parameter and not a Url parameter
            GetVirtualPathHelper(
                CreateRoute("{Controller}.mvc/{action}/{end}", null, new RouteValueDictionary(new { foo = CreateHttpMethodConstraint("GET") }), null),
                new RouteValueDictionary(),
                new RouteValueDictionary(new { controller = "Orders", action = "Index", end = "end", foo = "GET" }),
                "Orders.mvc/Index/end");
        }

        [Fact]
        public void DefaultRoutingValuesTestWithStringEmpty()
        {
            var data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}", new RouteValueDictionary(new { val1 = "42", val2 = "", val3 = "" }), new RouteValueDictionary());
            Assert.Equal("Test/42", data.VirtualPath);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}/{val4}", new RouteValueDictionary(new { val1 = "21", val2 = "", val3 = "", val4 = "" }), new RouteValueDictionary(new { val1 = "42", val2 = "11", val3 = "", val4 = "" }));
            Assert.Equal("Test/42/11", data.VirtualPath);

        }

        [Fact]
        public void MixedDefaultAndExplicitRoutingValuesTestWithStringEmpty()
        {
            var data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}", new RouteValueDictionary(new { val1 = "21", val2 = "", val3 = "" }), new RouteValueDictionary(new { val1 = "42" }));
            Assert.Equal("Test/42", data.VirtualPath);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}/{val4}", new RouteValueDictionary(new { val1 = "21", val2 = "", val3 = "", val4 = "" }), new RouteValueDictionary(new { val1 = "42", val2 = "11" }));
            Assert.Equal("Test/42/11", data.VirtualPath);
        }

        [Fact]
        public void DefaultRoutingValuesTestWithNull()
        {
            var data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}", new RouteValueDictionary(new { val1 = "42", val2 = (string)null, val3 = (string)null }), new RouteValueDictionary());
            Assert.Equal("Test/42", data.VirtualPath);
        }

        [Fact]
        public void MixedDefaultAndExplicitRoutingValuesTestWithNull()
        {
            var data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}", new RouteValueDictionary(new { val1 = "21", val2 = (string)null, val3 = (string)null }), new RouteValueDictionary(new { val1 = "42" }));
            Assert.Equal("Test/42", data.VirtualPath);

            data = GetVirtualPathFromRoute("~/Test/", "Test/{val1}/{val2}/{val3}/{val4}", new RouteValueDictionary(new { val1 = "21", val2 = (string)null, val3 = (string)null, val4 = (string)null }), new RouteValueDictionary(new { val1 = "42", val2 = "11" }));
            Assert.Equal("Test/42/11", data.VirtualPath);
        }

#endif

        private static IRouteValues CreateRouteData()
        {
            return new RouteValues(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        }

        private static RouteValueDictionary CreateRouteValueDictionary()
        {
            var values = new RouteValueDictionary();
            return values;
        }

        private static void GetRouteDataHelper(TemplateRoute route, string requestPath, RouteValueDictionary expectedValues)
        {
            // Arrange
            HttpContext context = GetHttpContext(requestPath);

            // Act
            var rd = route.GetRouteData(context);

            // Assert
            if (expectedValues == null)
            {
                Assert.Null(rd);
            }
            else
            {
                Assert.NotNull(rd);
                Assert.Equal<int>(rd.Values.Count, expectedValues.Count);
                foreach (string key in rd.Values.Keys)
                {
                    Assert.Equal(expectedValues[key], rd.Values[key]);
                }
            }
        }

#if URLGENERATION
        private static void GetVirtualPathHelper(TemplateRoute route, RouteValueDictionary currentValues, RouteValueDictionary newValues, string expectedPath)
        {
            // Arrange
            newValues = newValues ?? new RouteValueDictionary();

            HttpContext context = GetHttpContext("/app", String.Empty, null);
            var rd = CreateRouteData();
            foreach (var currentValue in currentValues)
            {
                rd.Values.Add(currentValue.Key, currentValue.Value);
            }

            // Act
            var vpd = route.GetVirtualPath(context, newValues);

            // Assert
            if (expectedPath == null)
            {
                Assert.Null(vpd);
            }
            else
            {
                Assert.NotNull(vpd);
                Assert.Equal<string>(expectedPath, vpd.VirtualPath);
            }
        }

#endif
        private static ITemplateRouteConstraint CreateHttpMethodConstraint(params string[] methods)
        {
            return null;
        }

        internal static HttpContext GetHttpContext(string requestPath)
        {
            return GetHttpContext(null, requestPath);
        }

        private static HttpContext GetHttpContext(string appPath, string requestPath)
        {
            if (!String.IsNullOrEmpty(requestPath) && requestPath[0] == '~')
            {
                requestPath = requestPath.Substring(1);
            }

            if (!String.IsNullOrEmpty(requestPath) && requestPath[0] != '/')
            {
                requestPath = "/" + requestPath;
            }

            var context = new MockHttpContext();
            context.Request.Path = new PathString(requestPath);
            context.Request.PathBase = new PathString(appPath);

            return context;
        }

        private static TemplateRoute CreateRoute(string template)
        {
            return CreateRoute(template, null, null, null);
        }

        private static TemplateRoute CreateRoute(string template, RouteValueDictionary defaults)
        {
            return CreateRoute(template, defaults, null, null);
        }

        private static TemplateRoute CreateRoute(string template, RouteValueDictionary defaults, RouteValueDictionary constraints)
        {
            return CreateRoute(template, defaults, constraints, null);
        }

        private static TemplateRoute CreateRoute(string template, RouteValueDictionary defaults, RouteValueDictionary constraints, RouteValueDictionary dataTokens)
        {
            return new TemplateRoute(template, defaults, constraints, dataTokens);
        }

        private class DevDivBugs178588CustomConstraint
        {
            public string AllowedHeader
            {
                get;
                set;
            }
        }

        private class DevDivBugs178588CustomRoute : TemplateRoute
        {
            public DevDivBugs178588CustomRoute(string url, RouteValueDictionary defaults, RouteValueDictionary constraints)
                : base(url, defaults, constraints, null)
            {
            }

            protected override bool ProcessConstraint(HttpContext httpContext, object constraint, string parameterName, IDictionary<string, object> values, RouteDirection routeDirection)
            {
                if (constraint is DevDivBugs178588CustomConstraint)
                {
                    return false;
                }
                else
                {
                    return base.ProcessConstraint(httpContext, constraint, parameterName, values, routeDirection);
                }
            }
        }

        private sealed class ConstraintData
        {
            public object Constraint
            {
                get;
                set;
            }
            public string ParameterName
            {
                get;
                set;
            }
            public object ParameterValue
            {
                get;
                set;
            }
        }

        private class CustomConstraintTemplateRoute : TemplateRoute
        {
            public CustomConstraintTemplateRoute(string url, RouteValueDictionary defaults, RouteValueDictionary constraints)
                : base(url, defaults, constraints, null)
            {
            }

            public ConstraintData ConstraintData
            {
                get;
                set;
            }

            protected override bool ProcessConstraint(HttpContext request, object constraint, string parameterName, IDictionary<string, object> values, RouteDirection routeDirection)
            {
                object parameterValue;
                values.TryGetValue(parameterName, out parameterValue);

                // Save the parameter values to validate them in the unit tests
                ConstraintData = new ConstraintData
                {
                    Constraint = constraint,
                    ParameterName = parameterName,
                    ParameterValue = parameterValue,
                };

                if (constraint is int)
                {
                    int lengthRequirement = (int)constraint;
                    string paramString = parameterValue as string;
                    if (paramString == null)
                    {
                        throw new InvalidOperationException("This constraint only works with string values.");
                    }
                    return (paramString.Length == lengthRequirement);
                }
                else
                {
                    return base.ProcessConstraint(request, constraint, parameterName, values, routeDirection);
                }
            }
        }

        // This is a placeholder
        private class RouteValueDictionary : Dictionary<string, object>
        {
            public RouteValueDictionary()
                : base(StringComparer.OrdinalIgnoreCase)
            {
            }

            public RouteValueDictionary(object obj)
                : base(StringComparer.OrdinalIgnoreCase)
            {
                foreach (var property in obj.GetType().GetProperties())
                {
                    Add(property.Name, property.GetValue(obj));
                }
            }
        }

        private class MockHttpContext : HttpContext
        {
            private readonly Dictionary<Type, object> _features = new Dictionary<Type, object>();
            private readonly MockHttpRequest _request;

            public MockHttpContext()
            {
                _request = new MockHttpRequest(this);
            }

            public override void Dispose()
            {
            }

            public override object GetFeature(Type type)
            {
                return _features[type];
            }

            public override IDictionary<object, object> Items
            {
                get { throw new NotImplementedException(); }
            }

            public override HttpRequest Request
            {
                get { return _request; }
            }

            public override HttpResponse Response
            {
                get { throw new NotImplementedException(); }
            }

            public override void SetFeature(Type type, object instance)
            {
                _features[type] = instance;
            }
        }

        private class MockHttpRequest : HttpRequest
        {
            private readonly HttpContext _context;
            public MockHttpRequest(HttpContext context)
            {
                _context = context;
            }

            public override Stream Body
            {
                get;
                set;
            }

            public override CancellationToken CallCanceled
            {
                get;
                set;
            }

            public override IReadableStringCollection Cookies
            {
                get { throw new NotImplementedException(); }
            }

            public override IHeaderDictionary Headers
            {
                get { throw new NotImplementedException(); }
            }

            public override HostString Host
            {
                get;
                set;
            }

            public override HttpContext HttpContext
            {
                get { return _context; }
            }

            public override bool IsSecure
            {
                get { throw new NotImplementedException(); }
            }

            public override string Method
            {
                get;
                set;
            }

            public override PathString Path
            {
                get;
                set;
            }

            public override PathString PathBase
            {
                get;
                set;
            }

            public override string Protocol
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override IReadableStringCollection Query
            {
                get { throw new NotImplementedException(); }
            }

            public override QueryString QueryString
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override string Scheme
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
