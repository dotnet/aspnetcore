// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class RegexDispatcherValueConstraintTest
    {
        [Theory]
        [InlineData("abc", "abc", true)]    // simple match
        [InlineData("Abc", "abc", true)]    // case insensitive match
        [InlineData("Abc ", "abc", true)]   // Extra space on input match (because we don't add ^({0})$
        [InlineData("Abcd", "abc", true)]   // Extra char
        [InlineData("^Abcd", "abc", true)]  // Extra special char
        [InlineData("Abc", " abc", false)]  // Missing char
        [InlineData("123-456-2334", @"^\d{3}-\d{3}-\d{4}$", true)] // ssn
        [InlineData(@"12/4/2013", @"^\d{1,2}\/\d{1,2}\/\d{4}$", true)] // date
        [InlineData(@"abc@def.com", @"^\w+[\w\.]*\@\w+((-\w+)|(\w*))\.[a-z]{2,3}$", true)] // email
        public void RegexConstraintBuildRegexVerbatimFromInput(
            string routeValue,
            string constraintValue,
            bool shouldMatch)
        {
            // Arrange
            var constraint = new RegexDispatcherValueConstraint(constraintValue);
            var values = new DispatcherValueCollection(new { controller = routeValue });

            // Act
            var match = TestConstraint(constraint, values, "controller");

            // Assert
            Assert.Equal(shouldMatch, match);
        }

        [Fact]
        public void RegexConstraint_TakesRegexAsInput_SimpleMatch()
        {
            // Arrange
            var constraint = new RegexDispatcherValueConstraint(new Regex("^abc$"));
            var values = new DispatcherValueCollection(new { controller = "abc" });

            // Act
            var match = TestConstraint(constraint, values, "controller");

            // Assert
            Assert.True(match);
        }

        [Fact]
        public void RegexConstraintConstructedWithRegex_SimpleFailedMatch()
        {
            // Arrange
            var constraint = new RegexDispatcherValueConstraint(new Regex("^abc$"));
            var values = new DispatcherValueCollection(new { controller = "Abc" });

            // Act
            var match = TestConstraint(constraint, values, "controller");

            // Assert
            Assert.False(match);
        }

        [Fact]
        public void RegexConstraintFailsIfKeyIsNotFoundInRouteValues()
        {
            // Arrange
            var constraint = new RegexDispatcherValueConstraint(new Regex("^abc$"));
            var values = new DispatcherValueCollection(new { action = "abc" });

            // Act
            var match = TestConstraint(constraint, values, "controller");

            // Assert
            Assert.False(match);
        }

        [Theory]
        [InlineData("tr-TR")]
        [InlineData("en-US")]
        public void RegexConstraintIsCultureInsensitiveWhenConstructedWithString(string culture)
        {
            if (TestPlatformHelper.IsMono)
            {
                // The Regex in Mono returns true when matching the Turkish I for the a-z range which causes the test
                // to fail. Tracked via #100.
                return;
            }

            // Arrange
            var constraint = new RegexDispatcherValueConstraint("^([a-z]+)$");
            var values = new DispatcherValueCollection(new { controller = "\u0130" }); // Turkish upper-case dotted I

            using (new CultureReplacer(culture))
            {
                // Act
                var match = TestConstraint(constraint, values, "controller");

                // Assert
                Assert.False(match);
            }
        }

        private static bool TestConstraint(IDispatcherValueConstraint constraint, DispatcherValueCollection values, string routeKey)
        {
            var httpContext = new DefaultHttpContext();
            var constraintPurpose = ConstraintPurpose.IncomingRequest;

            var dispatcherValueConstraintContext = new DispatcherValueConstraintContext(httpContext, values, constraintPurpose)
            {
                Key = routeKey
            };

            return constraint.Match(dispatcherValueConstraintContext);
        }
    }
}
