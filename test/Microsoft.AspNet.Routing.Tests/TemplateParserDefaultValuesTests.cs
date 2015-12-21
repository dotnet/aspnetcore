// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class TemplateParserDefaultValuesTests
    {
        private static IInlineConstraintResolver _inlineConstraintResolver = GetInlineConstraintResolver();

        [Fact]
        public void InlineDefaultValueSpecified_InlineValueIsUsed()
        {
            // Arrange & Act
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                "{controller}/{action}/{id:int=12}",
                defaults: null,
                constraints: null);

            // Assert
            var defaults = ((Route)routeBuilder.Routes[0]).Defaults;
            Assert.Equal("12", defaults["id"]);
        }

        [Theory]
        [InlineData(@"{controller}/{action}/{p1:regex(([}}])\w+)=}}asd}", "}asd")]
        [InlineData(@"{p1:regex(^\d{{1,2}}\/\d{{1,2}}\/\d{{4}}$)=12/12/1234}", @"12/12/1234")]
        public void InlineDefaultValueSpecified_WithSpecialCharacters(string template, string value)
        {
            // Arrange & Act
            var routeBuilder = CreateRouteBuilder();

            // Act
            routeBuilder.MapRoute("mockName",
                template,
                defaults: null,
                constraints: null);

            // Assert
            var defaults = ((Route)routeBuilder.Routes[0]).Defaults;
            Assert.Equal(value, defaults["p1"]);
        }

        [Fact]
        public void ExplicitDefaultValueSpecified_WithInlineDefaultValue_Throws()
        {
            // Arrange
            var routeBuilder = CreateRouteBuilder();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(
                                () => routeBuilder.MapRoute("mockName",
                                                            "{controller}/{action}/{id:int=12}",
                                                            defaults: new { id = 13 },
                                                            constraints: null));

            var message = "The route parameter 'id' has both an inline default value and an explicit default" +
                          " value specified. A route parameter cannot contain an inline default value when" +
                          " a default value is specified explicitly. Consider removing one of them.";
            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void EmptyDefaultValue_WithOptionalParameter_Throws()
        {
            // Arrange
            var message = "An optional parameter cannot have default value." + Environment.NewLine +
                          "Parameter name: routeTemplate";
            var routeBuilder = CreateRouteBuilder();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                                () => routeBuilder.MapRoute("mockName",
                                                            "{controller}/{action}/{id:int=?}",
                                                            defaults: new { id = 13 },
                                                            constraints: null));

            Assert.Equal(message, ex.Message);
        }

        [Fact]
        public void NonEmptyDefaultValue_WithOptionalParameter_Throws()
        {
            // Arrange
            var message = "An optional parameter cannot have default value." + Environment.NewLine +
                          "Parameter name: routeTemplate";
            var routeBuilder = CreateRouteBuilder();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                routeBuilder.MapRoute(
                    "mockName",
                    "{controller}/{action}/{id:int=12?}",
                    defaults: new { id = 13 },
                    constraints: null);
            });

            Assert.Equal(message, ex.Message);
        }

        private static IRouteBuilder CreateRouteBuilder()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IInlineConstraintResolver>(_inlineConstraintResolver);

            var applicationBuilder = Mock.Of<IApplicationBuilder>();
            applicationBuilder.ApplicationServices = services.BuildServiceProvider();

            var routeBuilder = new RouteBuilder(applicationBuilder);
            routeBuilder.DefaultHandler = Mock.Of<IRouter>();
            return routeBuilder;
        }

        private static IInlineConstraintResolver GetInlineConstraintResolver()
        {
            var services = new ServiceCollection().AddOptions();
            var serviceProvider = services.BuildServiceProvider();
            var accessor = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();
            return new DefaultInlineConstraintResolver(accessor);
        }
    }
}
