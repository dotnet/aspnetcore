// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ValueProviderResultTest
    {
        [Fact]
        public void ConvertTo_ReturnsNullForReferenceTypes_WhenValueIsNull()
        {
            var valueProviderResult = new ValueProviderResult(null, null, CultureInfo.InvariantCulture);

            var convertedValue = valueProviderResult.ConvertTo(typeof(string));

            Assert.Equal(null, convertedValue);
        }

        [Fact]
        public void ConvertTo_ReturnsDefaultForValueTypes_WhenValueIsNull()
        {
            var valueProviderResult = new ValueProviderResult(null, null, CultureInfo.InvariantCulture);

            var convertedValue = valueProviderResult.ConvertTo(typeof(int));

            Assert.Equal(0, convertedValue);
        }

        [Fact]
        public void ConvertToCanConvertArraysToSingleElements()
        {
            // Arrange
            var vpr = new ValueProviderResult(new int[] { 1, 20, 42 }, "", CultureInfo.InvariantCulture);

            // Act
            var converted = (string)vpr.ConvertTo(typeof(string));

            // Assert
            Assert.Equal("1", converted);
        }

        [Fact]
        public void ConvertToCanConvertSingleElementsToArrays()
        {
            // Arrange
            var vpr = new ValueProviderResult(42, "", CultureInfo.InvariantCulture);

            // Act
            var converted = (string[])vpr.ConvertTo(typeof(string[]));

            // Assert
            Assert.NotNull(converted);
            var result = Assert.Single(converted);
            Assert.Equal("42", result);
        }

        [Fact]
        public void ConvertToCanConvertSingleElementsToSingleElements()
        {
            // Arrange
            var vpr = new ValueProviderResult(42, "", CultureInfo.InvariantCulture);

            // Act
            var converted = (string)vpr.ConvertTo(typeof(string));

            // Assert
            Assert.NotNull(converted);
            Assert.Equal("42", converted);
        }

        [Fact]
        public void ConvertingNullStringToNullableIntReturnsNull()
        {
            // Arrange
            object original = null;
            var vpr = new ValueProviderResult(original, "", CultureInfo.InvariantCulture);

            // Act
            var returned = (int?)vpr.ConvertTo(typeof(int?));

            // Assert
            Assert.Equal(returned, null);
        }

        [Fact]
        public void ConvertingWhiteSpaceStringToNullableIntReturnsNull()
        {
            // Arrange
            var original = " ";
            var vpr = new ValueProviderResult(original, "", CultureInfo.InvariantCulture);

            // Act
            var returned = (int?)vpr.ConvertTo(typeof(int?));

            // Assert
            Assert.Equal(returned, null);
        }

        [Fact]
        public void ConvertToReturnsNullIfArrayElementValueIsNull()
        {
            // Arrange
            var vpr = new ValueProviderResult(new string[] { null }, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(int));

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsNullIfTryingToConvertEmptyArrayToSingleElement()
        {
            // Arrange
            var vpr = new ValueProviderResult(new int[0], "", CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(int));

            // Assert
            Assert.Null(outValue);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t \r\n ")]
        public void ConvertToReturnsNullIfTrimmedValueIsEmptyString(object value)
        {
            // Arrange
            var vpr = new ValueProviderResult(value, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(int));

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsNullIfTrimmedValueIsEmptyString()
        {
            // Arrange
            var vpr = new ValueProviderResult(rawValue: null,
                                              attemptedValue: null,
                                              culture: CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(int[]));

            // Assert
            Assert.Null(outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsIntegerAndDestinationTypeIsEnum()
        {
            // Arrange
            var vpr = new ValueProviderResult(new object[] { 1 }, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(MyEnum));

            // Assert
            Assert.Equal(outValue, MyEnum.Value1);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsStringValueAndDestinationTypeIsEnum()
        {
            // Arrange
            var vpr = new ValueProviderResult(new object[] { "1" }, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(MyEnum));

            // Assert
            Assert.Equal(outValue, MyEnum.Value1);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementIsStringKeyAndDestinationTypeIsEnum()
        {
            // Arrange
            var vpr = new ValueProviderResult(new object[] { "Value1" }, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(MyEnum));

            // Assert
            Assert.Equal(outValue, MyEnum.Value1);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsStringAndDestinationIsNullableInteger()
        {
            // Arrange
            var vpr = new ValueProviderResult("12", null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(int?));

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsStringAndDestinationIsNullableDouble()
        {
            // Arrange
            var vpr = new ValueProviderResult("12.5", null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(double?));

            // Assert
            Assert.Equal(12.5, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalAndDestinationIsNullableInteger()
        {
            // Arrange
            var vpr = new ValueProviderResult(12M, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(int?));

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalAndDestinationIsNullableDouble()
        {
            // Arrange
            var vpr = new ValueProviderResult(12.5M, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(double?));

            // Assert
            Assert.Equal(12.5, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalDoubleAndDestinationIsNullableInteger()
        {
            // Arrange
            var vpr = new ValueProviderResult(12.5M, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(int?));

            // Assert
            Assert.Equal(12, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfElementIsDecimalDoubleAndDestinationIsNullableLong()
        {
            // Arrange
            var vpr = new ValueProviderResult(12.5M, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(long?));

            // Assert
            Assert.Equal(12L, outValue);
        }

        [Fact]
        public void ConvertToReturnsValueIfArrayElementInstanceOfDestinationType()
        {
            // Arrange
            var vpr = new ValueProviderResult(new object[] { "some string" }, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(string));

            // Assert
            Assert.Equal("some string", outValue);
        }

        [Theory]
        [InlineData(new object[] { new[] { 1, 0 } })]
        [InlineData(new object[] { new[] { "Value1", "Value0" } })]
        [InlineData(new object[] { new[] { "Value1", "value0" } })]
        public void ConvertTo_ConvertsEnumArrays(object value)
        {
            // Arrange
            var vpr = new ValueProviderResult(value, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = (MyEnum[])vpr.ConvertTo(typeof(MyEnum[]));

            // Assert
            Assert.Equal(2, outValue.Length);
            Assert.Equal(MyEnum.Value1, outValue[0]);
            Assert.Equal(MyEnum.Value0, outValue[1]);
        }

        [Fact]
        public void ConvertToReturnsValueIfInstanceOfDestinationType()
        {
            // Arrange
            var original = new[] { "some string" };
            var vpr = new ValueProviderResult(original, null, CultureInfo.InvariantCulture);

            // Act
            var outValue = vpr.ConvertTo(typeof(string[]));

            // Assert
            Assert.Same(original, outValue);
        }

        [Theory]
        [InlineData(typeof(int), typeof(InvalidOperationException), typeof(Exception))]
        [InlineData(typeof(double?), typeof(InvalidOperationException), typeof(Exception))]
        [InlineData(typeof(MyEnum?), typeof(InvalidOperationException), typeof(FormatException))]
        public void ConvertToThrowsIfConverterThrows(Type destinationType, Type exceptionType, Type innerExceptionType)
        {
            // Arrange
            var vpr = new ValueProviderResult("this-is-not-a-valid-value", null, CultureInfo.InvariantCulture);

            // Act & Assert
            var ex = Assert.Throws(exceptionType, () => vpr.ConvertTo(destinationType));
            Assert.IsType(innerExceptionType, ex.InnerException);
        }

        [Fact]
        public void ConvertToThrowsIfNoConverterExists()
        {
            // Arrange
            var vpr = new ValueProviderResult("x", null, CultureInfo.InvariantCulture);
            var destinationType = typeof(MyClassWithoutConverter);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => vpr.ConvertTo(destinationType));
            Assert.Equal("The parameter conversion from type 'System.String' to type " +
                        "'Microsoft.AspNet.Mvc.ModelBinding.ValueProviderResultTest+MyClassWithoutConverter' " +
                        "failed because no type converter can convert between these types.",
                         ex.Message);
        }

        [Fact]
        public void ConvertToUsesProvidedCulture()
        {
            // Arrange
            var original = "12,5";
            var vpr = new ValueProviderResult(original, null, new CultureInfo("en-GB"));
            var frCulture = new CultureInfo("fr-FR");

            // Act
            var cultureResult = vpr.ConvertTo(typeof(decimal), frCulture);

            // Assert
            Assert.Equal(12.5M, cultureResult);
            Assert.Throws<InvalidOperationException>(() => vpr.ConvertTo(typeof(decimal)));
        }

        [Fact]
        public void CulturePropertyDefaultsToInvariantCulture()
        {
            // Arrange
            var result = new ValueProviderResult(null, null, null);

            // Act & assert
            Assert.Same(CultureInfo.InvariantCulture, result.Culture);
        }

        [Theory]
        [MemberData(nameof(IntrinsicConversionData))]
        public void ConvertToCanConvertIntrinsics<T>(object initialValue, T expectedValue)
        {
            // Arrange
            var result = new ValueProviderResult(initialValue, "", CultureInfo.InvariantCulture);

            // Act & Assert
            Assert.Equal(expectedValue, result.ConvertTo(typeof(T)));
        }

        public static IEnumerable<object[]> IntrinsicConversionData
        {
            get
            {
                yield return new object[] { 42, 42L };
                yield return new object[] { 42, (short)42 };
                yield return new object[] { 42, (float)42.0 };
                yield return new object[] { 42, (double)42.0 };
                yield return new object[] { 42M, 42 };
                yield return new object[] { 42L, 42 };
                yield return new object[] { 42, (byte)42 };
                yield return new object[] { (short)42, 42 };
                yield return new object[] { (float)42.0, 42 };
                yield return new object[] { (double)42.0, 42 };
                yield return new object[] { (byte)42, 42 };
                yield return new object[] { "2008-01-01", new DateTime(2008, 01, 01) };
                yield return new object[] { "00:00:20", TimeSpan.FromSeconds(20) };
                yield return new object[]
                {
                    "c6687d3a-51f9-4159-8771-a66d2b7d7038",
                    Guid.Parse("c6687d3a-51f9-4159-8771-a66d2b7d7038")
                };
            }
        }

        [Theory]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(MyEnum))]
        public void ConvertTo_Throws_IfValueIsNotStringData(Type destinationType)
        {
            // Arrange
            var result = new ValueProviderResult(new MyClassWithoutConverter(), "", CultureInfo.InvariantCulture);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => result.ConvertTo(destinationType));

            // Assert
            var expectedMessage = string.Format("The parameter conversion from type '{0}' to type '{1}' " +
                                                "failed because no type converter can convert between these types.",
                                                typeof(MyClassWithoutConverter), destinationType);
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void ConvertTo_Throws_IfDestinationTypeIsNotConvertible()
        {
            // Arrange
            var value = "Hello world";
            var destinationType = typeof(MyClassWithoutConverter);
            var result = new ValueProviderResult(value, "", CultureInfo.InvariantCulture);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => result.ConvertTo(destinationType));

            // Assert
            var expectedMessage = string.Format("The parameter conversion from type '{0}' to type '{1}' " +
                                                "failed because no type converter can convert between these types.",
                                                value.GetType(), typeof(MyClassWithoutConverter));
            Assert.Equal(expectedMessage, ex.Message);
        }

        private class MyClassWithoutConverter
        {
        }

        private enum MyEnum
        {
            Value0 = 0,
            Value1 = 1
        }
    }
}