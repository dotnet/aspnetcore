// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

#if NET45
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class ConstraintsBuilderTests
    {
        [Theory]
        [MemberData("EmptyAndNullDictionary")]
        public void ConstraintBuilderReturnsNull_OnNullOrEmptyInput(IDictionary<string, object> input)
        {
            var result = RouteConstraintBuilder.BuildConstraints(input);

            Assert.Null(result);
        }

        [Theory]
        [MemberData("EmptyAndNullDictionary")]
        public void ConstraintBuilderWithTemplateReturnsNull_OnNullOrEmptyInput(IDictionary<string, object> input)
        {
            var result = RouteConstraintBuilder.BuildConstraints(input, "{controller}");

            Assert.Null(result);
        }

        [Fact]
        public void GetRouteDataWithConstraintsThatIsAStringCreatesARegex()
        {
            // Arrange
            var dictionary = new RouteValueDictionary(new { controller = "abc" });
            var constraintDictionary = RouteConstraintBuilder.BuildConstraints(dictionary);

            // Assert
            Assert.Equal(1, constraintDictionary.Count);
            Assert.Equal("controller", constraintDictionary.First().Key);

            var constraint = constraintDictionary["controller"];

            Assert.IsType<RegexConstraint>(constraint);
        }

        [Fact]
        public void GetRouteDataWithConstraintsThatIsCustomConstraint_IsPassThrough()
        {
            // Arrange
            var originalConstraint = new Mock<IRouteConstraint>().Object;

            var dictionary = new RouteValueDictionary(new { controller = originalConstraint });
            var constraintDictionary = RouteConstraintBuilder.BuildConstraints(dictionary);

            // Assert
            Assert.Equal(1, constraintDictionary.Count);
            Assert.Equal("controller", constraintDictionary.First().Key);

            var constraint = constraintDictionary["controller"];

            Assert.Equal(originalConstraint, constraint);
        }

        [Fact]
        public void GetRouteDataWithConstraintsThatIsNotStringOrCustomConstraint_Throws()
        {
            // Arrange
            var dictionary = new RouteValueDictionary(new { controller = new RouteValueDictionary() });

            ExceptionAssert.Throws<InvalidOperationException>(
                () => RouteConstraintBuilder.BuildConstraints(dictionary),
                "The constraint entry 'controller' must have a string value or be of a type which implements '" +
                typeof(IRouteConstraint) + "'.");
        }

        [Fact]
        public void RouteTemplateGetRouteDataWithConstraintsThatIsNotStringOrCustomConstraint_Throws()
        {
            // Arrange
            var dictionary = new RouteValueDictionary(new { controller = new RouteValueDictionary() });

            ExceptionAssert.Throws<InvalidOperationException>(
                () => RouteConstraintBuilder.BuildConstraints(dictionary, "{controller}/{action}"),
                "The constraint entry 'controller' on the route with route template " +
                "'{controller}/{action}' must have a string value or be of a type which implements '" +
                typeof(IRouteConstraint) + "'.");
        }

        [Theory]
        [InlineData("abc", "abc", true)]      // simple case
        [InlineData("abc", "bbb|abc", true)]  // Regex or
        [InlineData("Abc", "abc", true)]      // Case insensitive
        [InlineData("Abc ", "abc", false)]    // Matches whole (but no trimming)
        [InlineData("Abcd", "abc", false)]    // Matches whole (additional non whitespace char)
        [InlineData("Abc", " abc", false)]    // Matches whole (less one char)
        public void StringConstraintsMatchingScenarios(string routeValue,
                                                       string constraintValue,
                                                       bool shouldMatch)
        {
            // Arrange
            var dictionary = new RouteValueDictionary(new { controller = routeValue });

            var constraintDictionary = RouteConstraintBuilder.BuildConstraints(
                new RouteValueDictionary(new { controller = constraintValue }));
            var constraint = constraintDictionary["controller"];

            Assert.Equal(shouldMatch,
                constraint.Match(
                    httpContext: new Mock<HttpContext>().Object,
                    route: new Mock<IRouter>().Object,
                    routeKey: "controller",
                    values: dictionary,
                    routeDirection: RouteDirection.IncomingRequest));
        }

        public static IEnumerable<object> EmptyAndNullDictionary
        {
            get
            {
                return new[]
                {
                    new Object[]
                    {
                        null,
                    },

                    new Object[]
                    {
                        new Dictionary<string, object>(),
                    },
                };
            }
        }
    }
}
#endif