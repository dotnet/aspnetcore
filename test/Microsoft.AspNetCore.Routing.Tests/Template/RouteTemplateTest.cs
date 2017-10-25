// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Dispatcher.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Template
{
    public class RouteTemplateTest
    {
        [Fact]
        public void ToRoutePattern_ConvertsToRoutePattern_MultipleSegments()
        {
            // Arrange
            var routeTemplate = TemplateParser.Parse("api/{foo}");

            // Act
            var routePattern = routeTemplate.ToRoutePattern();

            // Assert
            Assert.Same(routeTemplate.TemplateText, routePattern.RawText);
            Assert.Collection(
                routePattern.PathSegments,
                s =>
                {
                    Assert.Collection(
                        s.Parts,
                        p =>
                        {
                            var literal = Assert.IsType<RoutePatternLiteral>(p);
                            Assert.Equal("api", literal.Content);
                        });
                },
                s =>
                {
                    Assert.Collection(
                        s.Parts,
                        p =>
                        {
                            var parameter = Assert.IsType<RoutePatternParameter>(p);
                            Assert.Equal("foo", parameter.Name);
                            Assert.Equal(RoutePatternParameterKind.Standard, parameter.ParameterKind);
                            Assert.Null(parameter.DefaultValue);
                            Assert.Empty(parameter.Constraints);
                        });
                });
        }

        [Fact]
        public void ToRoutePattern_ConvertsToRoutePattern_ComplexSegment()
        {
            // Arrange
            var routeTemplate = TemplateParser.Parse("api-{foo}");

            // Act
            var routePattern = routeTemplate.ToRoutePattern();

            // Assert
            Assert.Same(routeTemplate.TemplateText, routePattern.RawText);
            Assert.Collection(
                routePattern.PathSegments,
                s =>
                {
                    Assert.Collection(
                        s.Parts,
                        p =>
                        {
                            var literal = Assert.IsType<RoutePatternLiteral>(p);
                            Assert.Equal("api-", literal.Content);
                        },
                        p =>
                        {
                            var parameter = Assert.IsType<RoutePatternParameter>(p);
                            Assert.Equal("foo", parameter.Name);
                            Assert.Equal(RoutePatternParameterKind.Standard, parameter.ParameterKind);
                            Assert.Null(parameter.DefaultValue);
                            Assert.Empty(parameter.Constraints);
                        });
                });
        }

        [Fact]
        public void ToRoutePattern_ConvertsToRoutePattern_CatchAllParameter()
        {
            // Arrange
            var routeTemplate = TemplateParser.Parse("{*foo}");

            // Act
            var routePattern = routeTemplate.ToRoutePattern();

            // Assert
            Assert.Same(routeTemplate.TemplateText, routePattern.RawText);
            Assert.Collection(
                routePattern.PathSegments,
                s =>
                {
                    Assert.Collection(
                        s.Parts,
                        p =>
                        {
                            var parameter = Assert.IsType<RoutePatternParameter>(p);
                            Assert.Equal("foo", parameter.Name);
                            Assert.Equal(RoutePatternParameterKind.CatchAll, parameter.ParameterKind);
                            Assert.Null(parameter.DefaultValue);
                            Assert.Empty(parameter.Constraints);
                        });
                });
        }

        [Fact]
        public void ToRoutePattern_ConvertsToRoutePattern_OptionalParameter()
        {
            // Arrange
            var routeTemplate = TemplateParser.Parse("{foo?}");

            // Act
            var routePattern = routeTemplate.ToRoutePattern();

            // Assert
            Assert.Same(routeTemplate.TemplateText, routePattern.RawText);
            Assert.Collection(
                routePattern.PathSegments,
                s =>
                {
                    Assert.Collection(
                        s.Parts,
                        p =>
                        {
                            var parameter = Assert.IsType<RoutePatternParameter>(p);
                            Assert.Equal("foo", parameter.Name);
                            Assert.Equal(RoutePatternParameterKind.Optional, parameter.ParameterKind);
                            Assert.Null(parameter.DefaultValue);
                            Assert.Empty(parameter.Constraints);
                        });
                });
        }

        [Fact]
        public void ToRoutePattern_ConvertsToRoutePattern_Constraints()
        {
            // Arrange
            var routeTemplate = TemplateParser.Parse("{foo:bar:baz}");

            // Act
            var routePattern = routeTemplate.ToRoutePattern();

            // Assert
            Assert.Same(routeTemplate.TemplateText, routePattern.RawText);
            Assert.Collection(
                routePattern.PathSegments,
                s =>
                {
                    Assert.Collection(
                        s.Parts,
                        p =>
                        {
                            var parameter = Assert.IsType<RoutePatternParameter>(p);
                            Assert.Equal("foo", parameter.Name);
                            Assert.Equal(RoutePatternParameterKind.Standard, parameter.ParameterKind);
                            Assert.Null(parameter.DefaultValue);
                            Assert.Collection(
                                parameter.Constraints,
                                c =>
                                {
                                    Assert.Equal("bar", c.Content);
                                },
                                c =>
                                {
                                    Assert.Equal("baz", c.Content);
                                });
                        });
                });
        }

        [Fact]
        public void ToRoutePattern_ConvertsToRoutePattern_OptionalSeparator()
        {
            // Arrange
            var routeTemplate = TemplateParser.Parse("{bar}.{foo?}");

            // Act
            var routePattern = routeTemplate.ToRoutePattern();

            // Assert
            Assert.Same(routeTemplate.TemplateText, routePattern.RawText);
            Assert.Collection(
                routePattern.PathSegments,
                s =>
                {
                    Assert.Collection(
                        s.Parts,
                        p =>
                        {
                            var parameter = Assert.IsType<RoutePatternParameter>(p);
                            Assert.Equal("bar", parameter.Name);
                            Assert.Equal(RoutePatternParameterKind.Standard, parameter.ParameterKind);
                            Assert.Null(parameter.DefaultValue);
                            Assert.Empty(parameter.Constraints);
                        },
                        p =>
                        {
                            var separator = Assert.IsType<RoutePatternSeparator>(p);
                            Assert.Equal(".", separator.Content);
                        },
                        p =>
                        {
                            var parameter = Assert.IsType<RoutePatternParameter>(p);
                            Assert.Equal("foo", parameter.Name);
                            Assert.Equal(RoutePatternParameterKind.Optional, parameter.ParameterKind);
                            Assert.Null(parameter.DefaultValue);
                            Assert.Empty(parameter.Constraints);
                        });
                });
        }
    }
}
