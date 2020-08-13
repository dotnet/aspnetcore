// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class AttributeDictionaryTest
    {
        [Fact]
        public void AttributeDictionary_AddItems()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            // Act
            attributes.Add("zero", "0");
            attributes.Add("one", "1");
            attributes.Add("two", "2");

            // Assert
            Assert.Equal(3, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("one", "1"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_AddItems_AsKeyValuePairs()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            // Act
            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Assert
            Assert.Equal(3, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("one", "1"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_Add_DuplicateKey()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add("zero", "0");
            attributes.Add("one", "1");
            attributes.Add("two", "2");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => attributes.Add("one", "15"));
        }

        [Fact]
        public void AttributeDictionary_Clear()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            attributes.Clear();

            // Assert
            Assert.Empty(attributes);
            Assert.Empty(attributes);
        }

        [Fact]
        public void AttributeDictionary_Contains_Success()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var result = attributes.Contains(new KeyValuePair<string, string>("zero", "0"));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AttributeDictionary_Contains_Failure()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var result = attributes.Contains(new KeyValuePair<string, string>("zero", "nada"));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AttributeDictionary_ContainsKey_Success()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var result = attributes.ContainsKey("one");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AttributeDictionary_ContainsKey_Failure()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var result = attributes.ContainsKey("one!");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AttributeDictionary_CopyTo()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            var array = new KeyValuePair<string, string>[attributes.Count + 1];

            // Act
            attributes.CopyTo(array, 1);

            // Assert
            Assert.Collection(
                array,
                kvp => Assert.Equal(default(KeyValuePair<string, string>), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("one", "1"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_IsReadOnly()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            // Act
            var result = attributes.IsReadOnly;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AttributeDictionary_Keys()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var keys = attributes.Keys;

            // Assert
            Assert.Equal(3, keys.Count);
            Assert.Collection(
                keys,
                key => Assert.Equal("one", key),
                key => Assert.Equal("two", key),
                key => Assert.Equal("zero", key));
        }

        [Fact]
        public void AttributeDictionary_Remove_Success()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var result = attributes.Remove("one");

            // Assert
            Assert.True(result);
            Assert.Equal(2, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_Remove_Failure()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var result = attributes.Remove("nada");

            // Assert
            Assert.False(result);
            Assert.Equal(3, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("one", "1"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_Remove_KeyValuePair_Success()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var result = attributes.Remove(new KeyValuePair<string, string>("one", "1"));

            // Assert
            Assert.True(result);
            Assert.Equal(2, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_Remove_KeyValuePair_Failure()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var result = attributes.Remove(new KeyValuePair<string, string>("one", "0"));

            // Assert
            Assert.False(result);
            Assert.Equal(3, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("one", "1"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_TryGetValue_Success()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            string value;

            // Act
            var result = attributes.TryGetValue("two", out value);

            // Assert
            Assert.True(result);
            Assert.Equal("2", value);
        }

        [Fact]
        public void AttributeDictionary_TryGetValue_Failure()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            string value;

            // Act
            var result = attributes.TryGetValue("nada", out value);


            // Assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void AttributeDictionary_Values()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var values = attributes.Values;

            // Assert
            Assert.Equal(3, values.Count);
            Assert.Collection(
                values,
                key => Assert.Equal("1", key),
                key => Assert.Equal("2", key),
                key => Assert.Equal("0", key));
        }

        [Fact]
        public void AttributeDictionary_Indexer_Success()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            var value = attributes["two"];

            // Assert
            Assert.Equal("2", value);
        }

        [Fact]
        public void AttributeDictionary_Indexer_Fails()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => attributes["nada"]);
        }

        [Fact]
        public void AttributeDictionary_Indexer_SetValue()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            attributes["one"] = "1!";

            // Assert
            Assert.Equal(3, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("one", "1!"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_Indexer_Insert()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            attributes["exciting!"] = "1!";

            // Assert
            Assert.Equal(4, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("exciting!", "1!"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("one", "1"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }

        [Fact]
        public void AttributeDictionary_CaseInsensitive()
        {
            // Arrange
            var attributes = new AttributeDictionary();

            attributes.Add(new KeyValuePair<string, string>("zero", "0"));
            attributes.Add(new KeyValuePair<string, string>("one", "1"));
            attributes.Add(new KeyValuePair<string, string>("two", "2"));

            // Act
            attributes["oNe"] = "1!";

            // Assert
            Assert.Equal(3, attributes.Count);
            Assert.Collection(
                attributes,
                kvp => Assert.Equal(new KeyValuePair<string, string>("oNe", "1!"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("two", "2"), kvp),
                kvp => Assert.Equal(new KeyValuePair<string, string>("zero", "0"), kvp));
        }
    }
}
