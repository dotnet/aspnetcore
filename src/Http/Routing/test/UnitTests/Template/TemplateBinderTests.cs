// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Template.Tests
{
    public class TemplateBinderTests
    {
        private readonly IInlineConstraintResolver _inlineConstraintResolver = GetInlineConstraintResolver();

        public static TheoryData EmptyAndNullDefaultValues =>
            new TheoryData<string, RouteValueDictionary, RouteValueDictionary, string>
            {
                {
                    "Test/{val1}/{val2}",
                    new RouteValueDictionary(new {val1 = "", val2 = ""}),
                    new RouteValueDictionary(new {val2 = "SomeVal2"}),
                    null
                },
                {
                    "Test/{val1}/{val2}",
                    new RouteValueDictionary(new {val1 = "", val2 = ""}),
                    new RouteValueDictionary(new {val1 = "a"}),
                    "/Test/a"
                },
                {
                    "Test/{val1}/{val2}/{val3}",
                    new RouteValueDictionary(new {val1 = "", val3 = ""}),
                    new RouteValueDictionary(new {val2 = "a"}),
                    null
                },
                {
                    "Test/{val1}/{val2}",
                    new RouteValueDictionary(new {val1 = "", val2 = ""}),
                    new RouteValueDictionary(new {val1 = "a", val2 = "b"}),
                    "/Test/a/b"
                },
                {
                    "Test/{val1}/{val2}/{val3}",
                    new RouteValueDictionary(new {val1 = "", val2 = "", val3 = ""}),
                    new RouteValueDictionary(new {val1 = "a", val2 = "b", val3 = "c"}),
                    "/Test/a/b/c"
                },
                {
                    "Test/{val1}/{val2}/{val3}",
                    new RouteValueDictionary(new {val1 = "", val2 = "", val3 = ""}),
                    new RouteValueDictionary(new {val1 = "a", val2 = "b"}),
                    "/Test/a/b"
                },
                {
                    "Test/{val1}/{val2}/{val3}",
                    new RouteValueDictionary(new {val1 = "", val2 = "", val3 = ""}),
                    new RouteValueDictionary(new {val1 = "a"}),
                    "/Test/a"
                },
                {
                    "Test/{val1}",
                    new RouteValueDictionary(new {val1 = "42", val2 = "", val3 = ""}),
                    new RouteValueDictionary(),
                    "/Test"
                },
                {
                    "Test/{val1}/{val2}/{val3}",
                    new RouteValueDictionary(new {val1 = "42", val2 = (string)null, val3 = (string)null}),
                    new RouteValueDictionary(),
                    "/Test"
                },
                {
                    "Test/{val1}/{val2}/{val3}/{val4}",
                    new RouteValueDictionary(new {val1 = "21", val2 = "", val3 = "", val4 = ""}),
                    new RouteValueDictionary(new {val1 = "42", val2 = "11", val3 = "", val4 = ""}),
                    "/Test/42/11"
                },
                {
                    "Test/{val1}/{val2}/{val3}",
                    new RouteValueDictionary(new {val1 = "21", val2 = "", val3 = ""}),
                    new RouteValueDictionary(new {val1 = "42"}),
                    "/Test/42"
                },
                {
                    "Test/{val1}/{val2}/{val3}/{val4}",
                    new RouteValueDictionary(new {val1 = "21", val2 = "", val3 = "", val4 = ""}),
                    new RouteValueDictionary(new {val1 = "42", val2 = "11"}),
                    "/Test/42/11"
                },
                {
                    "Test/{val1}/{val2}/{val3}",
                    new RouteValueDictionary(new {val1 = "21", val2 = (string)null, val3 = (string)null}),
                    new RouteValueDictionary(new {val1 = "42"}),
                    "/Test/42"
                },
                {
                    "Test/{val1}/{val2}/{val3}/{val4}",
                    new RouteValueDictionary(new {val1 = "21", val2 = (string)null, val3 = (string)null, val4 = (string)null}),
                    new RouteValueDictionary(new {val1 = "42", val2 = "11"}),
                    "/Test/42/11"
                },
            };

        [Theory]
        [MemberData(nameof(EmptyAndNullDefaultValues))]
        public void Binding_WithEmptyAndNull_DefaultValues(
            string template,
            RouteValueDictionary defaults,
            RouteValueDictionary values,
            string expected)
        {
            // Arrange
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                TemplateParser.Parse(template),
                defaults);

            // Act & Assert
            var result = binder.GetValues(ambientValues: null, values: values);
            if (result == null)
            {
                if (expected == null)
                {
                    return;
                }
                else
                {
                    Assert.NotNull(result);
                }
            }

            var boundTemplate = binder.BindValues(result.AcceptedValues);
            if (expected == null)
            {
                Assert.Null(boundTemplate);
            }
            else
            {
                Assert.NotNull(boundTemplate);
                Assert.Equal(expected, boundTemplate);
            }
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnBothEndsMatches()
        {
            RunTest(
                "language/{lang}-{region}",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                "/language/xx-yy");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnLeftEndMatches()
        {
            RunTest(
                "language/{lang}-{region}a",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                "/language/xx-yya");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnRightEndMatches()
        {
            RunTest(
                "language/a{lang}-{region}",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                "/language/axx-yy");
        }

        public static TheoryData OptionalParamValues =>
            new TheoryData<string, RouteValueDictionary, RouteValueDictionary, RouteValueDictionary, string>
            {
                // defaults
                // ambient values
                // values
                {
                    "Test/{val1}/{val2}.{val3?}",
                    new RouteValueDictionary(new {val1 = "someval1", val2 = "someval2"}),
                    new RouteValueDictionary(new {val3 = "someval3"}),
                    new RouteValueDictionary(new {val3 = "someval3"}),
                    "/Test/someval1/someval2.someval3"
                },
                {
                    "Test/{val1}/{val2}.{val3?}",
                    new RouteValueDictionary(new {val1 = "someval1", val2 = "someval2"}),
                    new RouteValueDictionary(new {val3 = "someval3a"}),
                    new RouteValueDictionary(new {val3 = "someval3v"}),
                    "/Test/someval1/someval2.someval3v"
                },
                {
                    "Test/{val1}/{val2}.{val3?}",
                    new RouteValueDictionary(new {val1 = "someval1", val2 = "someval2"}),
                    new RouteValueDictionary(new {val3 = "someval3a"}),
                    new RouteValueDictionary(),
                    "/Test/someval1/someval2.someval3a"
                },
                {
                    "Test/{val1}/{val2}.{val3?}",
                    new RouteValueDictionary(new {val1 = "someval1", val2 = "someval2"}),
                    new RouteValueDictionary(),
                    new RouteValueDictionary(new {val3 = "someval3v"}),
                    "/Test/someval1/someval2.someval3v"
                },
                {
                    "Test/{val1}/{val2}.{val3?}",
                    new RouteValueDictionary(new {val1 = "someval1", val2 = "someval2"}),
                    new RouteValueDictionary(),
                    new RouteValueDictionary(),
                    "/Test/someval1/someval2"
                },
                {
                    "Test/{val1}.{val2}.{val3}.{val4?}",
                    new RouteValueDictionary(new {val1 = "someval1", val2 = "someval2" }),
                    new RouteValueDictionary(),
                    new RouteValueDictionary(new {val4 = "someval4", val3 = "someval3" }),
                    "/Test/someval1.someval2."
                    + "someval3.someval4"
                },
                {
                    "Test/{val1}.{val2}.{val3}.{val4?}",
                    new RouteValueDictionary(new {val1 = "someval1", val2 = "someval2" }),
                    new RouteValueDictionary(),
                    new RouteValueDictionary(new {val3 = "someval3" }),
                    "/Test/someval1.someval2."
                    + "someval3"
                },
                {
                    "Test/.{val2?}",
                    new RouteValueDictionary(new { }),
                    new RouteValueDictionary(),
                    new RouteValueDictionary(new {val2 = "someval2" }),
                    "/Test/.someval2"
                },
                {
                    "Test/{val1}.{val2}",
                    new RouteValueDictionary(new {val1 = "someval1", val2 = "someval2" }),
                    new RouteValueDictionary(),
                    new RouteValueDictionary(new {val3 = "someval3" }),
                    "/Test/someval1.someval2?" +
                    "val3=someval3"
                },
            };

        [Theory]
        [MemberData(nameof(OptionalParamValues))]
        public void GetVirtualPathWithMultiSegmentWithOptionalParam(
            string template,
            RouteValueDictionary defaults,
            RouteValueDictionary ambientValues,
            RouteValueDictionary values,
            string expected)
        {
            // Arrange
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                TemplateParser.Parse(template),
                defaults);

            // Act & Assert
            var result = binder.GetValues(ambientValues: ambientValues, values: values);
            if (result == null)
            {
                if (expected == null)
                {
                    return;
                }
                else
                {
                    Assert.NotNull(result);
                }
            }

            var boundTemplate = binder.BindValues(result.AcceptedValues);
            if (expected == null)
            {
                Assert.Null(boundTemplate);
            }
            else
            {
                Assert.NotNull(boundTemplate);
                Assert.Equal(expected, boundTemplate);
            }
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnNeitherEndMatches()
        {
            RunTest(
                "language/a{lang}-{region}a",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                "/language/axx-yya");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnNeitherEndDoesNotMatch()
        {
            RunTest(
                "language/a{lang}-{region}a",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "", region = "yy" }),
                null);
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnNeitherEndDoesNotMatch2()
        {
            RunTest(
                "language/a{lang}-{region}a",
                null,
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "xx", region = "" }),
                null);
        }

        [Fact]
        public void GetVirtualPathWithSimpleMultiSegmentParamsOnBothEndsMatches()
        {
            RunTest(
                "language/{lang}",
                null,
                new RouteValueDictionary(new { lang = "en" }),
                new RouteValueDictionary(new { lang = "xx" }),
                "/language/xx");
        }

        [Fact]
        public void GetVirtualPathWithSimpleMultiSegmentParamsOnLeftEndMatches()
        {
            RunTest(
                "language/{lang}-",
                null,
                new RouteValueDictionary(new { lang = "en" }),
                new RouteValueDictionary(new { lang = "xx" }),
                "/language/xx-");
        }

        [Fact]
        public void GetVirtualPathWithSimpleMultiSegmentParamsOnRightEndMatches()
        {
            RunTest(
                "language/a{lang}",
                null,
                new RouteValueDictionary(new { lang = "en" }),
                new RouteValueDictionary(new { lang = "xx" }),
                "/language/axx");
        }

        [Fact]
        public void GetVirtualPathWithSimpleMultiSegmentParamsOnNeitherEndMatches()
        {
            RunTest(
                "language/a{lang}a",
                null,
                new RouteValueDictionary(new { lang = "en" }),
                new RouteValueDictionary(new { lang = "xx" }),
                "/language/axxa");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentStandardMvcRouteMatches()
        {
            RunTest(
                "{controller}.mvc/{action}/{id}",
                new RouteValueDictionary(new { action = "Index", id = (string)null }),
                new RouteValueDictionary(new { controller = "home", action = "list", id = (string)null }),
                new RouteValueDictionary(new { controller = "products" }),
                "/products.mvc");
        }

        [Fact]
        public void GetVirtualPathWithMultiSegmentParamsOnBothEndsWithDefaultValuesMatches()
        {
            RunTest(
                "language/{lang}-{region}",
                new RouteValueDictionary(new { lang = "xx", region = "yy" }),
                new RouteValueDictionary(new { lang = "en", region = "US" }),
                new RouteValueDictionary(new { lang = "zz" }),
                "/language/zz-yy");
        }

        [Fact]
        public void GetUrlWithDefaultValue()
        {
            // URL should be found but excluding the 'id' parameter, which has only a default value.
            RunTest(
               "{controller}/{action}/{id}",
               new RouteValueDictionary(new { id = "defaultid" }),
               new RouteValueDictionary(new { controller = "home", action = "oldaction" }),
               new RouteValueDictionary(new { action = "newaction" }),
               "/home/newaction");
        }

        [Fact]
        public void GetVirtualPathWithEmptyStringRequiredValueReturnsNull()
        {
            RunTest(
                "foo/{controller}",
                null,
                new RouteValueDictionary(new { }),
                new RouteValueDictionary(new { controller = "" }),
                null);
        }

        [Fact]
        public void GetVirtualPathWithNullRequiredValueReturnsNull()
        {
            RunTest(
                "foo/{controller}",
                null,
                new RouteValueDictionary(new { }),
                new RouteValueDictionary(new { controller = (string)null }),
                null);
        }

        [Fact]
        public void GetVirtualPathWithRequiredValueReturnsPath()
        {
            RunTest(
                "foo/{controller}",
                null,
                new RouteValueDictionary(new { }),
                new RouteValueDictionary(new { controller = "home" }),
                "/foo/home");
        }

        [Fact]
        public void GetUrlWithNullDefaultValue()
        {
            // URL should be found but excluding the 'id' parameter, which has only a default value.
            RunTest(
                "{controller}/{action}/{id}",
                new RouteValueDictionary(new { id = (string)null }),
                new RouteValueDictionary(new { controller = "home", action = "oldaction", id = (string)null }),
                new RouteValueDictionary(new { action = "newaction" }),
                "/home/newaction");
        }

        [Fact]
        public void GetVirtualPathCanFillInSeparatedParametersWithDefaultValues()
        {
            RunTest(
                "{controller}/{language}-{locale}",
                new RouteValueDictionary(new { language = "en", locale = "US" }),
                new RouteValueDictionary(),
                new RouteValueDictionary(new { controller = "Orders" }),
                "/Orders/en-US");
        }

        [Fact]
        public void GetVirtualPathWithUnusedNullValueShouldGenerateUrlAndIgnoreNullValue()
        {
            RunTest(
                "{controller}.mvc/{action}/{id}",
                new RouteValueDictionary(new { action = "Index", id = "" }),
                new RouteValueDictionary(new { controller = "Home", action = "Index", id = "" }),
                new RouteValueDictionary(new { controller = "Home", action = "TestAction", id = "1", format = (string)null }),
                "/Home.mvc/TestAction/1");
        }

        [Fact]
        public void GetUrlWithMissingValuesDoesntMatch()
        {
            RunTest(
                "{controller}/{action}/{id}",
                null,
                new { controller = "home", action = "oldaction" },
                new { action = "newaction" },
                null);
        }

        [Fact]
        public void GetUrlWithEmptyRequiredValuesReturnsNull()
        {
            RunTest(
                "{p1}/{p2}/{p3}",
                null,
                new { p1 = "v1", },
                new { p2 = "", p3 = "" },
                null);
        }

        [Fact]
        public void GetUrlWithEmptyOptionalValuesReturnsShortUrl()
        {
            RunTest(
                "{p1}/{p2}/{p3}",
                new { p2 = "d2", p3 = "d3" },
                new { p1 = "v1", },
                new { p2 = "", p3 = "" },
                "/v1");
        }

        [Fact]
        public void GetUrlShouldIgnoreValuesAfterChangedParameter()
        {
            RunTest(
                "{controller}/{action}/{id}",
                new { action = "Index", id = (string)null },
                new { controller = "orig", action = "init", id = "123" },
                new { action = "new", },
                "/orig/new");
        }

        [Fact]
        public void GetUrlWithNullForMiddleParameterIgnoresRemainingParameters()
        {
            RunTest(
                "UrlGeneration1/{controller}.mvc/{action}/{category}/{year}/{occasion}/{SafeParam}",
                new { year = 1995, occasion = "Christmas", action = "Play", SafeParam = "SafeParamValue" },
                new { controller = "UrlRouting", action = "Play", category = "Photos", year = "2008", occasion = "Easter", SafeParam = "SafeParamValue" },
                new { year = (string)null, occasion = "Hola" },
                "/UrlGeneration1/UrlRouting.mvc/Play/"
                + "Photos/1995/Hola");
        }

        [Fact]
        public void GetUrlWithEmptyStringForMiddleParameterIgnoresRemainingParameters()
        {
            var ambientValues = new RouteValueDictionary();
            ambientValues.Add("controller", "UrlRouting");
            ambientValues.Add("action", "Play");
            ambientValues.Add("category", "Photos");
            ambientValues.Add("year", "2008");
            ambientValues.Add("occasion", "Easter");
            ambientValues.Add("SafeParam", "SafeParamValue");

            var values = new RouteValueDictionary();
            values.Add("year", String.Empty);
            values.Add("occasion", "Hola");

            RunTest(
                "UrlGeneration1/{controller}.mvc/{action}/{category}/{year}/{occasion}/{SafeParam}",
                new RouteValueDictionary(new { year = 1995, occasion = "Christmas", action = "Play", SafeParam = "SafeParamValue" }),
                ambientValues,
                values,
                "/UrlGeneration1/UrlRouting.mvc/"
                + "Play/Photos/1995/Hola");
        }

        [Fact]
        public void GetUrlWithEmptyStringForMiddleParameterShouldUseDefaultValue()
        {
            var ambientValues = new RouteValueDictionary();
            ambientValues.Add("Controller", "Test");
            ambientValues.Add("Action", "Fallback");
            ambientValues.Add("param1", "fallback1");
            ambientValues.Add("param2", "fallback2");
            ambientValues.Add("param3", "fallback3");

            var values = new RouteValueDictionary();
            values.Add("controller", "subtest");
            values.Add("param1", "b");

            RunTest(
                "{controller}.mvc/{action}/{param1}",
                new RouteValueDictionary(new { action = "Default" }),
                ambientValues,
                values,
                "/subtest.mvc/Default/b");
        }

        [Fact]
        public void GetUrlVerifyEncoding()
        {
            var values = new RouteValueDictionary();
            values.Add("controller", "#;?:@&=+$,");
            values.Add("action", "showcategory");
            values.Add("id", 123);
            values.Add("so?rt", "de?sc");
            values.Add("maxPrice", 100);

            RunTest(
                "{controller}.mvc/{action}/{id}",
                new RouteValueDictionary(new { controller = "Home" }),
                new RouteValueDictionary(new { controller = "home", action = "Index", id = (string)null }),
                values,
                "/%23;%3F%3A@%26%3D%2B$,.mvc/showcategory/123?so%3Frt=de%3Fsc&maxPrice=100");
        }

        [Fact]
        public void GetUrlGeneratesQueryStringForNewValuesAndEscapesQueryString()
        {
            var values = new RouteValueDictionary(new { controller = "products", action = "showcategory", id = 123, maxPrice = 100 });
            values.Add("so?rt", "de?sc");

            RunTest(
                "{controller}.mvc/{action}/{id}",
                new RouteValueDictionary(new { controller = "Home" }),
                new RouteValueDictionary(new { controller = "home", action = "Index", id = (string)null }),
                values,
               "/products.mvc/showcategory/123" +
               "?so%3Frt=de%3Fsc&maxPrice=100");
        }

        [Fact]
        public void GetUrlGeneratesQueryStringForNewValuesButIgnoresNewValuesThatMatchDefaults()
        {
            RunTest(
                "{controller}.mvc/{action}/{id}",
                new RouteValueDictionary(new { controller = "Home", Custom = "customValue" }),
                new RouteValueDictionary(new { controller = "Home", action = "Index", id = (string)null }),
                new RouteValueDictionary(
                    new
                    {
                        controller = "products",
                        action = "showcategory",
                        id = 123,
                        sort = "desc",
                        maxPrice = 100,
                        custom = "customValue"
                    }),
                "/products.mvc/showcategory/123" +
                "?sort=desc&maxPrice=100");
        }

        [Fact]
        public void GetVirtualPathEncodesParametersAndLiterals()
        {
            RunTest(
                "bl%og/{controller}/he llo/{action}",
                null,
                new RouteValueDictionary(new { controller = "ho%me", action = "li st" }),
                new RouteValueDictionary(),
                "/bl%25og/ho%25me/he%20llo/li%20st");
        }

        [Fact]
        public void GetVirtualDoesNotEncodeLeadingSlashes()
        {
            RunTest(
                "{controller}/{action}",
                null,
                new RouteValueDictionary(new { controller = "/home", action = "/my/index" }),
                new RouteValueDictionary(),
                "/home/%2Fmy%2Findex");
        }

        [Fact]
        public void GetUrlWithCatchAllWithValue()
        {
            RunTest(
                "{p1}/{*p2}",
                new RouteValueDictionary(new { id = "defaultid" }),
                new RouteValueDictionary(new { p1 = "v1" }),
                new RouteValueDictionary(new { p2 = "v2a/v2b" }),
                "/v1/v2a%2Fv2b");
        }

        [Fact]
        public void GetUrlWithCatchAllWithEmptyValue()
        {
            RunTest(
                "{p1}/{*p2}",
                new RouteValueDictionary(new { id = "defaultid" }),
                new RouteValueDictionary(new { p1 = "v1" }),
                new RouteValueDictionary(new { p2 = "" }),
                "/v1");
        }

        [Fact]
        public void GetUrlWithCatchAllWithNullValue()
        {
            RunTest(
                "{p1}/{*p2}",
                new RouteValueDictionary(new { id = "defaultid" }),
                new RouteValueDictionary(new { p1 = "v1" }),
                new RouteValueDictionary(new { p2 = (string)null }),
                "/v1");
        }

        [Fact]
        public void GetUrlWithLeadingTildeSlash()
        {
            RunTest(
                "~/foo",
                null,
                null,
                new RouteValueDictionary(new { }),
                "/foo");
        }

        [Fact]
        public void GetUrlWithLeadingSlash()
        {
            RunTest(
                "/foo",
                null,
                null,
                new RouteValueDictionary(new { }),
                "/foo");
        }

        [Fact]
        public void TemplateBinder_KeepsExplicitlySuppliedRouteValues_OnFailedRouteMatch()
        {
            // Arrange
            var template = "{area?}/{controller=Home}/{action=Index}/{id?}";
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                TemplateParser.Parse(template),
                defaults: null);
            var ambientValues = new RouteValueDictionary();
            var routeValues = new RouteValueDictionary(new { controller = "Test", action = "Index" });

            // Act
            var templateValuesResult = binder.GetValues(ambientValues, routeValues);
            var boundTemplate = binder.BindValues(templateValuesResult.AcceptedValues);

            // Assert
            Assert.Null(boundTemplate);
            Assert.Equal(2, templateValuesResult.CombinedValues.Count);
            object routeValue;
            Assert.True(templateValuesResult.CombinedValues.TryGetValue("controller", out routeValue));
            Assert.Equal("Test", routeValue?.ToString());
            Assert.True(templateValuesResult.CombinedValues.TryGetValue("action", out routeValue));
            Assert.Equal("Index", routeValue?.ToString());
        }

#if ROUTE_COLLECTION

        [Fact]
        public void GetUrlShouldValidateOnlyAcceptedParametersAndUserDefaultValuesForInvalidatedParameters()
        {
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
        public void GetUrlWithRouteThatHasExtensionWithSubsequentDefaultValueIncludesExtensionButNotDefaultValue()
        {
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
        public void GetUrlWithValuesThatAreCompletelyDifferentFromTheCurrentRoute()
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
        public void GetVirtualPathUsesCurrentValuesNotInRouteToMatch()
        {
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

#if DATA_TOKENS
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
#endif

#if ROUTE_FORMAT_HELPER

        [Fact]
        public void UrlWithEscapedOpenCloseBraces()
        {
            RouteFormatHelper("foo/{{p1}}", "foo/{p1}");
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

        private static void RouteFormatHelper(string routeUrl, string requestUrl)
        {
            var defaults = new RouteValueDictionary(new { route = "matched" });
            var r = CreateRoute(routeUrl, defaults, null);

            GetRouteDataHelper(r, requestUrl, defaults);
            GetVirtualPathHelper(r, new RouteValueDictionary(), null, Uri.EscapeUriString(requestUrl));
        }

#endif

#if CONSTRAINTS
        [Fact]
        public void GetVirtualPathWithNonParameterConstraintReturnsUrlWithoutQueryString()
        {
            // DevDiv Bugs 199612: UrlRouting: UrlGeneration should not append parameter to query string if it is a Constraint parameter and not a Url parameter
            RunTest(
                "{Controller}.mvc/{action}/{end}",
                null,
                new RouteValueDictionary(new { foo = CreateHttpMethodConstraint("GET") }),
                new RouteValueDictionary(),
                new RouteValueDictionary(new { controller = "Orders", action = "Index", end = "end", foo = "GET" }),
                "Orders.mvc/Index/end");
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

#endif

        private static void RunTest(
            string template,
            RouteValueDictionary defaults,
            RouteValueDictionary ambientValues,
            RouteValueDictionary values,
            string expected)
        {
            // Arrange
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                TemplateParser.Parse(template),
                defaults);

            // Act & Assert
            var result = binder.GetValues(ambientValues, values);
            if (result == null)
            {
                if (expected == null)
                {
                    return;
                }
                else
                {
                    Assert.NotNull(result);
                }
            }

            var boundTemplate = binder.BindValues(result.AcceptedValues);
            if (expected == null)
            {
                Assert.Null(boundTemplate);
            }
            else
            {
                Assert.NotNull(boundTemplate);

                // We want to chop off the query string and compare that using an unordered comparison
                var expectedParts = new PathAndQuery(expected);
                var actualParts = new PathAndQuery(boundTemplate);

                Assert.Equal(expectedParts.Path, actualParts.Path);

                if (expectedParts.Parameters == null)
                {
                    Assert.Null(actualParts.Parameters);
                }
                else
                {
                    Assert.Equal(expectedParts.Parameters.Count, actualParts.Parameters.Count);

                    foreach (var kvp in expectedParts.Parameters)
                    {
                        string value;
                        Assert.True(actualParts.Parameters.TryGetValue(kvp.Key, out value));
                        Assert.Equal(kvp.Value, value);
                    }
                }
            }
        }

        private static void RunTest(
            string template,
            object defaults,
            object ambientValues,
            object values,
            string expected)
        {
            RunTest(
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(ambientValues),
                new RouteValueDictionary(values),
                expected);
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData("", null, true)]
        [InlineData(null, "", true)]
        [InlineData("blog", null, false)]
        [InlineData(null, "store", false)]
        [InlineData("Cool", "cool", true)]
        [InlineData("Co0l", "cool", false)]
        public void RoutePartsEqualTest(object left, object right, bool expected)
        {
            // Arrange & Act & Assert
            if (expected)
            {
                Assert.True(TemplateBinder.RoutePartsEqual(left, right));
            }
            else
            {
                Assert.False(TemplateBinder.RoutePartsEqual(left, right));
            }
        }

        [Fact]
        public void GetValues_SuccessfullyMatchesRouteValues_ForExplicitEmptyStringValue_AndNullDefault()
        {
            // Arrange
            var expected = "/Home/Index";
            var template = "Home/Index";
            var defaults = new RouteValueDictionary(new { controller = "Home", action = "Index", area = (string)null });
            var ambientValues = new RouteValueDictionary(new { controller = "Rail", action = "Schedule", area = "Travel" });
            var explicitValues = new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" });
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                TemplateParser.Parse(template),
                defaults);

            // Act1
            var result = binder.GetValues(ambientValues, explicitValues);

            // Assert1
            Assert.NotNull(result);

            // Act2
            var boundTemplate = binder.BindValues(result.AcceptedValues);

            // Assert2
            Assert.NotNull(boundTemplate);
            Assert.Equal(expected, boundTemplate);
        }

        [Fact]
        public void GetValues_SuccessfullyMatchesRouteValues_ForExplicitNullValue_AndEmptyStringDefault()
        {
            // Arrange
            var expected = "/Home/Index";
            var template = "Home/Index";
            var defaults = new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" });
            var ambientValues = new RouteValueDictionary(new { controller = "Rail", action = "Schedule", area = "Travel" });
            var explicitValues = new RouteValueDictionary(new { controller = "Home", action = "Index", area = (string)null });
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                TemplateParser.Parse(template),
                defaults);

            // Act1
            var result = binder.GetValues(ambientValues, explicitValues);

            // Assert1
            Assert.NotNull(result);

            // Act2
            var boundTemplate = binder.BindValues(result.AcceptedValues);

            // Assert2
            Assert.NotNull(boundTemplate);
            Assert.Equal(expected, boundTemplate);
        }

        [Fact]
        public void BindValues_ParameterTransformer()
        {
            // Arrange
            var expected = "/ConventionalTransformerRoute/conventional-transformer/Param/my-value";

            var template = "ConventionalTransformerRoute/conventional-transformer/Param/{param:length(500):slugify?}";
            var defaults = new RouteValueDictionary(new { controller = "ConventionalTransformer", action = "Param" });
            var ambientValues = new RouteValueDictionary(new { controller = "ConventionalTransformer", action = "Param" });
            var explicitValues = new RouteValueDictionary(new { controller = "ConventionalTransformer", action = "Param", param = "MyValue" });
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                RoutePatternFactory.Parse(
                    template,
                    defaults,
                    parameterPolicies: null,
                    requiredValues: new { area = (string)null, action = "Param", controller = "ConventionalTransformer", page = (string)null }),
                defaults,
                requiredKeys: defaults.Keys,
                parameterPolicies: new (string, IParameterPolicy)[] { ("param", new LengthRouteConstraint(500)), ("param", new SlugifyParameterTransformer()), });

            // Act
            var result = binder.GetValues(ambientValues, explicitValues);
            var boundTemplate = binder.BindValues(result.AcceptedValues);

            // Assert
            Assert.Equal(expected, boundTemplate);
        }

        [Fact]
        public void BindValues_AmbientAndExplicitValuesDoNotMatch_Success()
        {
            // Arrange
            var expected = "/Travel/Flight";

            var template = "{area}/{controller}/{action}";
            var defaults = new RouteValueDictionary(new { action = "Index" });
            var ambientValues = new RouteValueDictionary(new { area = "Travel", controller = "Rail", action = "Index" });
            var explicitValues = new RouteValueDictionary(new { controller = "Flight", action = "Index" });
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                RoutePatternFactory.Parse(
                    template,
                    defaults,
                    parameterPolicies: null,
                    requiredValues: new { area = "Travel", action = "SomeAction", controller = "Flight", page = (string)null }),
                defaults,
                requiredKeys: new string[] { "area", "action", "controller", "page" },
                parameterPolicies: null);

            // Act
            var result = binder.GetValues(ambientValues, explicitValues);
            var boundTemplate = binder.BindValues(result.AcceptedValues);

            // Assert
            Assert.Equal(expected, boundTemplate);
        }

        [Fact]
        public void BindValues_LinkingFromPageToAController_Success()
        {
            // Arrange
            var expected = "/LG2/SomeAction";

            var template = "{controller=Home}/{action=Index}/{id?}";
            var defaults = new RouteValueDictionary();
            var ambientValues = new RouteValueDictionary(new { page = "/LGAnotherPage", id = "17" });
            var explicitValues = new RouteValueDictionary(new { controller = "LG2", action = "SomeAction" });
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                RoutePatternFactory.Parse(
                    template,
                    defaults,
                    parameterPolicies: null,
                    requiredValues: new { area = (string)null, action = "SomeAction", controller = "LG2", page = (string)null }),
                defaults,
                requiredKeys: new string[] { "area", "action", "controller", "page" },
                parameterPolicies: null);

            // Act
            var result = binder.GetValues(ambientValues, explicitValues);
            var boundTemplate = binder.BindValues(result.AcceptedValues);

            // Assert
            Assert.Equal(expected, boundTemplate);
        }

        // Regression test for dotnet/aspnetcore#4212
        //
        // An ambient value should be used to satisfy a required value even if if we're discarding
        // ambient values.
        [Fact]
        public void BindValues_LinkingFromPageToAControllerInAreaWithAmbientArea_Success()
        {
            // Arrange
            var expected = "/Admin/LG2/SomeAction";

            var template = "{area}/{controller=Home}/{action=Index}/{id?}";
            var defaults = new RouteValueDictionary();
            var ambientValues = new RouteValueDictionary(new { area = "Admin", page = "/LGAnotherPage", id = "17" });
            var explicitValues = new RouteValueDictionary(new { controller = "LG2", action = "SomeAction" });
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                RoutePatternFactory.Parse(
                    template,
                    defaults,
                    parameterPolicies: null,
                    requiredValues: new { area = "Admin", action = "SomeAction", controller = "LG2", page = (string)null }),
                defaults,
                requiredKeys: new string[] { "area", "action", "controller", "page" },
                parameterPolicies: null);

            // Act
            var result = binder.GetValues(ambientValues, explicitValues);
            var boundTemplate = binder.BindValues(result.AcceptedValues);

            // Assert
            Assert.Equal(expected, boundTemplate);
        }

        [Fact]
        public void BindValues_HasUnmatchingAmbientValues_Discard()
        {
            // Arrange
            var expected = "/Admin/LG3/SomeAction?anothervalue=5";

            var template = "Admin/LG3/SomeAction/{id?}";
            var defaults = new RouteValueDictionary(new { controller = "LG3", action = "SomeAction", area = "Admin" });
            var ambientValues = new RouteValueDictionary(new { controller = "LG1", action = "LinkToAnArea", id = "17" });
            var explicitValues = new RouteValueDictionary(new { controller = "LG3", area = "Admin", action = "SomeAction", anothervalue = "5" });
            var binder = new TemplateBinder(
                UrlEncoder.Default,
                new DefaultObjectPoolProvider().Create(new UriBuilderContextPooledObjectPolicy()),
                RoutePatternFactory.Parse(
                    template,
                    defaults,
                    parameterPolicies: null,
                    requiredValues: new { area = "Admin", action = "SomeAction", controller = "LG3", page = (string)null }),
                defaults,
                requiredKeys: new string[] { "area", "action", "controller", "page" },
                parameterPolicies: null);

            // Act
            var result = binder.GetValues(ambientValues, explicitValues);
            var boundTemplate = binder.BindValues(result.AcceptedValues);

            // Assert
            Assert.Equal(expected, boundTemplate);
        }

        private static IInlineConstraintResolver GetInlineConstraintResolver()
        {
            var services = new ServiceCollection().AddOptions();
            var serviceProvider = services.BuildServiceProvider();
            var accessor = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();
            return new DefaultInlineConstraintResolver(accessor, serviceProvider);
        }

        private class PathAndQuery
        {
            public PathAndQuery(string uri)
            {
                var queryIndex = uri.IndexOf("?", StringComparison.Ordinal);
                if (queryIndex == -1)
                {
                    Path = uri;
                }
                else
                {
                    Path = uri.Substring(0, queryIndex);

                    var query = uri.Substring(queryIndex + 1);
                    Parameters =
                        query
                            .Split(new char[] { '&' }, StringSplitOptions.None)
                            .Select(s => s.Split(new char[] { '=' }, StringSplitOptions.None))
                            .ToDictionary(pair => pair[0], pair => pair[1]);
                }
            }

            public string Path { get; private set; }

            public Dictionary<string, string> Parameters { get; private set; }
        }
    }
}
