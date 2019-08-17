// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class EnvironmentVariableSkipConditionTest
    {
        private readonly string _skipReason = "Test skipped on environment variable with name '{0}' and value '{1}'" +
            $" for the '{nameof(EnvironmentVariableSkipConditionAttribute.RunOnMatch)}' value of '{{2}}'.";

        [Theory]
        [InlineData("false")]
        [InlineData("")]
        [InlineData(null)]
        public void IsMet_DoesNotMatch(string environmentVariableValue)
        {
            // Arrange
            var attribute = new EnvironmentVariableSkipConditionAttribute(
                new TestEnvironmentVariable("Run", environmentVariableValue),
                "Run",
                "true");

            // Act
            var isMet = attribute.IsMet;

            // Assert
            Assert.False(isMet);
        }

        [Theory]
        [InlineData("True")]
        [InlineData("TRUE")]
        [InlineData("true")]
        public void IsMet_DoesCaseInsensitiveMatch_OnValue(string environmentVariableValue)
        {
            // Arrange
            var attribute = new EnvironmentVariableSkipConditionAttribute(
                new TestEnvironmentVariable("Run", environmentVariableValue),
                "Run",
                "true");

            // Act
            var isMet = attribute.IsMet;

            // Assert
            Assert.True(isMet);
            Assert.Equal(
                string.Format(_skipReason, "Run", environmentVariableValue, attribute.RunOnMatch),
                attribute.SkipReason);
        }

        [Fact]
        public void IsMet_DoesSuccessfulMatch_OnNull()
        {
            // Arrange
            var attribute = new EnvironmentVariableSkipConditionAttribute(
                new TestEnvironmentVariable("Run", null),
                "Run",
                "true", null); // skip the test when the variable 'Run' is explicitly set to 'true' or is null (default)

            // Act
            var isMet = attribute.IsMet;

            // Assert
            Assert.True(isMet);
            Assert.Equal(
                string.Format(_skipReason, "Run", "(null)", attribute.RunOnMatch),
                attribute.SkipReason);
        }

        [Theory]
        [InlineData("false")]
        [InlineData("")]
        [InlineData(null)]
        public void IsMet_MatchesOnMultipleSkipValues(string environmentVariableValue)
        {
            // Arrange
            var attribute = new EnvironmentVariableSkipConditionAttribute(
                new TestEnvironmentVariable("Run", environmentVariableValue),
                "Run",
                "false", "", null);

            // Act
            var isMet = attribute.IsMet;

            // Assert
            Assert.True(isMet);
        }

        [Fact]
        public void IsMet_DoesNotMatch_OnMultipleSkipValues()
        {
            // Arrange
            var attribute = new EnvironmentVariableSkipConditionAttribute(
                new TestEnvironmentVariable("Build", "100"),
                "Build",
                "125", "126");

            // Act
            var isMet = attribute.IsMet;

            // Assert
            Assert.False(isMet);
        }

        [Theory]
        [InlineData("CentOS")]
        [InlineData(null)]
        [InlineData("")]
        public void IsMet_Matches_WhenRunOnMatchIsFalse(string environmentVariableValue)
        {
            // Arrange
            var attribute = new EnvironmentVariableSkipConditionAttribute(
                new TestEnvironmentVariable("LinuxFlavor", environmentVariableValue),
                "LinuxFlavor",
                "Ubuntu14.04")
            {
                // Example: Run this test on all OSes except on "Ubuntu14.04"
                RunOnMatch = false
            };

            // Act
            var isMet = attribute.IsMet;

            // Assert
            Assert.True(isMet);
        }

        [Fact]
        public void IsMet_DoesNotMatch_WhenRunOnMatchIsFalse()
        {
            // Arrange
            var attribute = new EnvironmentVariableSkipConditionAttribute(
                new TestEnvironmentVariable("LinuxFlavor", "Ubuntu14.04"),
                "LinuxFlavor",
                "Ubuntu14.04")
            {
                // Example: Run this test on all OSes except on "Ubuntu14.04"
                RunOnMatch = false
            };

            // Act
            var isMet = attribute.IsMet;

            // Assert
            Assert.False(isMet);
        }

        private struct TestEnvironmentVariable : IEnvironmentVariable
        {
            private readonly string _varName;

            public TestEnvironmentVariable(string varName, string value)
            {
                _varName = varName;
                Value = value;
            }

            public string Value { get; private set; }

            public string Get(string name)
            {
                if(string.Equals(name, _varName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return Value;
                }
                return string.Empty;
            }
        }
    }
}
