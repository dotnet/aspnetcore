// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
                Pets = new[] { "Aramis", "Porthos", "D'Artagnan" },
                Hobby = Hobbies.Swordfighting,
                Nicknames = new List<string> { "Comte de la Fère", "Armand" },
                BirthInstant = new DateTimeOffset(1825, 8, 6, 18, 45, 21, TimeSpan.FromHours(-6)),
                Age = new TimeSpan(7665, 1, 30, 0)
            };

            // Act/Assert
            Assert.Equal(
                "{\"Id\":1844,\"Name\":\"Athos\",\"Pets\":[\"Aramis\",\"Porthos\",\"D'Artagnan\"],\"Hobby\":2,\"Nicknames\":[\"Comte de la Fère\",\"Armand\"],\"BirthInstant\":\"1825-08-06T18:45:21.0000000-06:00\",\"Age\":\"7665.01:30:00\"}",
                JsonUtil.Serialize(person));
        }

        [Fact]
        public void CanDeserializeClassFromJson()
        {
            // Arrange
            var json = "{\"Id\":1844,\"Name\":\"Athos\",\"Pets\":[\"Aramis\",\"Porthos\",\"D'Artagnan\"],\"Hobby\":2,\"Nicknames\":[\"Comte de la Fère\",\"Armand\"],\"BirthInstant\":\"1825-08-06T18:45:21.0000000-06:00\",\"Age\":\"7665.01:30:00\"}";

            // Act
            var person = JsonUtil.Deserialize<Person>(json);

            // Assert
            Assert.Equal(1844, person.Id);
            Assert.Equal("Athos", person.Name);
            Assert.Equal(new[] { "Aramis", "Porthos", "D'Artagnan" }, person.Pets);
            Assert.Equal(Hobbies.Swordfighting, person.Hobby);
            Assert.Equal(new[] { "Comte de la Fère", "Armand" }, person.Nicknames);
            Assert.Equal(new DateTimeOffset(1825, 8, 6, 18, 45, 21, TimeSpan.FromHours(-6)), person.BirthInstant);
            Assert.Equal(new TimeSpan(7665, 1, 30, 0), person.Age);
        }

        [Fact]
        public void CanSerializeStructToJson()
        {
            // Arrange
            var commandResult = new SimpleStruct
            {
                StringProperty = "Test",
                BoolProperty = true,
                NullableIntProperty = 1
            };
            
            // Act
            var result = JsonUtil.Serialize(commandResult);
            
            // Assert
            Assert.Equal("{\"StringProperty\":\"Test\",\"BoolProperty\":true,\"NullableIntProperty\":1}", result);
        }

        [Fact]
        public void CanDeserializeStructFromJson()
        {
            // Arrange
            var json = "{\"StringProperty\":\"Test\",\"BoolProperty\":true,\"NullableIntProperty\":1}";

            //Act
            var simpleError = JsonUtil.Deserialize<SimpleStruct>(json);

            // Assert
            Assert.Equal("Test", simpleError.StringProperty);
            Assert.True(simpleError.BoolProperty);
            Assert.Equal(1, simpleError.NullableIntProperty);
        }

        struct SimpleStruct
        {
            public string StringProperty { get; set; }
            public bool BoolProperty { get; set; }
            public int? NullableIntProperty { get; set; }
        }

        class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string[] Pets { get; set; }
            public Hobbies Hobby { get; set; }
            public IList<string> Nicknames { get; set; }
            public DateTimeOffset BirthInstant { get; set; }
            public TimeSpan Age { get; set; }
        }

        enum Hobbies { Reading = 1, Swordfighting = 2 }
    }
}
