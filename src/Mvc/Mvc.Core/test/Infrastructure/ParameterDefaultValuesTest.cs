// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

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

    [Fact]
    public void GetParameterDefaultValues_ReturnsExpectedValues_ForStructTypes()
    {
        // Arrange
        var methodInfo = typeof(TestObject).GetMethod("DefaultValuesOfStructTypes");

        // Act
        var actualValues = ParameterDefaultValues.GetParameterDefaultValues(methodInfo);

        // Assert
        Assert.Equal(
            new object[] { default(Guid), default(TimeSpan), default(DateTime), default(DateTimeOffset) },
            actualValues);
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

        // Note that default value for DateTime currently throws a FormatException
        // https://github.com/dotnet/corefx/issues/12338
        public void DefaultValuesOfStructTypes(
            Guid guid = default(Guid),
            TimeSpan timeSpan = default(TimeSpan),
            DateTime dateTime = default(DateTime),
            DateTimeOffset dateTimeOffset = default(DateTimeOffset))
        {
        }
    }
}
