// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class DefaultLinkGeneratorTest
    {
        [Fact]
        public void GetLink_Success()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}");
            var linkGenerator = CreateLinkGenerator(endpoint);

            // Act
            var link = linkGenerator.GetLink(new { controller = "Home" });

            // Assert
            Assert.Equal("/Home", link);
        }

        [Fact]
        public void GetLink_Fail_ThrowsException()
        {
            // Arrange
            var expectedMessage = "Could not find a matching endpoint to generate a link.";
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(endpoint);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => linkGenerator.GetLink(new { controller = "Home" }));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TryGetLink_Fail()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(endpoint);

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new { controller = "Home" },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
            Assert.Null(link);
        }

        [Fact]
        public void GetLink_MultipleEndpoints_Success()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{id?}");
            var endpoint2 = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var endpoint3 = EndpointFactory.CreateMatcherEndpoint("{controller}");
            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2, endpoint3);

            // Act
            var link = linkGenerator.GetLink(new { controller = "Home", action = "Index", id = "10" });

            // Assert
            Assert.Equal("/Home/Index/10", link);
        }

        [Fact]
        public void GetLink_MultipleEndpoints_Success2()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{id}");
            var endpoint2 = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var endpoint3 = EndpointFactory.CreateMatcherEndpoint("{controller}");
            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2, endpoint3);

            // Act
            var link = linkGenerator.GetLink(new { controller = "Home", action = "Index" });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_EncodesIntermediate_DefaultValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{p1}/{p2=a b}/{p3=foo}");
            var linkGenerator = CreateLinkGenerator(endpoint);

            // Act
            var link = linkGenerator.GetLink(new { p1 = "Home", p3 = "bar" });

            // Assert
            Assert.Equal("/Home/a%20b/bar", link);
        }

        [Theory]
        [InlineData("a/b/c", "/Home/Index/a%2Fb%2Fc")]
        [InlineData("a/b b1/c c1", "/Home/Index/a%2Fb%20b1%2Fc%20c1")]
        public void GetLink_EncodesValue_OfSingleAsteriskCatchAllParameter(string routeValue, string expected)
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{*path}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { path = routeValue });

            // Assert
            Assert.Equal(expected, link);
        }

        [Theory]
        [InlineData("/", "/Home/Index//")]
        [InlineData("a", "/Home/Index/a")]
        [InlineData("a/", "/Home/Index/a/")]
        [InlineData("a/b", "/Home/Index/a/b")]
        [InlineData("a/b/c", "/Home/Index/a/b/c")]
        [InlineData("a/b/cc", "/Home/Index/a/b/cc")]
        [InlineData("a/b/c/", "/Home/Index/a/b/c/")]
        [InlineData("a/b/c//", "/Home/Index/a/b/c//")]
        [InlineData("a//b//c", "/Home/Index/a//b//c")]
        public void GetLink_DoesNotEncodeSlashes_OfDoubleAsteriskCatchAllParameter(string routeValue, string expected)
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{**path}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { path = routeValue });

            // Assert
            Assert.Equal(expected, link);
        }

        [Fact]
        public void GetLink_EncodesContentOtherThanSlashes_OfDoubleAsteriskCatchAllParameter()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{**path}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { path = "a/b b1/c c1" });

            // Assert
            Assert.Equal("/Home/Index/a/b%20b1/c%20c1", link);
        }

        [Fact]
        public void GetLink_EncodesValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { name = "name with %special #characters" });

            // Assert
            Assert.Equal("/Home/Index?name=name%20with%20%25special%20%23characters", link);
        }

        [Fact]
        public void GetLink_ForListOfStrings()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var context = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(context, new { color = new List<string> { "red", "green", "blue" } });

            // Assert
            Assert.Equal("/Home/Index?color=red&color=green&color=blue", link);
        }

        [Fact]
        public void GetLink_ForListOfInts()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { items = new List<int> { 10, 20, 30 } });

            // Assert
            Assert.Equal("/Home/Index?items=10&items=20&items=30", link);
        }

        [Fact]
        public void GetLink_ForList_Empty()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { color = new List<string> { } });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_ForList_StringWorkaround()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { page = 1, color = new List<string> { "red", "green", "blue" }, message = "textfortest" });

            // Assert
            Assert.Equal("/Home/Index?page=1&color=red&color=green&color=blue&message=textfortest", link);
        }

        [Fact]
        public void GetLink_Success_AmbientValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { action = "Index" });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_GeneratesLowercaseUrl_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(new[] { endpoint }, new RouteOptions() { LowercaseUrls = true });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { action = "Index" });

            // Assert
            Assert.Equal("/home/index", link);
        }

        [Fact]
        public void GetLink_GeneratesLowercaseQueryString_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { LowercaseUrls = true, LowercaseQueryStrings = true });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", ShowStatus = "True", INFO = "DETAILED" });

            // Assert
            Assert.Equal("/home/index?showstatus=true&info=detailed", link);
        }

        [Fact]
        public void GetLink_GeneratesLowercaseQueryString_OnlyIfLowercaseUrlIsTrue_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { LowercaseUrls = false, LowercaseQueryStrings = true });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", ShowStatus = "True", INFO = "DETAILED" });

            // Assert
            Assert.Equal("/Home/Index?ShowStatus=True&INFO=DETAILED", link);
        }

        [Fact]
        public void GetLink_AppendsTrailingSlash_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { AppendTrailingSlash = true });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { action = "Index" });

            // Assert
            Assert.Equal("/Home/Index/", link);
        }

        [Fact]
        public void GetLink_GeneratesLowercaseQueryStringAndTrailingSlash_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { LowercaseUrls = true, LowercaseQueryStrings = true, AppendTrailingSlash = true });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", ShowStatus = "True", INFO = "DETAILED" });

            // Assert
            Assert.Equal("/home/index/?showstatus=true&info=detailed", link);
        }

        [Fact]
        public void GetLink_LowercaseUrlSetToTrue_OnRouteOptions_OverridenByCallsiteValue()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { LowercaseUrls = true });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "HoMe" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                values: new { action = "InDex" },
                new LinkOptions
                {
                    LowercaseUrls = false
                });

            // Assert
            Assert.Equal("/HoMe/InDex", link);
        }

        [Fact]
        public void GetLink_LowercaseUrlSetToFalse_OnRouteOptions_OverridenByCallsiteValue()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { LowercaseUrls = false });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "HoMe" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                values: new { action = "InDex" },
                new LinkOptions
                {
                    LowercaseUrls = true
                });

            // Assert
            Assert.Equal("/home/index", link);
        }

        [Fact]
        public void GetLink_LowercaseUrlQueryStringsSetToTrue_OnRouteOptions_OverridenByCallsiteValue()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { LowercaseUrls = true, LowercaseQueryStrings = true });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                values: new { action = "Index", ShowStatus = "True", INFO = "DETAILED" },
                new LinkOptions
                {
                    LowercaseUrls = false,
                    LowercaseQueryStrings = false
                });

            // Assert
            Assert.Equal("/Home/Index?ShowStatus=True&INFO=DETAILED", link);
        }

        [Fact]
        public void GetLink_LowercaseUrlQueryStringsSetToFalse_OnRouteOptions_OverridenByCallsiteValue()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { LowercaseUrls = false, LowercaseQueryStrings = false });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                values: new { action = "Index", ShowStatus = "True", INFO = "DETAILED" },
                new LinkOptions
                {
                    LowercaseUrls = true,
                    LowercaseQueryStrings = true
                });

            // Assert
            Assert.Equal("/home/index?showstatus=true&info=detailed", link);
        }

        [Fact]
        public void GetLink_AppendTrailingSlashSetToFalse_OnRouteOptions_OverridenByCallsiteValue()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new[] { endpoint },
                new RouteOptions() { AppendTrailingSlash = false });
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                values: new { action = "Index" },
                new LinkOptions
                {
                    AppendTrailingSlash = true
                });

            // Assert
            Assert.Equal("/Home/Index/", link);
        }

        [Fact]
        public void RouteGenerationRejectsConstraints()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = "\\d{4}" });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { p1 = "abcd" },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void RouteGenerationAcceptsConstraints()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = new RegexRouteConstraint("\\d{4}"), });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { p1 = "hello", p2 = "1234" },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/hello/1234", link);
        }

        [Fact]
        public void RouteWithCatchAllRejectsConstraints()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{*p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = new RegexRouteConstraint("\\d{4}") });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { p1 = "abcd" },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void RouteWithCatchAllAcceptsConstraints()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{*p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = new RegexRouteConstraint("\\d{4}") });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { p1 = "hello", p2 = "1234" },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/hello/1234", link);
        }

        [Fact]
        public void GetLinkWithNonParameterConstraintReturnsUrlWithoutQueryString()
        {
            // Arrange
            var target = new Mock<IRouteConstraint>();
            target
                .Setup(
                    e => e.Match(
                        It.IsAny<HttpContext>(),
                        It.IsAny<IRouter>(),
                        It.IsAny<string>(),
                        It.IsAny<RouteValueDictionary>(),
                        It.IsAny<RouteDirection>()))
                .Returns(true)
                .Verifiable();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = target.Object });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { p1 = "hello", p2 = "1234" },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/hello/1234", link);
            target.VerifyAll();
        }

        // Any ambient values from the current request should be visible to constraint, even
        // if they have nothing to do with the route generating a link
        [Fact]
        public void GetLink_ConstraintsSeeAmbientValues()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "slug/Home/Store",
                defaults: new { controller = "Home", action = "Store" },
                constraints: new { c = constraint });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(
                ambientValues: new { controller = "Home", action = "Blog", extra = "42" });
            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", extra = "42" });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { action = "Store" },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/slug/Home/Store", link);
            Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
        }

        // Non-parameter default values from the routing generating a link are not in the 'values'
        // collection when constraints are processed.
        [Fact]
        public void GetLink_ConstraintsDontSeeDefaults_WhenTheyArentParameters()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "slug/Home/Store",
                defaults: new { controller = "Home", action = "Store", otherthing = "17" },
                constraints: new { c = constraint });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Blog" });
            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { action = "Store" });

            // Assert
            Assert.Equal("/slug/Home/Store", link);
            Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
        }

        // Default values are visible to the constraint when they are used to fill a parameter.
        [Fact]
        public void GetLink_ConstraintsSeesDefault_WhenThereItsAParamter()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "slug/{controller}/{action}",
                defaults: new { action = "Index" },
                constraints: new { c = constraint, });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { controller = "Home", action = "Blog" });
            var expectedValues = new RouteValueDictionary(
                new { controller = "Shopping", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { controller = "Shopping" });

            // Assert
            Assert.Equal("/slug/Shopping", link);
            Assert.Equal(expectedValues, constraint.Values);
        }

        // Default values from the routing generating a link are in the 'values' collection when
        // constraints are processed - IFF they are specified as values or ambient values.
        [Fact]
        public void GetLink_ConstraintsSeeDefaults_IfTheyAreSpecifiedOrAmbient()
        {
            // Arrange
            var constraint = new CapturingConstraint();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "slug/Home/Store",
                defaults: new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" },
                constraints: new { c = constraint, });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(
                ambientValues: new { controller = "Home", action = "Blog", otherthing = "17" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Store", thirdthing = "13" });

            // Assert
            Assert.Equal("/slug/Home/Store", link);
            Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
        }

        [Fact]
        public void GetLink_InlineConstraints_Success()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id:int}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home", id = 4 });

            // Assert
            Assert.Equal("/Home/Index/4", link);
        }

        [Fact]
        public void GetLink_InlineConstraints_NonMatchingvalue()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { id = "int" });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { action = "Index", controller = "Home", id = "not-an-integer" },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void GetLink_InlineConstraints_OptionalParameter_ValuePresent()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id:int?}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { action = "Index", controller = "Home", id = 98 });

            // Assert
            Assert.Equal("/Home/Index/98", link);
        }

        [Fact]
        public void GetLink_InlineConstraints_OptionalParameter_ValueNotPresent()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { id = "int" });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { action = "Index", controller = "Home" });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_InlineConstraints_OptionalParameter_ValuePresent_ConstraintFails()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { id = "int" });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { action = "Index", controller = "Home", id = "not-an-integer" },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void GetLink_InlineConstraints_MultipleInlineConstraints()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id:int:range(1,20)}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home", id = 14 });

            // Assert
            Assert.Equal("/Home/Index/14", link);
        }

        [Fact]
        public void GetLink_InlineConstraints_CompositeInlineConstraint_Fails()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id:int:range(1,20)}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { action = "Index", controller = "Home", id = 50 },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void GetLink_InlineConstraints_CompositeConstraint_FromConstructor()
        {
            // Arrange
            var constraint = new MaxLengthRouteConstraint(20);
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{name}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { name = constraint });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home", name = "products" });

            // Assert
            Assert.Equal("/Home/Index/products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterPresentInValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home", name = "products" });

            // Assert
            Assert.Equal("/Home/Index/products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterNotPresentInValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home" });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterPresentInValuesAndDefaults()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "{controller}/{action}/{name}",
                defaults: new { name = "default-products" });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home", name = "products" });

            // Assert
            Assert.Equal("/Home/Index/products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterNotPresentInValues_PresentInDefaults()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "{controller}/{action}/{name}",
                defaults: new { name = "products" });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home" });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_ParameterNotPresentInTemplate_PresentInValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{name}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home", name = "products", format = "json" });

            // Assert
            Assert.Equal("/Home/Index/products?format=json", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_FollowedByDotAfterSlash_ParameterPresent()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "{controller}/{action}/.{name?}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(
                httpContext,
                new { action = "Index", controller = "Home", name = "products" });

            // Assert
            Assert.Equal("/Home/Index/.products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_FollowedByDotAfterSlash_ParameterNotPresent()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/.{name?}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { action = "Index", controller = "Home" });

            // Assert
            Assert.Equal("/Home/Index/", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_InSimpleSegment()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { action = "Index", controller = "Home" });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_TwoOptionalParameters_OneValueFromAmbientValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("a/{b=15}/{c?}/{d?}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { c = "17" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { });

            // Assert
            Assert.Equal("/a/15/17", link);
        }

        [Fact]
        public void GetLink_OptionalParameterAfterDefault_OneValueFromAmbientValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("a/{b=15}/{c?}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { c = "17" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { });

            // Assert
            Assert.Equal("/a/15/17", link);
        }

        [Fact]
        public void GetLink_TwoOptionalParametersAfterDefault_LastValueFromAmbientValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("a/{b=15}/{c?}/{d?}");
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { d = "17" });

            // Act
            var link = linkGenerator.GetLink(httpContext, new { });

            // Assert
            Assert.Equal("/a", link);
        }

        public static TheoryData<object, object, object, object> DoesNotDiscardAmbientValuesData
        {
            get
            {
                // - ambient values
                // - explicit values
                // - required values
                // - defaults
                return new TheoryData<object, object, object, object>
                {
                    // link to same action on same controller
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link to same action on same controller - ignoring case
                    {
                        new { controller = "ProDUcts", action = "EDit", id = 10 },
                        new { controller = "ProDUcts", action = "EDit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link to same action and same controller on same area
                    {
                        new { area = "Admin", controller = "Products", action = "Edit", id = 10 },
                        new { area = "Admin", controller = "Products", action = "Edit" },
                        new { area = "Admin", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "Admin", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link to same action and same controller on same area
                    {
                        new { area = "Admin", controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = "Admin", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "Admin", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link to same action and same controller
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = "", controller = "Products", action = "Edit", page = "" },
                        new { area = "", controller = "Products", action = "Edit", page = "" }
                    },

                    // link to same page
                    {
                        new { page = "Products/Edit", id = 10 },
                        new { page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit" }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DoesNotDiscardAmbientValuesData))]
        public void TryGetLink_DoesNotDiscardAmbientValues_IfAllRequiredKeysMatch(
            object ambientValues,
            object explicitValues,
            object requiredValues,
            object defaults)
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "Products/Edit/{id}",
                requiredValues: requiredValues,
                defaults: defaults);
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues);

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new RouteValueDictionary(explicitValues),
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/Products/Edit/10", link);
        }

        [Fact]
        public void TryGetLink_DoesNotDiscardAmbientValues_IfAllRequiredValuesMatch_ForGenericKeys()
        {
            // Verifying that discarding works in general usage case i.e when keys are not like controller, action etc.

            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "Products/Edit/{id}",
                requiredValues: new { c = "Products", a = "Edit" },
                defaults: new { c = "Products", a = "Edit" });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { c = "Products", a = "Edit", id = 10 });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { c = "Products", a = "Edit" },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/Products/Edit/10", link);
        }

        [Fact]
        public void TryGetLink_DiscardsAmbientValues_ForGenericKeys()
        {
            // Verifying that discarding works in general usage case i.e when keys are not like controller, action etc.

            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "Products/Edit/{id}",
                requiredValues: new { c = "Products", a = "Edit" },
                defaults: new { c = "Products", a = "Edit" });
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues: new { c = "Products", a = "Edit", id = 10 });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new { c = "Products", a = "List" },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
            Assert.Null(link);
        }

        public static TheoryData<object, object, object, object> DiscardAmbientValuesData
        {
            get
            {
                // - ambient values
                // - explicit values
                // - required values
                // - defaults
                return new TheoryData<object, object, object, object>
                {
                    // link to different action on same controller
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { controller = "Products", action = "List" },
                        new { area = (string)null, controller = "Products", action = "List", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "List", page = (string)null }
                    },

                    // link to different action on same controller and same area
                    {
                        new { area = "Customer", controller = "Products", action = "Edit", id = 10 },
                        new { area = "Customer", controller = "Products", action = "List" },
                        new { area = "Customer", controller = "Products", action = "List", page = (string)null },
                        new { area = "Customer", controller = "Products", action = "List", page = (string)null }
                    },

                    // link from one area to a different one
                    {
                        new { area = "Admin", controller = "Products", action = "Edit", id = 10 },
                        new { area = "Consumer", controller = "Products", action = "Edit" },
                        new { area = "Consumer", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "Consumer", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link from non-area to a area one
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { area = "Consumer", controller = "Products", action = "Edit" },
                        new { area = "Consumer", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "Consumer", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link from area to a non-area based action
                    {
                        new { area = "Admin", controller = "Products", action = "Edit", id = 10 },
                        new { area = "", controller = "Products", action = "Edit" },
                        new { area = "", controller = "Products", action = "Edit", page = (string)null },
                        new { area = "", controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link from controller-action to a page
                    {
                        new { controller = "Products", action = "Edit", id = 10 },
                        new { page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit"},
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit"}
                    },

                    // link from a page to controller-action
                    {
                        new { page = "Products/Edit", id = 10 },
                        new { controller = "Products", action = "Edit" },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null },
                        new { area = (string)null, controller = "Products", action = "Edit", page = (string)null }
                    },

                    // link from one page to a different page
                    {
                        new { page = "Products/Details", id = 10 },
                        new { page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit" },
                        new { area = (string)null, controller = (string)null, action = (string)null, page = "Products/Edit" }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DiscardAmbientValuesData))]
        public void TryGetLink_DiscardsAmbientValues_IfAnyAmbientValue_IsDifferentThan_EndpointRequiredValues(
            object ambientValues,
            object explicitValues,
            object requiredValues,
            object defaults)
        {
            // Linking to a different action on the same controller

            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "Products/Edit/{id}",
                requiredValues: requiredValues,
                defaults: defaults);
            var linkGenerator = CreateLinkGenerator(endpoint);
            var httpContext = CreateHttpContext(ambientValues);

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                httpContext,
                new RouteValueDictionary(explicitValues),
                out var link);

            // Assert
            Assert.False(canGenerateLink);
            Assert.Null(link);
        }

        [Fact]
        public void TryGetLinkByAddress_WithCustomAddress_CanGenerateLink()
        {
            // Arrange
            var services = GetBasicServices();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IEndpointFinder<INameMetadata>, EndpointFinderByName>());
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint(
                "Products/Details/{id}",
                requiredValues: new { controller = "Products", action = "Details" },
                defaults: new { controller = "Products", action = "Details" });
            var endpoint2 = EndpointFactory.CreateMatcherEndpoint(
                "Customers/Details/{id}",
                requiredValues: new { controller = "Customers", action = "Details" },
                defaults: new { controller = "Customers", action = "Details" },
                metadata: new NameMetadata("CustomerDetails"));
            var linkGenerator = CreateLinkGenerator(new[] { endpoint1, endpoint2 }, new RouteOptions(), services);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLinkByAddress<INameMetadata>(
                httpContext,
                address: new NameMetadata("CustomerDetails"),
                values: new { id = 10 },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/Customers/Details/10", link);
        }

        [Fact]
        public void TryGetLinkByAddress_WithCustomAddress_CanGenerateLink_RespectsLinkOptions_SuppliedAtCallSite()
        {
            // Arrange
            var services = GetBasicServices();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IEndpointFinder<INameMetadata>, EndpointFinderByName>());
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint(
                "Products/Details/{id}",
                requiredValues: new { controller = "Products", action = "Details" },
                defaults: new { controller = "Products", action = "Details" });
            var endpoint2 = EndpointFactory.CreateMatcherEndpoint(
                "Customers/Details/{id}",
                requiredValues: new { controller = "Customers", action = "Details" },
                defaults: new { controller = "Customers", action = "Details" },
                metadata: new NameMetadata("CustomerDetails"));
            var linkGenerator = CreateLinkGenerator(new[] { endpoint1, endpoint2 }, new RouteOptions(), services);
            var httpContext = CreateHttpContext(ambientValues: new { });

            // Act
            var canGenerateLink = linkGenerator.TryGetLinkByAddress<INameMetadata>(
                httpContext,
                address: new NameMetadata("CustomerDetails"),
                values: new { id = 10 },
                new LinkOptions
                {
                    LowercaseUrls = true
                },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/customers/details/10", link);
        }

        [Fact]
        public void GetTemplate_ByRouteValues_ReturnsTemplate()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint(
                "Product/Edit/{id}",
                requiredValues: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null },
                defaults: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null });
            var linkGenerator = CreateLinkGenerator(endpoint1);
            var values = new RouteValueDictionary(new { controller = "Product", action = "Edit" });

            // Act
            var template = linkGenerator.GetTemplate(values);

            // Assert
            var defaultTemplate = Assert.IsType<DefaultLinkGenerationTemplate>(template);
            Assert.Same(linkGenerator, defaultTemplate.LinkGenerator);
            Assert.Equal(new[] { endpoint1 }, defaultTemplate.Endpoints);
            Assert.Equal(values, defaultTemplate.EarlierExplicitValues);
            Assert.Null(defaultTemplate.HttpContext);
            Assert.Empty(defaultTemplate.AmbientValues);
        }

        [Fact]
        public void GetTemplate_ByRouteName_ReturnsTemplate()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint(
                "Product/Edit/{id}",
                defaults: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null },
                metadata: new RouteValuesAddressMetadata(
                    "EditProduct",
                    new RouteValueDictionary(new { controller = "Product", action = "Edit", area = (string)null, page = (string)null })));
            var linkGenerator = CreateLinkGenerator(endpoint1);

            // Act
            var template = linkGenerator.GetTemplate("EditProduct", values: new { });

            // Assert
            var defaultTemplate = Assert.IsType<DefaultLinkGenerationTemplate>(template);
            Assert.Same(linkGenerator, defaultTemplate.LinkGenerator);
            Assert.Equal(new[] { endpoint1 }, defaultTemplate.Endpoints);
            Assert.Empty(defaultTemplate.EarlierExplicitValues);
            Assert.Null(defaultTemplate.HttpContext);
            Assert.Empty(defaultTemplate.AmbientValues);
        }

        [Fact]
        public void GetTemplate_ByRouteName_ReturnsTemplate_WithMultipleEndpoints()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint(
                "Product/Edit/{id}",
                defaults: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null },
                metadata: new RouteValuesAddressMetadata(
                    "default",
                    new RouteValueDictionary(new { controller = "Product", action = "Edit", area = (string)null, page = (string)null })));
            var endpoint2 = EndpointFactory.CreateMatcherEndpoint(
                "Product/Details/{id}",
                defaults: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null },
                metadata: new RouteValuesAddressMetadata(
                    "default",
                    new RouteValueDictionary(new { controller = "Product", action = "Edit", area = (string)null, page = (string)null })));
            var linkGenerator = CreateLinkGenerator(endpoint1, endpoint2);

            // Act
            var template = linkGenerator.GetTemplate("default", values: new { });

            // Assert
            var defaultTemplate = Assert.IsType<DefaultLinkGenerationTemplate>(template);
            Assert.Same(linkGenerator, defaultTemplate.LinkGenerator);
            Assert.Equal(new[] { endpoint1, endpoint2 }, defaultTemplate.Endpoints);
            Assert.Empty(defaultTemplate.EarlierExplicitValues);
            Assert.Null(defaultTemplate.HttpContext);
            Assert.Empty(defaultTemplate.AmbientValues);
        }

        [Fact]
        public void GetTemplateByAddress_ByCustomAddress_ReturnsTemplate()
        {
            // Arrange
            var services = GetBasicServices();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IEndpointFinder<INameMetadata>, EndpointFinderByName>());
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint(
                "Product/Edit/{id}",
                requiredValues: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null },
                defaults: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null });
            var endpoint2 = EndpointFactory.CreateMatcherEndpoint(
                "Customers/Details/{id}",
                requiredValues: new { controller = "Customers", action = "Details" },
                defaults: new { controller = "Customers", action = "Details" },
                metadata: new NameMetadata("CustomerDetails"));
            var linkGenerator = CreateLinkGenerator(new[] { endpoint1, endpoint2 }, new RouteOptions(), services);

            // Act
            var template = linkGenerator.GetTemplateByAddress<INameMetadata>(new NameMetadata("CustomerDetails"));

            // Assert
            var defaultTemplate = Assert.IsType<DefaultLinkGenerationTemplate>(template);
            Assert.Same(linkGenerator, defaultTemplate.LinkGenerator);
            Assert.Equal(new[] { endpoint2 }, defaultTemplate.Endpoints);
            Assert.Empty(defaultTemplate.EarlierExplicitValues);
            Assert.Null(defaultTemplate.HttpContext);
            Assert.Empty(defaultTemplate.AmbientValues);
        }

        [Fact]
        public void MakeUrl_Honors_LinkOptions()
        {
            // Arrange
            var services = GetBasicServices();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IEndpointFinder<INameMetadata>, EndpointFinderByName>());
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint(
                "Product/Edit/{id}",
                requiredValues: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null },
                defaults: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null });
            var endpoint2 = EndpointFactory.CreateMatcherEndpoint(
                "Customers/Details/{id}",
                requiredValues: new { controller = "Customers", action = "Details" },
                defaults: new { controller = "Customers", action = "Details" },
                metadata: new NameMetadata("CustomerDetails"));
            var linkGenerator = CreateLinkGenerator(new[] { endpoint1, endpoint2 }, new RouteOptions(), services);

            // Act1
            var template = linkGenerator.GetTemplateByAddress<INameMetadata>(new NameMetadata("CustomerDetails"));

            // Assert1
            Assert.NotNull(template);

            // Act2
            var link = template.MakeUrl(new { id = 10 }, new LinkOptions { LowercaseUrls = true });

            // Assert2
            Assert.Equal("/customers/details/10", link);

            // Act3
            link = template.MakeUrl(new { id = 25 });

            // Assert3
            Assert.Equal("/Customers/Details/25", link);
        }

        [Fact]
        public void MakeUrl_GeneratesLink_WithExtraRouteValues()
        {
            // Arrange
            var endpoint1 = EndpointFactory.CreateMatcherEndpoint(
                "Product/Edit/{id}",
                requiredValues: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null },
                defaults: new { controller = "Product", action = "Edit", area = (string)null, page = (string)null });
            var linkGenerator = CreateLinkGenerator(endpoint1);

            // Act1
            var template = linkGenerator.GetTemplate(
                values: new { controller = "Product", action = "Edit", foo = "bar" });

            // Assert1
            Assert.NotNull(template);

            // Act2
            var link = template.MakeUrl(new { id = 10 });

            // Assert2
            Assert.Equal("/Product/Edit/10?foo=bar", link);

            // Act3
            link = template.MakeUrl(new { id = 25, foo = "boo" });

            // Assert3
            Assert.Equal("/Product/Edit/25?foo=boo", link);
        }

        private LinkGenerator CreateLinkGenerator(params Endpoint[] endpoints)
        {
            return CreateLinkGenerator(endpoints, routeOptions: null);
        }

        private LinkGenerator CreateLinkGenerator(
            Endpoint[] endpoints,
            RouteOptions routeOptions,
            ServiceCollection services = null)
        {
            if (services == null)
            {
                services = GetBasicServices();
            }

            if (endpoints != null || endpoints.Length > 0)
            {
                services.Configure<EndpointOptions>(o =>
                {
                    o.DataSources.Add(new DefaultEndpointDataSource(endpoints));
                });
            }

            routeOptions = routeOptions ?? new RouteOptions();
            var options = Options.Create(routeOptions);
            var serviceProvider = services.BuildServiceProvider();

            return new DefaultLinkGenerator(
                new DefaultMatchProcessorFactory(options, serviceProvider),
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                options,
                NullLogger<DefaultLinkGenerator>.Instance,
                serviceProvider);
        }

        private HttpContext CreateHttpContext(object ambientValues)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IEndpointFeature>(new EndpointFeature
            {
                Values = new RouteValueDictionary(ambientValues)
            });
            return httpContext;
        }

        private ServiceCollection GetBasicServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddOptions();
            services.AddRouting();
            services.AddLogging();
            return services;
        }

        private class EndpointFinderByName : IEndpointFinder<INameMetadata>
        {
            private readonly CompositeEndpointDataSource _dataSource;

            public EndpointFinderByName(CompositeEndpointDataSource dataSource)
            {
                _dataSource = dataSource;
            }

            public IEnumerable<Endpoint> FindEndpoints(INameMetadata address)
            {
                var endpoint = _dataSource.Endpoints.SingleOrDefault(e =>
                {
                    var nameMetadata = e.Metadata.GetMetadata<INameMetadata>();
                    return nameMetadata != null && string.Equals(address.Name, nameMetadata.Name);
                });
                return new[] { endpoint };
            }
        }

        private interface INameMetadata
        {
            string Name { get; }
        }

        private class NameMetadata : INameMetadata
        {
            public NameMetadata(string name)
            {
                Name = name;
            }
            public string Name { get; }
        }
    }
}
