// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure
{
    public abstract class TempDataSerializerTestBase
    {
        [Fact]
        public void DeserializeTempData_ReturnsEmptyDictionary_DataIsEmpty()
        {
            // Arrange
            var serializer = GetTempDataSerializer();

            // Act
            var tempDataDictionary = serializer.Deserialize(new byte[0]);

            // Assert
            Assert.NotNull(tempDataDictionary);
            Assert.Empty(tempDataDictionary);
        }

        [Fact]
        public void RoundTripTest_NullValue()
        {
            // Arrange
            var key = "NullKey";
            var testProvider = GetTempDataSerializer();
            var input = new Dictionary<string, object>
            {
                { key, null }
            };

            // Act
            var bytes = testProvider.Serialize(input);
            var values = testProvider.Deserialize(bytes);

            // Assert
            Assert.True(values.ContainsKey(key));
            Assert.Null(values[key]);
        }

        [Theory]
        [InlineData(-10)]
        [InlineData(3340)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void RoundTripTest_IntValue(int value)
        {
            // Arrange
            var key = "test-key";
            var testProvider = GetTempDataSerializer();
            var input = new Dictionary<string, object>
            {
                { key, value },
            };

            // Act
            var bytes = testProvider.Serialize(input);
            var values = testProvider.Deserialize(bytes);

            // Assert
            var roundTripValue = Assert.IsType<int>(values[key]);
            Assert.Equal(value, roundTripValue);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(10)]
        public void RoundTripTest_NullableInt(int? value)
        {
            // Arrange
            var key = "test-key";
            var testProvider = GetTempDataSerializer();
            var input = new Dictionary<string, object>
            {
                { key, value },
            };

            // Act
            var bytes = testProvider.Serialize(input);
            var values = testProvider.Deserialize(bytes);

            // Assert
            var roundTripValue = (int?)values[key];
            Assert.Equal(value, roundTripValue);
        }

        [Fact]
        public void RoundTripTest_StringValue()
        {
            // Arrange
            var key = "test-key";
            var value = "test-value";
            var testProvider = GetTempDataSerializer();
            var input = new Dictionary<string, object>
            {
                { key, value },
            };

            // Act
            var bytes = testProvider.Serialize(input);
            var values = testProvider.Deserialize(bytes);

            // Assert
            var roundTripValue = Assert.IsType<string>(values[key]);
            Assert.Equal(value, roundTripValue);
        }

        [Fact]
        public void RoundTripTest_Enum()
        {
            // Arrange
            var key = "test-key";
            var value = DayOfWeek.Friday;
            var testProvider = GetTempDataSerializer();
            var input = new Dictionary<string, object>
            {
                { key, value },
            };

            // Act
            var bytes = testProvider.Serialize(input);
            var values = testProvider.Deserialize(bytes);

            // Assert
            var roundTripValue = (DayOfWeek)values[key];
            Assert.Equal(value, roundTripValue);
        }

        protected abstract TempDataSerializer GetTempDataSerializer();
    }
}
