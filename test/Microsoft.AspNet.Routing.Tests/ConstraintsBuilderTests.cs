using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class ConstraintsBuilderTests
    {
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
        [InlineData("abc", "abc", true)]
        [InlineData("Abc", "abc", true)]
        [InlineData("Abc ", "abc", false)]
        [InlineData("Abcd", "abc", false)]
        [InlineData("Abc", " abc", false)]
        public void StringConstraintsMatchesWholeValueCaseInsensitively(string routeValue,
                                                                        string constraintValue,
                                                                        bool shouldMatch)
        {
            // Arrange
            var dictionary = new RouteValueDictionary(new { controller = routeValue });

            var constraintDictionary = RouteConstraintBuilder.BuildConstraints(
                new RouteValueDictionary(new { controller = constraintValue }));
            var constraint = constraintDictionary["controller"];

            Assert.Equal(shouldMatch,
                constraint.EasyMatch("controller", dictionary));
        }
    }
}
