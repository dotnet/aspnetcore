// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Xunit;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    public class TemplateRouteTests
    {
        [Fact]
        public void MatchSingleRoute()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/Bank/DoAction/123");
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", null);

            // Act
            var match = r.Match(new RouteContext(context));

            // Assert
            Assert.NotNull(match);
            Assert.Equal("Bank", match.Values["controller"]);
            Assert.Equal("DoAction", match.Values["action"]);
            Assert.Equal("123", match.Values["id"]);
        }

        [Fact]
        public void NoMatchSingleRoute()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/Bank/DoAction");
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", null);

            // Act
            var rd = r.Match(new RouteContext(context));

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchSingleRouteWithDefaults()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/Bank/DoAction");
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", new RouteValueDictionary(new { id = "default id" }));

            // Act
            var rd = r.Match(new RouteContext(context));

            // Assert
            Assert.Equal("Bank", rd.Values["controller"]);
            Assert.Equal("DoAction", rd.Values["action"]);
            Assert.Equal("default id", rd.Values["id"]);
        }

        [Fact]
        public void NoMatchSingleRouteWithDefaults()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/Bank");
            TemplateRoute r = CreateRoute("{controller}/{action}/{id}", new RouteValueDictionary(new { id = "default id" }));

            // Act
            var rd = r.Match(new RouteContext(context));

            // Assert
            Assert.Null(rd);
        }

        [Fact]
        public void MatchRouteWithLiterals()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/111/bar/222");
            TemplateRoute r = CreateRoute("moo/{p1}/bar/{p2}", new RouteValueDictionary(new { p2 = "default p2" }));

            // Act
            var rd = r.Match(new RouteContext(context));

            // Assert
            Assert.Equal("111", rd.Values["p1"]);
            Assert.Equal("222", rd.Values["p2"]);
        }

        [Fact]
        public void MatchRouteWithLiteralsAndDefaults()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/moo/111/bar/");
            TemplateRoute r = CreateRoute("moo/{p1}/bar/{p2}", new RouteValueDictionary(new { p2 = "default p2" }));

            // Act
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

            // Assert
            Assert.NotNull(rd);
        }

        private void VerifyRouteMatchesWithContext(string route, string requestUrl)
        {
            HttpContext context = GetHttpContext(requestUrl);
            TemplateRoute r = CreateRoute(route, null);

            // Act
            var rd = r.Match(new RouteContext(context));

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
            TemplateRoute r = CreateRoute("{p1}/{p2}", new RouteValueDictionary(new { p2 = (string)null, foo = "bar" }));

            // Act
            var rd = r.Match(new RouteContext(context));

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
                new RouteValueDictionary(new { controller = "blog", action = "showpost", m = (string)null, d = (string)null }));

            // Act
            var rd = r.Match(new RouteContext(context));

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
                CreateRoute("language/{lang}-{region}", new RouteValueDictionary(new { lang = "xx", region = "yy" })),
                "language/-", 
                null);
        }

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
                    })),
                "Home/ShowPilot/777/12345./foobar",
                new RouteValueDictionary(new { controller = "Home", action = "ShowPilot", missionId = "777", name = "12345./foobar" }));
        }

        [Fact]
        public void RouteWithCatchAllClauseCapturesManySlashes()
        {
            // Arrange
            HttpContext context = GetHttpContext("~/v1/v2/v3");
            TemplateRoute r = CreateRoute("{p1}/{*p2}", null);

            // Act
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            var rd = r.Match(new RouteContext(context));

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
            TemplateRoute r = CreateRoute("{p1}/{*p2}", new RouteValueDictionary(new { p2 = "catchall" }));

            // Act
            var rd = r.Match(new RouteContext(context));

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
            TemplateRoute r = CreateRoute("{p1}/{*p2}", new RouteValueDictionary(new { p2 = "catchall" }));

            // Act
            var rd = r.Match(new RouteContext(context));

            // Assert
            Assert.NotNull(rd);
            Assert.Equal<int>(2, rd.Values.Count);
            Assert.Equal("v1", rd.Values["p1"]);
            Assert.Equal("hello/whatever", rd.Values["p2"]);
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
                new RouteValueDictionary() { { " ", "not a space" }, { "op.tional", "default value" }, { "ran!dom", "va@lue" } });

            GetRouteDataHelper(
                r,
                "foo/space/weird/orderid",
                new RouteValueDictionary() { { " ", "space" }, { ".!$%", "weird" }, { "dynamic.data", "orderid" }, { "op.tional", "default value" }, { "ran!dom", "va@lue" } });
        }

        [Fact]
        public void GetRouteDataDoesNotMatchRouteWithLiteralSeparatorDefaultsButNoValue()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" })),
                "foo",
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchesRouteWithLiteralSeparatorDefaultsAndLeftValue()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" })),
                "foo/xx-",
                null);
        }

        [Fact]
        public void GetRouteDataDoesNotMatchesRouteWithLiteralSeparatorDefaultsAndRightValue()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" })),
                "foo/-yy",
                null);
        }

        [Fact]
        public void GetRouteDataMatchesRouteWithLiteralSeparatorDefaultsAndValue()
        {
            GetRouteDataHelper(
                CreateRoute("{controller}/{language}-{locale}", new RouteValueDictionary(new { language = "en", locale = "US" })),
                "foo/xx-yy",
                new RouteValueDictionary { { "language", "xx" }, { "locale", "yy" }, { "controller", "foo" } });
        }

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
            var match = route.Match(new RouteContext(context));

            // Assert
            if (expectedValues == null)
            {
                Assert.Null(match);
            }
            else
            {
                Assert.NotNull(match);
                Assert.Equal<int>(expectedValues.Count, match.Values.Count);
                foreach (string key in match.Values.Keys)
                {
                    Assert.Equal(expectedValues[key], match.Values[key]);
                }
            }
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
            return CreateRoute(template, null);
        }

        private static TemplateRoute CreateRoute(string template, RouteValueDictionary defaults)
        {
            return new TemplateRoute(new MockRouteEndpoint(), template, defaults);
        }

        private class MockRouteEndpoint : IRouteEndpoint
        {
            public Task<bool> Send(HttpContext context)
            {
                throw new NotImplementedException();
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
