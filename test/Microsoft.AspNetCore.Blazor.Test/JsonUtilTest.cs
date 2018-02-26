// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test
{
    public class JsonUtilTest
    {
        // It's not useful to have a complete set of behavior specifications for
        // what the JSON serializer/deserializer does in all cases here. We merely
        // expose a simple wrapper over a third-party library that maintains its
        // own specs and tests.
        //
        // We should only add tests here to cover behaviors that Blazor itself
        // depends on.

        [Theory]
        [InlineData(null, "null")]
        [InlineData("My string", "\"My string\"")]
        [InlineData(123, "123")]
        [InlineData(123.456f, "123.456")]
        [InlineData(123.456d, "123.456")]
        [InlineData(true, "true")]
        public void CanSerializePrimitivesToJson(object value, string expectedJson)
        {
            Assert.Equal(expectedJson, JsonUtil.Serialize(value));
        }

        [Theory]
        [InlineData("null", null)]
        [InlineData("\"My string\"", "My string")]
        [InlineData("123", 123L)] // Would also accept 123 as a System.Int32, but Int64 is fine as a default
        [InlineData("123.456", 123.456d)]
        [InlineData("true", true)]
        public void CanDeserializePrimitivesFromJson(string json, object expectedValue)
        {
            Assert.Equal(expectedValue, JsonUtil.Deserialize<object>(json));
        }

        [Fact]
        public void CanSerializeClassToJson()
        {
            // Arrange
            var person = new Person
            {
                Id = 1844,
                Name = "Athos",
                Pets = new[] { "Aramis", "Porthos", "D'Artagnan" }
            };

            // Act/Assert
            Assert.Equal(
                "{\"Id\":1844,\"Name\":\"Athos\",\"Pets\":[\"Aramis\",\"Porthos\",\"D'Artagnan\"]}",
                JsonUtil.Serialize(person));
        }

        [Fact]
        public void CanDeserializeClassFromJson()
        {
            // Arrange
            var json = "{\"Id\":1844,\"Name\":\"Athos\",\"Pets\":[\"Aramis\",\"Porthos\",\"D'Artagnan\"]}";

            // Act
            var person = JsonUtil.Deserialize<Person>(json);

            // Assert
            Assert.Equal(1844, person.Id);
            Assert.Equal("Athos", person.Name);
            Assert.Equal(new[] { "Aramis", "Porthos", "D'Artagnan" }, person.Pets);
        }

        class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string[] Pets { get; set; }
        }
    }
}
