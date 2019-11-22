// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    // This is some basic coverage, it's not in depth because there are many many APIs here
    // and they mostly call through to CoreFx. We don't want to test the globalization details
    // of .NET in detail where we can avoid it.
    //
    // Instead there's a sampling of things that have somewhat unique behavior or semantics.
    public class BindConverterTest
    {
        [Fact]
        public void FormatValue_Bool()
        {
            // Arrange
            var value = true;
            var expected = true;

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_Bool_Generic()
        {
            // Arrange
            var value = true;
            var expected = true;

            // Act
            var actual = BindConverter.FormatValue<bool>(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_NullableBool()
        {
            // Arrange
            var value = (bool?)true;
            var expected = true;

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_NullableBool_Generic()
        {
            // Arrange
            var value = true;
            var expected = true;

            // Act
            var actual = BindConverter.FormatValue<bool?>(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_NullableBoolNull()
        {
            // Arrange
            var value = (bool?)null;
            var expected = (bool?)null;

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_NullableBoolNull_Generic()
        {
            // Arrange
            var value = (bool?)null;
            var expected = (bool?)null;

            // Act
            var actual = BindConverter.FormatValue<bool?>(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_Int()
        {
            // Arrange
            var value = 17;
            var expected = "17";

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_Int_Generic()
        {
            // Arrange
            var value = 17;
            var expected = "17";

            // Act
            var actual = BindConverter.FormatValue<int>(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_NullableInt()
        {
            // Arrange
            var value = (int?)17;
            var expected = "17";

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_NullableInt_Generic()
        {
            // Arrange
            var value = 17;
            var expected = "17";

            // Act
            var actual = BindConverter.FormatValue<int?>(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_DateTime()
        {
            // Arrange
            var value = DateTime.Now;
            var expected = value.ToString(CultureInfo.CurrentCulture);

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_DateTime_Format()
        {
            // Arrange
            var value = DateTime.Now;
            var expected = value.ToString("MM-yyyy", CultureInfo.InvariantCulture);

            // Act
            var actual = BindConverter.FormatValue(value, "MM-yyyy", CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_Enum()
        {
            // Arrange
            var value = SomeLetters.A;
            var expected = value.ToString();

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_Enum_OutOfRange()
        {
            // Arrange
            var value = SomeLetters.A + 3;
            var expected = value.ToString();

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FormatValue_NullableEnum()
        {
            // Arrange
            var value = (SomeLetters?)null;

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void FormatValue_TypeConverter()
        {
            // Arrange
            var value = new Person()
            {
                Name = "Glenn",
                Age = 47,
            };

            var expected = JsonSerializer.Serialize(value);

            // Act
            var actual = BindConverter.FormatValue(value);

            // Assert
            Assert.Equal(expected, actual);
        }

        private enum SomeLetters
        {
            A,
            B,
            C,
            Q,
        }

        [TypeConverter(typeof(PersonConverter))]
        private class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        private class PersonConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                {
                    return true;
                }

                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string text)
                {
                    return JsonSerializer.Deserialize<Person>(text);
                }

                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(string))
                {
                    return true;
                }

                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                {
                    return JsonSerializer.Serialize((Person)value);
                }

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
    }
}
