// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    public class TempDataSerializerTest
    {
        public static TheoryData<object, Type> InvalidTypes
        {
            get
            {
                return new TheoryData<object, Type>
                {
                    { new object(), typeof(object) },
                    { new object[3], typeof(object) },
                    { new TestItem(), typeof(TestItem) },
                    { new List<TestItem>(), typeof(TestItem) },
                    { new Dictionary<string, TestItem>(), typeof(TestItem) },
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidTypes))]
        public void EnsureObjectCanBeSerialized_ThrowsException_OnInvalidType(object value, Type type)
        {
            // Arrange
            var testProvider = new TempDataSerializer();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                testProvider.EnsureObjectCanBeSerialized(value);
            });
            Assert.Equal($"The '{typeof(TempDataSerializer).FullName}' cannot serialize " +
                $"an object of type '{type}'.",
                exception.Message);
        }

        public static TheoryData<object, Type> InvalidDictionaryTypes
        {
            get
            {
                return new TheoryData<object, Type>
                {
                    { new Dictionary<int, string>(), typeof(int) },
                    { new Dictionary<Uri, Guid>(), typeof(Uri) },
                    { new Dictionary<object, string>(), typeof(object) },
                    { new Dictionary<TestItem, TestItem>(), typeof(TestItem) }
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidDictionaryTypes))]
        public void EnsureObjectCanBeSerialized_ThrowsException_OnInvalidDictionaryType(object value, Type type)
        {
            // Arrange
            var testProvider = new TempDataSerializer();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                testProvider.EnsureObjectCanBeSerialized(value);
            });
            Assert.Equal($"The '{typeof(TempDataSerializer).FullName}' cannot serialize a dictionary " +
                $"with a key of type '{type}'. The key must be of type 'System.String'.",
                exception.Message);
        }

        public static TheoryData<object> ValidTypes
        {
            get
            {
                return new TheoryData<object>
                {
                    { 10 },
                    { new int[]{ 10, 20 } },
                    { "FooValue" },
                    { new Uri("http://Foo") },
                    { Guid.NewGuid() },
                    { new List<string> { "foo", "bar" } },
                    { new DateTimeOffset() },
                    { 100.1m },
                    { new Dictionary<string, int>() },
                    { new Uri[] { new Uri("http://Foo"), new Uri("http://Bar") } },
                    { DayOfWeek.Sunday },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValidTypes))]
        public void EnsureObjectCanBeSerialized_DoesNotThrow_OnValidType(object value)
        {
            // Arrange
            var testProvider = new TempDataSerializer();

            // Act & Assert (Does not throw)
            testProvider.EnsureObjectCanBeSerialized(value);
        }

        [Fact]
        public void DeserializeTempData_ReturnsEmptyDictionary_DataIsEmpty()
        {
            // Arrange
            var serializer = new TempDataSerializer();

            // Act
            var tempDataDictionary = serializer.Deserialize(new byte[0]);

            // Assert
            Assert.NotNull(tempDataDictionary);
            Assert.Empty(tempDataDictionary);
        }

        [Fact]
        public void SerializeAndDeserialize_NullValue_RoundTripsSuccessfully()
        {
            // Arrange
            var key = "NullKey";
            var testProvider = new TempDataSerializer();
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

        private class TestItem
        {
            public int DummyInt { get; set; }
        }
    }
}
