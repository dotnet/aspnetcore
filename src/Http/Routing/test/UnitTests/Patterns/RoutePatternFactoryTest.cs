// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Template;
using Moq;
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
        public void Pattern_DifferentDuplicateDefaultValue_Throws()
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
        public void Pattern_SameDuplicateDefaultValue()
        {
            // Arrange
            var template = "{a=13}/{b}/{c}";
            var defaults = new { a = "13", };
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
                actual.Defaults,
                kvp => { Assert.Equal("a", kvp.Key); Assert.Equal("13", kvp.Value); });
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
                actual.GetParameter("a").ParameterPolicies,
                c => Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy),
                c => Assert.Equal("int", c.Content));
            Assert.Collection(
                actual.GetParameter("b").ParameterPolicies,
                c => Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy));

            Assert.Collection(
                actual.ParameterPolicies.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("a", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy),
                        c => Assert.Equal("int", c.Content));
                },
                kvp =>
                {
                    Assert.Equal("b", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy));
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
                actual.ParameterPolicies.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("d", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy));
                },
                kvp =>
                {
                    Assert.Equal("e", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy));
                });
        }

        [Fact]
        public void Pattern_ExtraConstraints_MultipleConstraintsForKey()
        {
            // Arrange
            var template = "{a}/{b}/{c}";
            var defaults = new { };
            var constraints = new { d = new object[] { new RegexRouteConstraint("foo"), new RegexRouteConstraint("bar"), "baz" } };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var actual = RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments);

            // Assert
            Assert.Collection(
                actual.ParameterPolicies.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("d", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.Equal("foo", Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy).Constraint.ToString()),
                        c => Assert.Equal("bar", Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy).Constraint.ToString()),
                        c => Assert.Equal("^(baz)$", Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy).Constraint.ToString()));
                });
        }

        [Fact]
        public void Pattern_ExtraConstraints_MergeMultipleConstraintsForKey()
        {
            // Arrange
            var template = "{a:int}/{b}/{c:int}";
            var defaults = new { };
            var constraints = new { b = "fizz", c = new object[] { new RegexRouteConstraint("foo"), new RegexRouteConstraint("bar"), "baz" } };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var actual = RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments);

            // Assert
            Assert.Collection(
                actual.ParameterPolicies.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("a", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.Equal("int", c.Content));
                },
                kvp =>
                {
                    Assert.Equal("b", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.Equal("^(fizz)$", Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy).Constraint.ToString()));
                },
                kvp =>
                {
                    Assert.Equal("c", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.Equal("foo", Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy).Constraint.ToString()),
                        c => Assert.Equal("bar", Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy).Constraint.ToString()),
                        c => Assert.Equal("^(baz)$", Assert.IsType<RegexRouteConstraint>(c.ParameterPolicy).Constraint.ToString()),
                        c => Assert.Equal("int", c.Content));
                });
        }

        [Fact]
        public void Pattern_ExtraConstraints_NestedArray_Throws()
        {
            // Arrange
            var template = "{a}/{b}/{c:int}";
            var defaults = new { };
            var constraints = new { c = new object[] { new object[0] } };

            var original = RoutePatternFactory.Parse(template);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                RoutePatternFactory.Pattern(
                    original.RawText,
                    defaults,
                    constraints,
                    original.PathSegments);
            });
        }

        [Fact]
        public void Pattern_ExtraConstraints_RouteConstraint()
        {
            // Arrange
            var template = "{a}/{b}/{c}";
            var defaults = new { };
            var constraints = new { d = Mock.Of<IRouteConstraint>(), e = Mock.Of<IRouteConstraint>(), };

            var original = RoutePatternFactory.Parse(template);

            // Act
            var actual = RoutePatternFactory.Pattern(
                original.RawText,
                defaults,
                constraints,
                original.PathSegments);

            // Assert
            Assert.Collection(
                actual.ParameterPolicies.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("d", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.NotNull(c.ParameterPolicy));
                },
                kvp =>
                {
                    Assert.Equal("e", kvp.Key);
                    Assert.Collection(
                        kvp.Value,
                        c => Assert.NotNull(c.ParameterPolicy));
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
                actual.ParameterPolicies.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("d", kvp.Key);
                    var regex = Assert.IsType<RegexRouteConstraint>(Assert.Single(kvp.Value).ParameterPolicy);
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
                $"Invalid constraint '17'. A constraint must be of type 'string' or '{typeof(IRouteConstraint)}'.",
                ex.Message);
        }

        [Fact]
        public void Pattern_ArrayOfSegments_ShouldMakeCopyOfArrayOfSegments()
        {
            // Arrange
            var literalPartA = RoutePatternFactory.LiteralPart("A");
            var paramPartB = RoutePatternFactory.ParameterPart("B");
            var paramPartC = RoutePatternFactory.ParameterPart("C");
            var paramPartD = RoutePatternFactory.ParameterPart("D");
            var segments = new[]
                {
                    RoutePatternFactory.Segment(literalPartA, paramPartB),
                    RoutePatternFactory.Segment(paramPartC, literalPartA),
                    RoutePatternFactory.Segment(paramPartD),
                    RoutePatternFactory.Segment(literalPartA)
                };

            // Act
            var actual = RoutePatternFactory.Pattern(segments);
            segments[1] = RoutePatternFactory.Segment(RoutePatternFactory.ParameterPart("E"));
            Array.Resize(ref segments, 2);

            // Assert
            Assert.Equal(3, actual.Parameters.Count);
            Assert.Same(paramPartB, actual.Parameters[0]);
            Assert.Same(paramPartC, actual.Parameters[1]);
            Assert.Same(paramPartD, actual.Parameters[2]);
        }

        [Fact]
        public void Pattern_RawTextAndArrayOfSegments_ShouldMakeCopyOfArrayOfSegments()
        {
            // Arrange
            var rawText = "raw";
            var literalPartA = RoutePatternFactory.LiteralPart("A");
            var paramPartB = RoutePatternFactory.ParameterPart("B");
            var paramPartC = RoutePatternFactory.ParameterPart("C");
            var paramPartD = RoutePatternFactory.ParameterPart("D");
            var segments = new[]
                {
                    RoutePatternFactory.Segment(literalPartA, paramPartB),
                    RoutePatternFactory.Segment(paramPartC, literalPartA),
                    RoutePatternFactory.Segment(paramPartD),
                    RoutePatternFactory.Segment(literalPartA)
                };

            // Act
            var actual = RoutePatternFactory.Pattern(rawText, segments);
            segments[1] = RoutePatternFactory.Segment(RoutePatternFactory.ParameterPart("E"));
            Array.Resize(ref segments, 2);

            // Assert
            Assert.Equal(3, actual.Parameters.Count);
            Assert.Same(paramPartB, actual.Parameters[0]);
            Assert.Same(paramPartC, actual.Parameters[1]);
            Assert.Same(paramPartD, actual.Parameters[2]);
        }

        [Fact]
        public void Pattern_DefaultsAndParameterPoliciesAndArrayOfSegments_ShouldMakeCopyOfArrayOfSegments()
        {
            // Arrange
            object defaults = new { B = 12, C = 4 };
            object parameterPolicies = null;
            var literalPartA = RoutePatternFactory.LiteralPart("A");
            var paramPartB = RoutePatternFactory.ParameterPart("B");
            var paramPartC = RoutePatternFactory.ParameterPart("C");
            var paramPartD = RoutePatternFactory.ParameterPart("D");
            var segments = new[]
                {
                    RoutePatternFactory.Segment(literalPartA, paramPartB),
                    RoutePatternFactory.Segment(paramPartC, literalPartA),
                    RoutePatternFactory.Segment(paramPartD),
                    RoutePatternFactory.Segment(literalPartA)
                };

            // Act
            var actual = RoutePatternFactory.Pattern(defaults, parameterPolicies, segments);
            segments[1] = RoutePatternFactory.Segment(RoutePatternFactory.ParameterPart("E"));
            Array.Resize(ref segments, 2);

            // Assert
            Assert.Equal(3, actual.Parameters.Count);
            Assert.Equal(paramPartB.Name, actual.Parameters[0].Name);
            Assert.Equal(12, actual.Parameters[0].Default);
            Assert.Null(paramPartB.Default);
            Assert.NotSame(paramPartB, actual.Parameters[0]);
            Assert.Equal(paramPartC.Name, actual.Parameters[1].Name);
            Assert.Equal(4, actual.Parameters[1].Default);
            Assert.NotSame(paramPartC, actual.Parameters[1]);
            Assert.Null(paramPartC.Default);
            Assert.Equal(paramPartD.Name, actual.Parameters[2].Name);
            Assert.Null(actual.Parameters[2].Default);
            Assert.Same(paramPartD, actual.Parameters[2]);
            Assert.Null(paramPartD.Default);
        }

        [Fact]
        public void Pattern_RawTextAndDefaultsAndParameterPoliciesAndArrayOfSegments_ShouldMakeCopyOfArrayOfSegments()
        {
            // Arrange
            var rawText = "raw";
            object defaults = new { B = 12, C = 4 };
            object parameterPolicies = null;
            var literalPartA = RoutePatternFactory.LiteralPart("A");
            var paramPartB = RoutePatternFactory.ParameterPart("B");
            var paramPartC = RoutePatternFactory.ParameterPart("C");
            var paramPartD = RoutePatternFactory.ParameterPart("D");
            var segments = new[]
                {
                    RoutePatternFactory.Segment(literalPartA, paramPartB),
                    RoutePatternFactory.Segment(paramPartC, literalPartA),
                    RoutePatternFactory.Segment(paramPartD),
                    RoutePatternFactory.Segment(literalPartA)
                };

            // Act
            var actual = RoutePatternFactory.Pattern(rawText, defaults, parameterPolicies, segments);
            segments[1] = RoutePatternFactory.Segment(RoutePatternFactory.ParameterPart("E"));
            Array.Resize(ref segments, 2);

            // Assert
            Assert.Equal(3, actual.Parameters.Count);
            Assert.Equal(paramPartB.Name, actual.Parameters[0].Name);
            Assert.Equal(12, actual.Parameters[0].Default);
            Assert.Null(paramPartB.Default);
            Assert.NotSame(paramPartB, actual.Parameters[0]);
            Assert.Equal(paramPartC.Name, actual.Parameters[1].Name);
            Assert.Equal(4, actual.Parameters[1].Default);
            Assert.NotSame(paramPartC, actual.Parameters[1]);
            Assert.Null(paramPartC.Default);
            Assert.Equal(paramPartD.Name, actual.Parameters[2].Name);
            Assert.Null(actual.Parameters[2].Default);
            Assert.Same(paramPartD, actual.Parameters[2]);
            Assert.Null(paramPartD.Default);
        }

        [Fact]
        public void Parse_WithRequiredValues()
        {
            // Arrange
            var template = "{controller=Home}/{action=Index}/{id?}";
            var defaults = new { area = "Admin", };
            var policies = new { };
            var requiredValues = new { area = "Admin", controller = "Store", action = "Index", };

            // Act
            var action = RoutePatternFactory.Parse(template, defaults, policies, requiredValues);

            // Assert
            Assert.Collection(
                action.RequiredValues.OrderBy(kvp => kvp.Key),
                kvp => { Assert.Equal("action", kvp.Key); Assert.Equal("Index", kvp.Value); },
                kvp => { Assert.Equal("area", kvp.Key); Assert.Equal("Admin", kvp.Value); },
                kvp => { Assert.Equal("controller", kvp.Key); Assert.Equal("Store", kvp.Value); });
        }

        [Fact]
        public void Parse_WithRequiredValues_AllowsNullRequiredValue()
        {
            // Arrange
            var template = "{controller=Home}/{action=Index}/{id?}";
            var defaults = new { };
            var policies = new { };
            var requiredValues = new { area = (string)null, controller = "Store", action = "Index", };

            // Act
            var action = RoutePatternFactory.Parse(template, defaults, policies, requiredValues);

            // Assert
            Assert.Collection(
                action.RequiredValues.OrderBy(kvp => kvp.Key),
                kvp => { Assert.Equal("action", kvp.Key); Assert.Equal("Index", kvp.Value); },
                kvp => { Assert.Equal("area", kvp.Key); Assert.Null(kvp.Value); },
                kvp => { Assert.Equal("controller", kvp.Key); Assert.Equal("Store", kvp.Value); });
        }

        [Fact]
        public void Parse_WithRequiredValues_AllowsEmptyRequiredValue()
        {
            // Arrange
            var template = "{controller=Home}/{action=Index}/{id?}";
            var defaults = new { };
            var policies = new { };
            var requiredValues = new { area = "", controller = "Store", action = "Index", };

            // Act
            var action = RoutePatternFactory.Parse(template, defaults, policies, requiredValues);

            // Assert
            Assert.Collection(
                action.RequiredValues.OrderBy(kvp => kvp.Key),
                kvp => { Assert.Equal("action", kvp.Key); Assert.Equal("Index", kvp.Value); },
                kvp => { Assert.Equal("area", kvp.Key); Assert.Equal("", kvp.Value); },
                kvp => { Assert.Equal("controller", kvp.Key); Assert.Equal("Store", kvp.Value); });
        }

        [Fact]
        public void Parse_WithRequiredValues_ThrowsForNonParameterNonDefault()
        {
            // Arrange
            var template = "{controller=Home}/{action=Index}/{id?}";
            var defaults = new { };
            var policies = new { };
            var requiredValues = new { area = "Admin", controller = "Store", action = "Index", };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                var action = RoutePatternFactory.Parse(template, defaults, policies, requiredValues);
            });

            // Assert
            Assert.Equal(
                "No corresponding parameter or default value could be found for the required value " +
                "'area=Admin'. A non-null required value must correspond to a route parameter or the " +
                "route pattern must have a matching default value.", 
                exception.Message);
        }
        
        [Fact]
        public void ParameterPart_ParameterNameAndDefaultAndParameterKindAndArrayOfParameterPolicies_ShouldMakeCopyOfParameterPolicies()
        {
            // Arrange (going through hoops to get an array of RoutePatternParameterPolicyReference)
            const string name = "Id";
            var defaults = new { a = "13", };
            var x = new InlineConstraint("x");
            var y = new InlineConstraint("y");
            var z = new InlineConstraint("z");
            var constraints = new[] { x, y, z };
            var templatePart = TemplatePart.CreateParameter("t", false, false, null, constraints);
            var routePatternParameterPart = (RoutePatternParameterPart) templatePart.ToRoutePatternPart();
            var policies = routePatternParameterPart.ParameterPolicies.ToArray();

            // Act
            var parameterPart = RoutePatternFactory.ParameterPart(name, defaults, RoutePatternParameterKind.Standard, policies);
            policies[0] = null;
            Array.Resize(ref policies, 2);

            // Assert
            Assert.NotNull(parameterPart.ParameterPolicies);
            Assert.Equal(3, parameterPart.ParameterPolicies.Count);
            Assert.NotNull(parameterPart.ParameterPolicies[0]);
            Assert.NotNull(parameterPart.ParameterPolicies[1]);
            Assert.NotNull(parameterPart.ParameterPolicies[2]);
        }

        [Fact]
        public void ParameterPart_ParameterNameAndDefaultAndParameterKindAndEnumerableOfParameterPolicies_ShouldMakeCopyOfParameterPolicies()
        {
            // Arrange (going through hoops to get an enumerable of RoutePatternParameterPolicyReference)
            const string name = "Id";
            var defaults = new { a = "13", };
            var x = new InlineConstraint("x");
            var y = new InlineConstraint("y");
            var z = new InlineConstraint("z");
            var constraints = new[] { x, y, z };
            var templatePart = TemplatePart.CreateParameter("t", false, false, null, constraints);
            var routePatternParameterPart = (RoutePatternParameterPart)templatePart.ToRoutePatternPart();
            var policies = routePatternParameterPart.ParameterPolicies.ToList();

            // Act
            var parameterPart = RoutePatternFactory.ParameterPart(name, defaults, RoutePatternParameterKind.Standard, policies);
            policies[0] = null;
            policies.RemoveAt(1);

            // Assert
            Assert.NotNull(parameterPart.ParameterPolicies);
            Assert.Equal(3, parameterPart.ParameterPolicies.Count);
            Assert.NotNull(parameterPart.ParameterPolicies[0]);
            Assert.NotNull(parameterPart.ParameterPolicies[1]);
            Assert.NotNull(parameterPart.ParameterPolicies[2]);
        }

        [Fact]
        public void Segment_EnumerableOfParts()
        {
            // Arrange
            var paramPartB = RoutePatternFactory.ParameterPart("B");
            var paramPartC = RoutePatternFactory.ParameterPart("C");
            var paramPartD = RoutePatternFactory.ParameterPart("D");
            var parts = new[] { paramPartB, paramPartC, paramPartD };

            // Act
            var actual = RoutePatternFactory.Segment((IEnumerable<RoutePatternParameterPart>) parts);
            parts[1] = RoutePatternFactory.ParameterPart("E");
            Array.Resize(ref parts, 2);

            // Assert
            Assert.Equal(3, actual.Parts.Count);
            Assert.Same(paramPartB, actual.Parts[0]);
            Assert.Same(paramPartC, actual.Parts[1]);
            Assert.Same(paramPartD, actual.Parts[2]);
        }

        [Fact]
        public void Segment_ArrayOfParts()
        {
            // Arrange
            var paramPartB = RoutePatternFactory.ParameterPart("B");
            var paramPartC = RoutePatternFactory.ParameterPart("C");
            var paramPartD = RoutePatternFactory.ParameterPart("D");
            var parts = new[] { paramPartB, paramPartC, paramPartD };

            // Act
            var actual = RoutePatternFactory.Segment(parts);
            parts[1] = RoutePatternFactory.ParameterPart("E");
            Array.Resize(ref parts, 2);

            // Assert
            Assert.Equal(3, actual.Parts.Count);
            Assert.Same(paramPartB, actual.Parts[0]);
            Assert.Same(paramPartC, actual.Parts[1]);
            Assert.Same(paramPartD, actual.Parts[2]);
        }
    }
}
