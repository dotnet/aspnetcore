// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Routing.Constraints;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    public class RoutePatternFactoryTest
    {
        [Fact]
        public void Pattern_MergesDefaultValues()
        {
            // Arrange
            var template = "{a}/{b}/{c=19}";
            var defaults = new { a = "15", b = 17 };
            var constraints = new { };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var actual = RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments);

            // Assert
            Assert.Equal("15", actual.GetParameter("a").Default);
            Assert.Equal(17, actual.GetParameter("b").Default);
            Assert.Equal("19", actual.GetParameter("c").Default);

            Assert.Collection(
                actual.Defaults.OrderBy(kvp => kvp.Key),
                kvp => { Assert.Equal("a", kvp.Key); Assert.Equal("15", kvp.Value); },
                kvp => { Assert.Equal("b", kvp.Key); Assert.Equal(17, kvp.Value); },
                kvp => { Assert.Equal("c", kvp.Key); Assert.Equal("19", kvp.Value); });
        }

        [Fact]
        public void Pattern_ExtraDefaultValues()
        {
            // Arrange
            var template = "{a}/{b}/{c}";
            var defaults = new { d = "15", e = 17 };
            var constraints = new { };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var actual = RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments);

            // Assert
            Assert.Collection(
                actual.Defaults.OrderBy(kvp => kvp.Key),
                kvp => { Assert.Equal("d", kvp.Key); Assert.Equal("15", kvp.Value); },
                kvp => { Assert.Equal("e", kvp.Key); Assert.Equal(17, kvp.Value); });
        }

        [Fact]
        public void Pattern_DuplicateDefaultValue_Throws()
        {
            // Arrange
            var template = "{a=13}/{b}/{c}";
            var defaults = new { a = "15", };
            var constraints = new { };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments));

            // Assert
            Assert.Equal(
                "The route parameter 'a' has both an inline default value and an explicit default " +
                "value specified. A route parameter cannot contain an inline default value when a " +
                "default value is specified explicitly. Consider removing one of them.",
                ex.Message);
        }

        [Fact]
        public void Pattern_OptionalParameterDefaultValue_Throws()
        {
            // Arrange
            var template = "{a}/{b}/{c?}";
            var defaults = new { c = "15", };
            var constraints = new { };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments));

            // Assert
            Assert.Equal(
                "An optional parameter cannot have default value.",
                ex.Message);
        }

        [Fact]
        public void Pattern_MergesConstraints()
        {
            // Arrange
            var template = "{a:int}/{b}/{c}";
            var defaults = new { };
            var constraints = new { a = new RegexRouteConstraint("foo"), b = new RegexRouteConstraint("bar") };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var actual = RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments);

            // Assert
            Assert.Collection(
                actual.GetParameter("a").Constraints,
                c => Assert.IsType<RegexRouteConstraint>(c.Constraint),
                c => Assert.Equal("int", c.Content));
            Assert.Collection(
                actual.GetParameter("b").Constraints,
                c => Assert.IsType<RegexRouteConstraint>(c.Constraint));

            Assert.Collection(
                actual.Constraints.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("a", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.IsType<RegexRouteConstraint>(c.Constraint),
                        c => Assert.Equal("int", c.Content));
                },
                kvp =>
                {
                    Assert.Equal("b", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.IsType<RegexRouteConstraint>(c.Constraint));
                });
        }

        [Fact]
        public void Pattern_ExtraConstraints()
        {
            // Arrange
            var template = "{a}/{b}/{c}";
            var defaults = new { };
            var constraints = new { d = new RegexRouteConstraint("foo"), e = new RegexRouteConstraint("bar") };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var actual = RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments);

            // Assert
            Assert.Collection(
                actual.Constraints.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("d", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.IsType<RegexRouteConstraint>(c.Constraint));
                },
                kvp =>
                {
                    Assert.Equal("e", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.IsType<RegexRouteConstraint>(c.Constraint));
                });
        }

        [Fact]
        public void Pattern_CreatesConstraintFromString()
        {
            // Arrange
            var template = "{a}/{b}/{c}";
            var defaults = new { };
            var constraints = new { d = "foo", };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var actual = RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments);

            // Assert
            Assert.Collection(
                actual.Constraints.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("d", kvp.Key);
                    var regex = Assert.IsType<RegexRouteConstraint>(Assert.Single(kvp.Value).Constraint);
                    Assert.Equal("^(foo)$", regex.Constraint.ToString());
                });
        }

        [Fact]
        public void Pattern_InvalidConstraintTypeThrows()
        {
            // Arrange
            var template = "{a}/{b}/{c}";
            var defaults = new { };
            var constraints = new { d = 17, };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments));

            // Assert
            Assert.Equal(
                "The constraint entry 'd' - '17' must have a string value or be of a type " +
                "which implements 'Microsoft.AspNetCore.Routing.IRouteConstraint'.",
                ex.Message);
        }
    }
}
