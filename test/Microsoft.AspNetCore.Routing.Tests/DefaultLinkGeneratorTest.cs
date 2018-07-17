// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing.EndpointFinders;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.Logging;
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
            var endpoint = CreateEndpoint("{controller}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home", link);
        }

        [Fact]
        public void GetLink_Fail_ThrowsException()
        {
            // Arrange
            var expectedMessage = "Could not find a matching endpoint to generate a link.";
            var endpoint = CreateEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home" });

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues));
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public void TryGetLink_Fail()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home" });

            // Act
            var canGenerateLink = linkGenerator.TryGetLink(
                new[] { endpoint },
                context.ExplicitValues,
                context.AmbientValues,
                out var link);

            // Assert
            Assert.False(canGenerateLink);
            Assert.Null(link);
        }

        [Fact]
        public void GetLink_MultipleEndpoints_Success()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("{controller}/{action}/{id?}");
            var endpoint2 = CreateEndpoint("{controller}/{action}");
            var endpoint3 = CreateEndpoint("{controller}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home", action = "Index", id = "10" });

            // Act
            var link = linkGenerator.GetLink(
                new[] { endpoint1, endpoint2, endpoint3 },
                context.ExplicitValues,
                context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index/10", link);
        }

        [Fact]
        public void GetLink_MultipleEndpoints_Success2()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("{controller}/{action}/{id}");
            var endpoint2 = CreateEndpoint("{controller}/{action}");
            var endpoint3 = CreateEndpoint("{controller}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(
                new[] { endpoint1, endpoint2, endpoint3 },
                context.ExplicitValues,
                context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_EncodesValues()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                suppliedValues: new { name = "name with %special #characters" },
                ambientValues: new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index?name=name%20with%20%25special%20%23characters", link);
        }

        [Fact]
        public void GetLink_ForListOfStrings()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                new { color = new List<string> { "red", "green", "blue" } },
                new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index?color=red&color=green&color=blue", link);
        }

        [Fact]
        public void GetLink_ForListOfInts()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                new { items = new List<int> { 10, 20, 30 } },
                new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index?items=10&items=20&items=30", link);
        }

        [Fact]
        public void GetLink_ForList_Empty()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                new { color = new List<string> { } },
                new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_ForList_StringWorkaround()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                new { page = 1, color = new List<string> { "red", "green", "blue" }, message = "textfortest" },
                new { controller = "Home", action = "Index" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index?page=1&color=red&color=green&color=blue&message=textfortest", link);
        }

        [Fact]
        public void GetLink_Success_AmbientValues()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                suppliedValues: new { action = "Index" },
                ambientValues: new { controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        //[Fact]
        //public void RouteGenerationRejectsConstraints()
        //{
        //    // Arrange
        //    var context = CreateRouteValuesContext(new { p1 = "abcd" });

        //    var endpoint = CreateEndpoints(
        //        "{p1}/{p2}",
        //        new { p2 = "catchall" },
        //        true,
        //        new RouteValueDictionary(new { p2 = "\\d{4}" }));

        //    // Act
        //    var virtualPath = route.GetLink(context);

        //    // Assert
        //    Assert.Null(virtualPath);
        //}

        //[Fact]
        //public void RouteGenerationAcceptsConstraints()
        //{
        //    // Arrange
        //    var context = CreateRouteValuesContext(new { p1 = "hello", p2 = "1234" });

        //    var endpoint = CreateEndpoints(
        //        "{p1}/{p2}",
        //        new { p2 = "catchall" },
        //        true,
        //        new RouteValueDictionary(new { p2 = "\\d{4}" }));

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.NotNull(pathData);
        //    Assert.Equal("/hello/1234", link);
        //    
        //    
        //}

        //[Fact]
        //public void RouteWithCatchAllRejectsConstraints()
        //{
        //    // Arrange
        //    var context = CreateRouteValuesContext(new { p1 = "abcd" });

        //    var endpoint = CreateEndpoints(
        //        "{p1}/{*p2}",
        //        new { p2 = "catchall" },
        //        true,
        //        new RouteValueDictionary(new { p2 = "\\d{4}" }));

        //    // Act
        //    var virtualPath = route.GetLink(context);

        //    // Assert
        //    Assert.Null(virtualPath);
        //}

        //[Fact]
        //public void RouteWithCatchAllAcceptsConstraints()
        //{
        //    // Arrange
        //    var context = CreateRouteValuesContext(new { p1 = "hello", p2 = "1234" });

        //    var endpoint = CreateEndpoints(
        //        "{p1}/{*p2}",
        //        new { p2 = "catchall" },
        //        true,
        //        new RouteValueDictionary(new { p2 = "\\d{4}" }));

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.NotNull(pathData);
        //    Assert.Equal("/hello/1234", link);
        //    
        //    
        //}

        //[Fact]
        //public void GetLinkWithNonParameterConstraintReturnsUrlWithoutQueryString()
        //{
        //    // Arrange
        //    var context = CreateRouteValuesContext(new { p1 = "hello", p2 = "1234" });

        //    var target = new Mock<IRouteConstraint>();
        //    target
        //        .Setup(
        //            e => e.Match(
        //                It.IsAny<HttpContext>(),
        //                It.IsAny<IRouter>(),
        //                It.IsAny<string>(),
        //                It.IsAny<RouteValueDictionary>(),
        //                It.IsAny<RouteDirection>()))
        //        .Returns(true)
        //        .Verifiable();

        //    var endpoint = CreateEndpoints(
        //        "{p1}/{p2}",
        //        new { p2 = "catchall" },
        //        true,
        //        new RouteValueDictionary(new { p2 = target.Object }));

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.NotNull(pathData);
        //    Assert.Equal("/hello/1234", link);
        //    
        //    

        //    target.VerifyAll();
        //}

        //// Any ambient values from the current request should be visible to constraint, even
        //// if they have nothing to do with the route generating a link
        //[Fact]
        //public void GetLink_ConstraintsSeeAmbientValues()
        //{
        //    // Arrange
        //    var constraint = new CapturingConstraint();
        //    var endpoint = CreateEndpoints(
        //        template: "slug/{controller}/{action}",
        //        defaultValues: null,
        //        handleRequest: true,
        //        constraints: new { c = constraint });

        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Store" },
        //        ambientValues: new { Controller = "Home", action = "Blog", extra = "42" });

        //    var expectedValues = new RouteValueDictionary(
        //        new { controller = "Home", action = "Store", extra = "42" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/slug/Home/Store", link);
        //    
        //    

        //    Assert.Equal(expectedValues, constraint.Values);
        //}

        //// Non-parameter default values from the routing generating a link are not in the 'values'
        //// collection when constraints are processed.
        //[Fact]
        //public void GetLink_ConstraintsDontSeeDefaults_WhenTheyArentParameters()
        //{
        //    // Arrange
        //    var constraint = new CapturingConstraint();
        //    var endpoint = CreateEndpoints(
        //        template: "slug/{controller}/{action}",
        //        defaultValues: new { otherthing = "17" },
        //        handleRequest: true,
        //        constraints: new { c = constraint });

        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Store" },
        //        ambientValues: new { Controller = "Home", action = "Blog" });

        //    var expectedValues = new RouteValueDictionary(
        //        new { controller = "Home", action = "Store" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/slug/Home/Store", link);
        //    
        //    

        //    Assert.Equal(expectedValues, constraint.Values);
        //}

        //// Default values are visible to the constraint when they are used to fill a parameter.
        //[Fact]
        //public void GetLink_ConstraintsSeesDefault_WhenThereItsAParamter()
        //{
        //    // Arrange
        //    var constraint = new CapturingConstraint();
        //    var endpoint = CreateEndpoints(
        //        template: "slug/{controller}/{action}",
        //        defaultValues: new { action = "Index" },
        //        handleRequest: true,
        //        constraints: new { c = constraint });

        //    var context = CreateRouteValuesContext(
        //        values: new { controller = "Shopping" },
        //        ambientValues: new { Controller = "Home", action = "Blog" });

        //    var expectedValues = new RouteValueDictionary(
        //        new { controller = "Shopping", action = "Index" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/slug/Shopping", link);
        //    
        //    

        //    Assert.Equal(expectedValues, constraint.Values);
        //}

        //// Default values from the routing generating a link are in the 'values' collection when
        //// constraints are processed - IFF they are specified as values or ambient values.
        //[Fact]
        //public void GetLink_ConstraintsSeeDefaults_IfTheyAreSpecifiedOrAmbient()
        //{
        //    // Arrange
        //    var constraint = new CapturingConstraint();
        //    var endpoint = CreateEndpoints(
        //        template: "slug/{controller}/{action}",
        //        defaultValues: new { otherthing = "17", thirdthing = "13" },
        //        handleRequest: true,
        //        constraints: new { c = constraint });

        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Store", thirdthing = "13" },
        //        ambientValues: new { Controller = "Home", action = "Blog", otherthing = "17" });

        //    var expectedValues = new RouteValueDictionary(
        //        new { controller = "Home", action = "Store", otherthing = "17", thirdthing = "13" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/slug/Home/Store", link);
        //    
        //    

        //    Assert.Equal(expectedValues.OrderBy(kvp => kvp.Key), constraint.Values.OrderBy(kvp => kvp.Key));
        //}

        //[Fact]
        //public void GetLink_InlineConstraints_Success()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints("{controller}/{action}/{id:int}");
        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home", id = 4 });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/Home/Index/4", link);
        //    
        //    
        //}

        //[Fact]
        //public void GetLink_InlineConstraints_NonMatchingvalue()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints("{controller}/{action}/{id:int}");
        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home", id = "asf" });

        //    // Act
        //    var path = route.GetLink(context);

        //    // Assert
        //    Assert.Null(path);
        //}

        //[Fact]
        //public void GetLink_InlineConstraints_OptionalParameter_ValuePresent()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints("{controller}/{action}/{id:int?}");
        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home", id = 98 });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/Home/Index/98", link);
        //    
        //    
        //}

        //[Fact]
        //public void GetLink_InlineConstraints_OptionalParameter_ValueNotPresent()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints("{controller}/{action}/{id:int?}");
        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/Home/Index", link);
        //    
        //    
        //}

        //[Fact]
        //public void GetLink_InlineConstraints_OptionalParameter_ValuePresent_ConstraintFails()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints("{controller}/{action}/{id:int?}");
        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home", id = "sdfd" });

        //    // Act
        //    var path = route.GetLink(context);

        //    // Assert
        //    Assert.Null(path);
        //}

        //[Fact]
        //public void GetLink_InlineConstraints_CompositeInlineConstraint()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints("{controller}/{action}/{id:int:range(1,20)}");
        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home", id = 14 });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/Home/Index/14", link);
        //    
        //    
        //}

        //[Fact]
        //public void GetLink_InlineConstraints_CompositeConstraint_FromConstructor()
        //{
        //    // Arrange
        //    var constraint = new MaxLengthRouteConstraint(20);
        //    var endpoint = CreateEndpoints(
        //        template: "{controller}/{action}/{name:alpha}",
        //        defaultValues: null,
        //        handleRequest: true,
        //        constraints: new { name = constraint });

        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home", name = "products" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/Home/Index/products", link);
        //    
        //    
        //}

        [Fact]
        public void GetLink_OptionalParameter_ParameterPresentInValues()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                suppliedValues: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index/products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterNotPresentInValues()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                suppliedValues: new { action = "Index", controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterPresentInValuesAndDefaults()
        {
            // Arrange
            var endpoint = CreateEndpoint(
                template: "{controller}/{action}/{name?}",
                defaultValues: new { name = "default-products" });
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                suppliedValues: new { action = "Index", controller = "Home", name = "products" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index/products", link);
        }

        [Fact]
        public void GetLink_OptionalParameter_ParameterNotPresentInValues_PresentInDefaults()
        {
            // Arrange
            var endpoint = CreateEndpoint(
                template: "{controller}/{action}/{name?}",
                defaultValues: new { name = "products" });
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                suppliedValues: new { action = "Index", controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        [Fact]
        public void GetLink_ParameterNotPresentInTemplate_PresentInValues()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}/{name}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                suppliedValues: new { action = "Index", controller = "Home", name = "products", format = "json" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index/products?format=json", link);
        }

        //[Fact]
        //public void GetLink_OptionalParameter_FollowedByDotAfterSlash_ParameterPresent()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints(
        //        template: "{controller}/{action}/.{name?}",
        //        defaultValues: null,
        //        handleRequest: true,
        //        constraints: null);

        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home", name = "products" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/Home/Index/.products", link);
        //    
        //    
        //}

        //[Fact]
        //public void GetLink_OptionalParameter_FollowedByDotAfterSlash_ParameterNotPresent()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints(
        //        template: "{controller}/{action}/.{name?}",
        //        defaultValues: null,
        //        handleRequest: true,
        //        constraints: null);

        //    var context = CreateRouteValuesContext(
        //        values: new { action = "Index", controller = "Home" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/Home/Index/", link);
        //    
        //    
        //}

        [Fact]
        public void GetLink_OptionalParameter_InSimpleSegment()
        {
            // Arrange
            var endpoint = CreateEndpoint("{controller}/{action}/{name?}");
            var linkGenerator = CreateLinkGenerator();
            var context = CreateRouteValuesContext(
                suppliedValues: new { action = "Index", controller = "Home" });

            // Act
            var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

            // Assert
            Assert.Equal("/Home/Index", link);
        }

        //[Fact]
        //public void GetLink_TwoOptionalParameters_OneValueFromAmbientValues()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints("a/{b=15}/{c?}/{d?}");
        //    var linkGenerator = CreateLinkGenerator();
        //    var context = CreateRouteValuesContext(
        //        suppliedValues: new { },
        //        ambientValues: new { c = "17" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/a/15/17", link);
        //}

        //[Fact]
        //public void GetLink_OptionalParameterAfterDefault_OneValueFromAmbientValues()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints("a/{b=15}/{c?}");
        //    var linkGenerator = CreateLinkGenerator();
        //    var context = CreateRouteValuesContext(
        //        suppliedValues: new { },
        //        ambientValues: new { c = "17" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.Equal("/a/15/17", link);
        //}

        //[Fact]
        //public void GetLink_TwoOptionalParametersAfterDefault_LastValueFromAmbientValues()
        //{
        //    // Arrange
        //    var endpoint = CreateEndpoints(
        //        template: "a/{b=15}/{c?}/{d?}",
        //        defaultValues: null,
        //        handleRequest: true,
        //        constraints: null);

        //    var context = CreateRouteValuesContext(
        //        values: new { },
        //        ambientValues: new { d = "17" });

        //    // Act
        //    var link = linkGenerator.GetLink(new[] { endpoint }, context.ExplicitValues, context.AmbientValues);

        //    // Assert
        //    Assert.NotNull(pathData);
        //    Assert.Equal("/a", link);
        //    
        //    
        //}

        private RouteValuesBasedEndpointFinderContext CreateRouteValuesContext(object suppliedValues, object ambientValues = null)
        {
            var context = new RouteValuesBasedEndpointFinderContext();
            context.ExplicitValues = new RouteValueDictionary(suppliedValues);
            context.AmbientValues = new RouteValueDictionary(ambientValues);
            return context;
        }

        private MatcherEndpoint CreateEndpoint(string template, object defaultValues = null)
        {
            var defaults = defaultValues == null ? new RouteValueDictionary() : new RouteValueDictionary(defaultValues);
            return new MatcherEndpoint(
                next => (httpContext) => Task.CompletedTask,
                template,
                defaults,
                new RouteValueDictionary(),
                new List<MatchProcessorReference>(),
                0,
                EndpointMetadataCollection.Empty,
                null);
        }

        private ILinkGenerator CreateLinkGenerator()
        {
            return new DefaultLinkGenerator(
                new DefaultMatchProcessorFactory(
                    Options.Create(new RouteOptions()),
                    Mock.Of<IServiceProvider>()),
                new DefaultObjectPool<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy()),
                NullLogger<DefaultLinkGenerator>.Instance);
        }
    }
}
