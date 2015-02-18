// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNet.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.JsonPatch.Test
{
    public class SimpleObjectAdapterTests
    {
        [Fact]
        public void AddResultsShouldReplace()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A"
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<string>(o => o.StringProperty, "B");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.StringProperty);
        }

        [Fact]
        public void AddResultsShouldReplaceWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A"
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<string>(o => o.StringProperty, "B");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.StringProperty);
        }

        [Fact]
        public void AddToList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void AddToListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void AddToListInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 4);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void AddToListInvalidPositionTooLargeWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void AddToListAtEnd()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 3);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, doc.IntegerList);
        }

        [Fact]
        public void AddToListAtEndWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 3);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, doc.IntegerList);
        }

        [Fact]
        public void AddToListAtBeginning()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void AddToListAtBeginningWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void AddToListInvalidPositionTooSmall()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, -1);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void AddToListInvalidPositionTooSmallWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void AddToListAppend()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, doc.IntegerList);
        }

        [Fact]
        public void AddToListAppendWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, doc.IntegerList);
        }

        [Fact]
        public void Remove()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A"
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<string>(o => o.StringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(null, doc.StringProperty);
        }

        [Fact]
        public void RemoveWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A"
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<string>(o => o.StringProperty);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(null, doc.StringProperty);
        }

        [Fact]
        public void RemoveFromList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList, 2);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.IntegerList);
        }

        [Fact]
        public void RemoveFromListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList, 2);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.IntegerList);
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList, 3);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooLargeWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList, 3);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooSmall()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList, -1);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooSmallWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void RemoveFromEndOfList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.IntegerList);
        }

        [Fact]
        public void RemoveFromEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.IntegerList);
        }

        [Fact]
        public void Replace()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                DecimalValue = 10
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<string>(o => o.StringProperty, "B");

            patchDoc.Replace(o => o.DecimalValue, 12);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.StringProperty);
            Assert.Equal(12, doc.DecimalValue);
        }

        [Fact]
        public void ReplaceWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                DecimalValue = 10
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<string>(o => o.StringProperty, "B");

            patchDoc.Replace(o => o.DecimalValue, 12);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.StringProperty);
            Assert.Equal(12, doc.DecimalValue);
        }

        [Fact]
        public void SerializationMustNotIncudeEnvelope()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                DecimalValue = 10,
                DoubleValue = 10,
                FloatValue = 10,
                IntegerValue = 10
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace(o => o.StringProperty, "B");
            patchDoc.Replace(o => o.DecimalValue, 12);
            patchDoc.Replace(o => o.DoubleValue, 12);
            patchDoc.Replace(o => o.FloatValue, 12);
            patchDoc.Replace(o => o.IntegerValue, 12);

            // Act
            var serialized = JsonConvert.SerializeObject(patchDoc);

            // Assert
            Assert.Equal(false, serialized.Contains("operations"));
            Assert.Equal(false, serialized.Contains("Operations"));
        }

        [Fact]
        public void DeserializationMustWorkWithoutEnvelope()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                DecimalValue = 10,
                DoubleValue = 10,
                FloatValue = 10,
                IntegerValue = 10
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace(o => o.StringProperty, "B");
            patchDoc.Replace(o => o.DecimalValue, 12);
            patchDoc.Replace(o => o.DoubleValue, 12);
            patchDoc.Replace(o => o.FloatValue, 12);
            patchDoc.Replace(o => o.IntegerValue, 12);

            // default: no envelope
            var serialized = JsonConvert.SerializeObject(patchDoc);

            // Act
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Assert
            Assert.IsType<JsonPatchDocument<SimpleDTO>>(deserialized);
        }

        [Fact]
        public void DeserializationMustFailWithEnvelope()
        {
            // Arrange
            string serialized = "{\"Operations\": [{ \"op\": \"replace\", \"path\": \"/title\", \"value\": \"New Title\"}]}";

            // Act & Assert
            Assert.Throws<JsonPatchException>(() =>
            {
                var deserialized
                    = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);
            });
        }

        [Fact]
        public void SerializationTests()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                DecimalValue = 10,
                DoubleValue = 10,
                FloatValue = 10,
                IntegerValue = 10
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace(o => o.StringProperty, "B");
            patchDoc.Replace(o => o.DecimalValue, 12);
            patchDoc.Replace(o => o.DoubleValue, 12);
            patchDoc.Replace(o => o.FloatValue, 12);
            patchDoc.Replace(o => o.IntegerValue, 12);

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserizalized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserizalized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.StringProperty);
            Assert.Equal(12, doc.DecimalValue);
            Assert.Equal(12, doc.DoubleValue);
            Assert.Equal(12, doc.FloatValue);
            Assert.Equal(12, doc.IntegerValue);
        }

        [Fact]
        public void SerializeAndReplaceGuidTest()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                GuidValue = Guid.NewGuid()
            };

            var newGuid = Guid.NewGuid();
            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace(o => o.GuidValue, newGuid);

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserizalized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserizalized.ApplyTo(doc);

            // Assert
            Assert.Equal(newGuid, doc.GuidValue);
        }

        [Fact]
        public void SerializeAndReplaceNestedObjectTest()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            var newDTO = new SimpleDTO()
            {
                DoubleValue = 1
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace(o => o.SimpleDTO, newDTO);

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(1, doc.SimpleDTO.DoubleValue);
            Assert.Equal(0, doc.SimpleDTO.IntegerValue);
            Assert.Equal(null, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceInList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<int>(o => o.IntegerList, 5, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<int>(o => o.IntegerList, 5, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceFullList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<List<int>>(o => o.IntegerList, new List<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<List<int>>(o => o.IntegerList, new List<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceFullListFromEnumerable()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<IEnumerable<int>>(o => o.IntegerList, new List<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceFullListFromEnumerableWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<IEnumerable<int>>(o => o.IntegerList, new List<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithCollection()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<IEnumerable<int>>(o => o.IntegerList, new Collection<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithCollectionWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<IEnumerable<int>>(o => o.IntegerList, new Collection<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceAtEndOfList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<int>(o => o.IntegerList, 5);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 5 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceAtEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<int>(o => o.IntegerList, 5);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 5 }, doc.IntegerList);
        }

        [Fact]
        public void ReplaceInListInvalidInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<int>(o => o.IntegerList, 5, 3);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() =>
            {
                patchDoc.ApplyTo(doc);
            });
        }

        [Fact]
        public void ReplaceInListInvalidInvalidPositionTooLargeWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<int>(o => o.IntegerList, 5, 3);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() =>
            {
                deserialized.ApplyTo(doc);
            });
        }

        [Fact]
        public void ReplaceInListInvalidPositionTooSmall()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<int>(o => o.IntegerList, 5, -1);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() =>
            {
                patchDoc.ApplyTo(doc);
            });
        }

        [Fact]
        public void ReplaceInListInvalidPositionTooSmallWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Replace<int>(o => o.IntegerList, 5, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTO>>(() =>
            {
                deserialized.ApplyTo(doc);
            });
        }

        [Fact]
        public void Copy()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<string>(o => o.StringProperty, o => o.AnotherStringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.AnotherStringProperty);
        }

        [Fact]
        public void CopyWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<string>(o => o.StringProperty, o => o.AnotherStringProperty);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.AnotherStringProperty);
        }

        [Fact]
        public void CopyInList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerList, 0, o => o.IntegerList, 1);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void CopyInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerList, 0, o => o.IntegerList, 1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void CopyFromListToEndOfList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerList, 0, o => o.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 1 }, doc.IntegerList);
        }

        [Fact]
        public void CopyFromListToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerList, 0, o => o.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 1 }, doc.IntegerList);
        }

        [Fact]
        public void CopyFromListToNonList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerList, 0, o => o.IntegerValue);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void CopyFromListToNonListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerList, 0, o => o.IntegerValue);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void CopyFromNonListToList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerValue, o => o.IntegerList, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void CopyFromNonListToListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerValue, o => o.IntegerList, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void CopyToEndOfList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerValue, o => o.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.IntegerList);
        }

        [Fact]
        public void CopyToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Copy<int>(o => o.IntegerValue, o => o.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.IntegerList);
        }

        [Fact]
        public void Move()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<string>(o => o.StringProperty, o => o.AnotherStringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.AnotherStringProperty);
            Assert.Equal(null, doc.StringProperty);
        }

        [Fact]
        public void MoveWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<string>(o => o.StringProperty, o => o.AnotherStringProperty);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.AnotherStringProperty);
            Assert.Equal(null, doc.StringProperty);
        }

        [Fact]
        public void MoveInList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerList, 0, o => o.IntegerList, 1);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 1, 3 }, doc.IntegerList);
        }

        [Fact]
        public void MoveInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerList, 0, o => o.IntegerList, 1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 1, 3 }, doc.IntegerList);
        }

        [Fact]
        public void MoveFromListToEndOfList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerList, 0, o => o.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3, 1 }, doc.IntegerList);
        }

        [Fact]
        public void MoveFromListToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerList, 0, o => o.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3, 1 }, doc.IntegerList);
        }

        [Fact]
        public void MoveFomListToNonList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerList, 0, o => o.IntegerValue);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.IntegerList);
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void MoveFomListToNonListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerList, 0, o => o.IntegerValue);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.IntegerList);
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void MoveFromNonListToList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerValue, o => o.IntegerList, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void MoveFromNonListToListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerValue, o => o.IntegerList, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.IntegerList);
        }

        [Fact]
        public void MoveToEndOfList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerValue, o => o.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.IntegerList);
        }

        [Fact]
        public void MoveToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Move<int>(o => o.IntegerValue, o => o.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.IntegerList);
        }
    }
}