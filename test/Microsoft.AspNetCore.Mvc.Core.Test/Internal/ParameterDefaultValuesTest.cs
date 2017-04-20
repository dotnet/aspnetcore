// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ParameterDefaultValuesTest
    {
        [Theory]
        [InlineData("DefaultAttributes", new object[] { "hello", true, 10 })]
        [InlineData("DefaultValues", new object[] { "hello", true, 20 })]
        [InlineData("DefaultValuesAndAttributes", new object[] { "hello", 20 })]
        [InlineData("NoDefaultAttributesAndValues", new object[] { null, 0, false, null })]
        public void GetParameterDefaultValues_ReturnsExpectedValues(string methodName, object[] expectedValues)
        {
            // Arrange
            var methodInfo = typeof(TestObject).GetMethod(methodName);

            // Act
            var actualValues = ParameterDefaultValues.GetParameterDefaultValues(methodInfo);

            // Assert
            Assert.Equal(expectedValues, actualValues);
        }

        private class TestObject
        {
            public void DefaultAttributes(
                [DefaultValue("hello")] string input1,
                [DefaultValue(true)] bool input2,
                [DefaultValue(10)] int input3)
            {
            }

            public void DefaultValues(
                string input1 = "hello",
                bool input2 = true,
                int input3 = 20)
            {
            }

            public void DefaultValuesAndAttributes(
                [DefaultValue("Hi")] string input1 = "hello",
                [DefaultValue(10)] int input3 = 20)
            {
            }

            public void NoDefaultAttributesAndValues(
                string input1,
                int input2,
                bool input3,
                TestObject input4)
            {
            }
        }
    }
}
