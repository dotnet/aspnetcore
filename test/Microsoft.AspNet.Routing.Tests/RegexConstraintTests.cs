using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class RegexConstraintTests
    {
        [Theory]
        [InlineData("abc", "abc", true)]
        [InlineData("Abc", "abc", true)]
        [InlineData("Abc ", "abc", true)]
        [InlineData("Abcd", "abc", true)]
        [InlineData("^Abcd", "abc", true)]
        [InlineData("Abc", " abc", false)]
        public void RegexConstraintDoesNotPrepend(string routeValue,
                                                  string constraintValue,
                                                  bool shouldMatch)
        {
            // Arrange
            var constraint = new RegexConstraint(constraintValue);
            var values = new RouteValueDictionary(new {controller = routeValue});

            // Assert
            Assert.Equal(shouldMatch, constraint.EasyMatch("controller", values));
        }

        [Fact]
        public void RegexConstraintCanTakeARegex_SuccessulMatch()
        {
            // Arrange
            var constraint = new RegexConstraint(new Regex("^abc$"));
            var values = new RouteValueDictionary(new { controller = "abc"});

            // Assert
            Assert.True(constraint.EasyMatch("controller", values));
        }

        [Fact]
        public void RegexConstraintFailsIfKeyIsNotFound()
        {
            // Arrange
            var constraint = new RegexConstraint(new Regex("^abc$"));
            var values = new RouteValueDictionary(new { action = "abc" });

            // Assert
            Assert.False(constraint.EasyMatch("controller", values));
        }

        [Fact]
        public void RegexConstraintCanTakeARegex_FailedMatch()
        {
            // Arrange
            var constraint = new RegexConstraint(new Regex("^abc$"));
            var values = new RouteValueDictionary(new { controller = "Abc" });

            // Assert
            Assert.False(constraint.EasyMatch("controller", values));
        }
    }
}
