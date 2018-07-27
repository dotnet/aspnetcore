// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.EndpointFinders;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.AspNetCore.Routing.TestObjects;
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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home", link);
        }

        [Fact]
        public void GetLink_Fail_ThrowsException()
        {
            // Arrange
            var expectedMessage = "Could not find a matching endpoint to generate a link.";
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home" });

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => linkGenerator.GetLink(
                    new LinkGeneratorContext
                    {
                        Endpoints = new[] { endpoint },
                        ExplicitValues = context.ExplicitValues,
                        AmbientValues = context.AmbientValues
                    }));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TryGetLink_Fail()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home" });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home", action = "Index", id = "10" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint1, endpoint2, endpoint3 },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint1, endpoint2, endpoint3 },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_EncodesValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { name = "name with %special #characters" },
                ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index?name=name%20with%20%25special%20%23characters", link);
        }

        [Fact]
        public void GetLink_ForListOfStrings()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                new { color = new List<string> { "red", "green", "blue" } },
                new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index?color=red&color=green&color=blue", link);
        }

        [Fact]
        public void GetLink_ForListOfInts()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                new { items = new List<int> { 10, 20, 30 } },
                new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index?items=10&items=20&items=30", link);
        }

        [Fact]
        public void GetLink_ForList_Empty()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                new { color = new List<string> { } },
                new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_ForList_StringWorkaround()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                new { page = 1, color = new List<string> { "red", "green", "blue" }, message = "textfortest" },
                new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index?page=1&color=red&color=green&color=blue&message=textfortest", link);
        }

        [Fact]
        public void GetLink_Success_AmbientValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_GeneratesLowercaseUrl_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(new RouteOptions() { LowercaseUrls = true });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/home/index", link);
        }

        [Fact]
        public void GetLink_GeneratesLowercaseQueryString_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new RouteOptions() { LowercaseUrls = true, LowercaseQueryStrings = true });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", ShowStatus = "True", INFO = "DETAILED" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/home/index?showstatus=true&info=detailed", link);
        }

        [Fact]
        public void GetLink_GeneratesLowercaseQueryString_OnlyIfLowercaseUrlIsTrue_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new RouteOptions() { LowercaseUrls = false, LowercaseQueryStrings = true });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", ShowStatus = "True", INFO = "DETAILED" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index?ShowStatus=True&INFO=DETAILED", link);
        }

        [Fact]
        public void GetLink_AppendsTrailingSlash_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(new RouteOptions() { AppendTrailingSlash = true });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/", link);
        }

        [Fact]
        public void GetLink_GeneratesLowercaseQueryStringAndTrailingSlash_SetOnRouteOptions()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(
                new RouteOptions() { LowercaseUrls = true, LowercaseQueryStrings = true, AppendTrailingSlash = true });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", ShowStatus = "True", INFO = "DETAILED" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/home/index/?showstatus=true&info=detailed", link);
        }

        [Fact]
        public void GetLink_LowercaseUrlSetToTrue_OnRouteOptions_OverridenByCallsiteValue()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator(new RouteOptions() { LowercaseUrls = true });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "InDex" },
                ambientValues: new { controller = "HoMe" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues,
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
            var linkGenerator = CreateLinkGenerator(new RouteOptions() { LowercaseUrls = false });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "InDex" },
                ambientValues: new { controller = "HoMe" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues,
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
                new RouteOptions() { LowercaseUrls = true, LowercaseQueryStrings = true });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", ShowStatus = "True", INFO = "DETAILED" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues,
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
                new RouteOptions() { LowercaseUrls = false, LowercaseQueryStrings = false });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", ShowStatus = "True", INFO = "DETAILED" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues,
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
            var linkGenerator = CreateLinkGenerator(new RouteOptions() { AppendTrailingSlash = false });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues,
                    AppendTrailingSlash = true
                });

            // Assert
            Assert.Equal("/Home/Index/", link);
        }

        [Fact]
        public void RouteGenerationRejectsConstraints()
        {
            // Arrange
            var context = CreateRouteValuesContext(new { p1 = "abcd" });
            var linkGenerator = CreateLinkGenerator();

            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = "\\d{4}" });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void RouteGenerationAcceptsConstraints()
        {
            // Arrange
            var context = CreateRouteValuesContext(new { p1 = "hello", p2 = "1234" });
            var linkGenerator = CreateLinkGenerator();

            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = new RegexRouteConstraint("\\d{4}"), });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/hello/1234", link);
        }

        [Fact]
        public void RouteWithCatchAllRejectsConstraints()
        {
            // Arrange
            var context = CreateRouteValuesContext(new { p1 = "abcd" });
            var linkGenerator = CreateLinkGenerator();

            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{*p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = new RegexRouteConstraint("\\d{4}") });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void RouteWithCatchAllAcceptsConstraints()
        {
            // Arrange
            var context = CreateRouteValuesContext(new { p1 = "hello", p2 = "1234" });
            var linkGenerator = CreateLinkGenerator();

            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                "{p1}/{*p2}",
                defaults: new { p2 = "catchall" },
                constraints: new { p2 = new RegexRouteConstraint("\\d{4}") });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
                out var link);

            // Assert
            Assert.True(canGenerateLink);
            Assert.Equal("/hello/1234", link);
        }

        [Fact]
        public void GetLinkWithNonParameterConstraintReturnsUrlWithoutQueryString()
        {
            // Arrange
            var context = CreateRouteValuesContext(new { p1 = "hello", p2 = "1234" });
            var linkGenerator = CreateLinkGenerator();
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

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
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
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "slug/Home/Store",
                defaults: new { controller = "Home", action = "Store" },
                constraints: new { c = constraint });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Store" },
                ambientValues: new { controller = "Home", action = "Blog", extra = "42" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", extra = "42" });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
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
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "slug/Home/Store",
                defaults: new { controller = "Home", action = "Store", otherthing = "17" },
                constraints: new { c = constraint });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Store" },
                ambientValues: new { controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

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
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "slug/{controller}/{action}",
                defaults: new { action = "Index" },
                constraints: new { c = constraint, });

            var context = CreateRouteValuesContext(
                explicitValues: new { controller = "Shopping" },
                ambientValues: new { controller = "Home", action = "Blog" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Shopping", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

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
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "slug/Home/Store",
                defaults: new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" },
                constraints: new { c = constraint, });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Store", thirdthing = "13" },
                ambientValues: new { controller = "Home", action = "Blog", otherthing = "17" });

            var expectedValues = new RouteValueDictionary(
                new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/slug/Home/Store", link);
            Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
        }

        [Fact]
        public void GetLink_InlineConstraints_Success()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id:int}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", id = 4 });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/4", link);
        }

        [Fact]
        public void GetLink_InlineConstraints_NonMatchingvalue()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { id = "int" });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", id = "not-an-integer" });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void GetLink_InlineConstraints_OptionalParameter_ValuePresent()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id:int?}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { });
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", id = 98 });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/98", link);
        }

        [Fact]
        public void GetLink_InlineConstraints_OptionalParameter_ValueNotPresent()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { id = "int" });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_InlineConstraints_OptionalParameter_ValuePresent_ConstraintFails()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id?}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { id = "int" });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", id = "not-an-integer" });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void GetLink_InlineConstraints_MultipleInlineConstraints()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id:int:range(1,20)}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", id = 14 });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/14", link);
        }

        [Fact]
        public void GetLink_InlineConstraints_CompositeInlineConstraint_Fails()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{id:int:range(1,20)}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", id = 50 });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
        }

        [Fact]
        public void GetLink_InlineConstraints_CompositeConstraint_FromConstructor()
        {
            // Arrange
            var constraint = new MaxLengthRouteConstraint(20);
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "Home/Index/{name}",
                defaults: new { controller = "Home", action = "Index" },
                constraints: new { name = constraint });

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterPresentInValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterNotPresentInValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_ParameterNotPresentInTemplate_PresentInValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{name}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", name = "products", format = "json" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/products?format=json", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_FollowedByDotAfterSlash_ParameterPresent()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint(
                template: "{controller}/{action}/.{name?}");

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/.products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_FollowedByDotAfterSlash_ParameterNotPresent()
        {
            // Arrange
            var linkGenerator = CreateLinkGenerator();
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/.{name?}");

            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    HttpContext = new DefaultHttpContext(),
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index/", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_InSimpleSegment()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { action = "Index", controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_TwoOptionalParameters_OneValueFromAmbientValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("a/{b=15}/{c?}/{d?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { },
                ambientValues: new { c = "17" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/a/15/17", link);
        }

        [Fact]
        public void GetLink_OptionalParameterAfterDefault_OneValueFromAmbientValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("a/{b=15}/{c?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
               explicitValues: new { },
               ambientValues: new { c = "17" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

            // Assert
            Assert.Equal("/a/15/17", link);
        }

        [Fact]
        public void GetLink_TwoOptionalParametersAfterDefault_LastValueFromAmbientValues()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateMatcherEndpoint("a/{b=15}/{c?}/{d?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
               explicitValues: new { },
               ambientValues: new { d = "17" });

            // Act
            var link = linkGenerator.GetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                });

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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: explicitValues,
                ambientValues: ambientValues);

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { c = "Products", a = "Edit" },
                ambientValues: new { c = "Products", a = "Edit", id = 10 });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: new { c = "Products", a = "List" },
                ambientValues: new { c = "Products", a = "Edit", id = 10 });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
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
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                explicitValues: explicitValues,
                ambientValues: ambientValues);

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new LinkGeneratorContext
                {
                    Endpoints = new[] { endpoint },
                    ExplicitValues = context.ExplicitValues,
                    AmbientValues = context.AmbientValues
                },
                out var link);

            // Assert
            Assert.False(canGenerateLink);
            Assert.Null(link);
        }

        private RouteValuesBasedEndpointFinderContext CreateRouteValuesContext(
            object explicitValues,
            object ambientValues = null)
        {
            var context = new RouteValuesBasedEndpointFinderContext();
            context.ExplicitValues = new RouteValueDictionary(explicitValues);
            context.AmbientValues = new RouteValueDictionary(ambientValues);
            return context;
        }

        private LinkGenerator CreateLinkGenerator(RouteOptions routeOptions = null)
        {
            routeOptions = routeOptions ?? new RouteOptions();
            var options = Options.Create(routeOptions);
            return new DefaultLinkGenerator(
                new DefaultMatchProcessorFactory(
                    options,
                    Mock.Of<IServiceProvider>()),
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                options,
                NullLogger<DefaultLinkGenerator>.Instance);
        }
    }
}
