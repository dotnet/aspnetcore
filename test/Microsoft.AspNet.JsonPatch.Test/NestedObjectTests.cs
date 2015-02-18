// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.JsonPatch.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace Microsoft.AspNet.JsonPatch.Test
{
    public class NestedObjectTests
    {
        [Fact]
        public void ReplacePropertyInNestedObject()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                IntegerValue = 1
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<string>(o => o.NestedDTO.StringProperty, "B");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.NestedDTO.StringProperty);
        }

        [Fact]
        public void ReplacePropertyInNestedObjectWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                IntegerValue = 1
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<string>(o => o.NestedDTO.StringProperty, "B");

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.NestedDTO.StringProperty);
        }

        [Fact]
        public void ReplaceNestedObject()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                IntegerValue = 1
            };

            var newNested = new NestedDTO() { StringProperty = "B" };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<NestedDTO>(o => o.NestedDTO, newNested);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.NestedDTO.StringProperty);
        }

        [Fact]
        public void ReplaceNestedObjectWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                IntegerValue = 1
            };

            var newNested = new NestedDTO() { StringProperty = "B" };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<NestedDTO>(o => o.NestedDTO, newNested);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.NestedDTO.StringProperty);
        }

        [Fact]
        public void AddResultsInReplace()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<string>(o => o.SimpleDTO.StringProperty, "B");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleDTO.StringProperty);
        }

        [Fact]
        public void AddResultsInReplaceWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<string>(o => o.SimpleDTO.StringProperty, "B");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleDTO.StringProperty);
        }

        [Fact]
        public void AddToList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<int>(o => o.SimpleDTO.IntegerList, 4, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void AddToListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<int>(o => o.SimpleDTO.IntegerList, 4, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void AddToComplextTypeListSpecifyIndex()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTOList = new List<SimpleDTO>()
                {
                    new SimpleDTO
                    {
                        StringProperty = "String1"
                    },
                    new SimpleDTO
                    {
                        StringProperty = "String2"
                    }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<string>(o => o.SimpleDTOList[0].StringProperty, "ChangedString1");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("ChangedString1", doc.SimpleDTOList[0].StringProperty);
        }

        [Fact]
        public void AddToComplextTypeListSpecifyIndexWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTOList = new List<SimpleDTO>()
                {
                    new SimpleDTO
                    {
                        StringProperty = "String1"
                    },
                    new SimpleDTO
                    {
                        StringProperty = "String2"
                    }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<string>(o => o.SimpleDTOList[0].StringProperty, "ChangedString1");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("ChangedString1", doc.SimpleDTOList[0].StringProperty);
        }

        [Fact]
        public void AddToListInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<int>(o => o.SimpleDTO.IntegerList, 4, 4);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void AddToListInvalidPositionTooLargeWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<int>(o => o.SimpleDTO.IntegerList, 4, 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void AddToListInvalidPositionTooSmall()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<int>(o => o.SimpleDTO.IntegerList, 4, -1);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void AddToListInvalidPositionTooSmallWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<int>(o => o.SimpleDTO.IntegerList, 4, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void AddToListAppend()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<int>(o => o.SimpleDTO.IntegerList, 4);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void AddToListAppendWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Add<int>(o => o.SimpleDTO.IntegerList, 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void Remove()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<string>(o => o.SimpleDTO.StringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(null, doc.SimpleDTO.StringProperty);
        }

        [Fact]
        public void RemoveWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<string>(o => o.SimpleDTO.StringProperty);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(null, doc.SimpleDTO.StringProperty);
        }

        [Fact]
        public void RemoveFromList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<int>(o => o.SimpleDTO.IntegerList, 2);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void RemoveFromListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<int>(o => o.SimpleDTO.IntegerList, 2);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<int>(o => o.SimpleDTO.IntegerList, 3);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooLargeWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<int>(o => o.SimpleDTO.IntegerList, 3);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooSmall()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<int>(o => o.SimpleDTO.IntegerList, -1);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooSmallWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<int>(o => o.SimpleDTO.IntegerList, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void RemoveFromEndOfList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<int>(o => o.SimpleDTO.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void RemoveFromEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Remove<int>(o => o.SimpleDTO.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void Replace()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A",
                    DecimalValue = 10
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<string>(o => o.SimpleDTO.StringProperty, "B");
            patchDoc.Replace(o => o.SimpleDTO.DecimalValue, 12);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleDTO.StringProperty);
            Assert.Equal(12, doc.SimpleDTO.DecimalValue);
        }

        [Fact]
        public void ReplaceWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A",
                    DecimalValue = 10
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<string>(o => o.SimpleDTO.StringProperty, "B");
            patchDoc.Replace(o => o.SimpleDTO.DecimalValue, 12);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleDTO.StringProperty);
            Assert.Equal(12, doc.SimpleDTO.DecimalValue);
        }

        [Fact]
        public void SerializationTests()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A",
                    DecimalValue = 10,
                    DoubleValue = 10,
                    FloatValue = 10,
                    IntegerValue = 10
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace(o => o.SimpleDTO.StringProperty, "B");
            patchDoc.Replace(o => o.SimpleDTO.DecimalValue, 12);
            patchDoc.Replace(o => o.SimpleDTO.DoubleValue, 12);
            patchDoc.Replace(o => o.SimpleDTO.FloatValue, 12);
            patchDoc.Replace(o => o.SimpleDTO.IntegerValue, 12);

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserizalized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserizalized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleDTO.StringProperty);
            Assert.Equal(12, doc.SimpleDTO.DecimalValue);
            Assert.Equal(12, doc.SimpleDTO.DoubleValue);
            Assert.Equal(12, doc.SimpleDTO.FloatValue);
            Assert.Equal(12, doc.SimpleDTO.IntegerValue);
        }

        [Fact]
        public void ReplaceInList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<int>(o => o.SimpleDTO.IntegerList, 5, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<int>(o => o.SimpleDTO.IntegerList, 5, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceFullList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<List<int>>(o => o.SimpleDTO.IntegerList, new List<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Arrange
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithSerialiation()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<List<int>>(o => o.SimpleDTO.IntegerList, new List<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceFullListFromEnumerable()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<IEnumerable<int>>(o => o.SimpleDTO.IntegerList, new List<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceFullListFromEnumerableWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<IEnumerable<int>>(o => o.SimpleDTO.IntegerList, new List<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithCollection()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<IEnumerable<int>>(o => o.SimpleDTO.IntegerList, new Collection<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithCollectionWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<IEnumerable<int>>(o => o.SimpleDTO.IntegerList, new Collection<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceAtEndOfList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<int>(o => o.SimpleDTO.IntegerList, 5);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 5 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceAtEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<int>(o => o.SimpleDTO.IntegerList, 5);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 5 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void ReplaceInListInvalidInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<int>(o => o.SimpleDTO.IntegerList, 5, 3);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void ReplaceInListInvalidInvalidPositionTooLargeWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<int>(o => o.SimpleDTO.IntegerList, 5, 3);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void ReplaceInListInvalidPositionTooSmall()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<int>(o => o.SimpleDTO.IntegerList, 5, -1);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { patchDoc.ApplyTo(doc); });
        }

        [Fact]
        public void ReplaceInListInvalidPositionTooSmallWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Replace<int>(o => o.SimpleDTO.IntegerList, 5, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act & Assert
            Assert.Throws<JsonPatchException<SimpleDTOWithNestedDTO>>(() => { deserialized.ApplyTo(doc); });
        }

        [Fact]
        public void Copy()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<string>(o => o.SimpleDTO.StringProperty, o => o.SimpleDTO.AnotherStringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.SimpleDTO.AnotherStringProperty);
        }

        [Fact]
        public void CopyWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<string>(o => o.SimpleDTO.StringProperty, o => o.SimpleDTO.AnotherStringProperty);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.SimpleDTO.AnotherStringProperty);
        }

        [Fact]
        public void CopyInList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerList, 1);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 1, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void CopyInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerList, 1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 1, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void CopyFromListToEndOfList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 1 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void CopyFromListToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 1 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void CopyFromListToNonList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerValue);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(1, doc.SimpleDTO.IntegerValue);
        }

        [Fact]
        public void CopyFromListToNonListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerValue);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(1, doc.SimpleDTO.IntegerValue);
        }

        [Fact]
        public void CopyFromNonListToList()
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

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerValue, o => o.SimpleDTO.IntegerList, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void CopyFromNonListToListWithSerialization()
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

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerValue, o => o.SimpleDTO.IntegerList, 0);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void CopyToEndOfList()
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

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerValue, o => o.SimpleDTO.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void CopyToEndOfListWithSerialization()
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

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Copy<int>(o => o.SimpleDTO.IntegerValue, o => o.SimpleDTO.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void Move()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<string>(o => o.SimpleDTO.StringProperty, o => o.SimpleDTO.AnotherStringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.SimpleDTO.AnotherStringProperty);
            Assert.Equal(null, doc.SimpleDTO.StringProperty);
        }

        [Fact]
        public void MoveWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<string>(o => o.SimpleDTO.StringProperty, o => o.SimpleDTO.AnotherStringProperty);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.SimpleDTO.AnotherStringProperty);
            Assert.Equal(null, doc.SimpleDTO.StringProperty);
        }

        [Fact]
        public void MoveInList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerList, 1);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 1, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void MoveInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerList, 1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 1, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void MoveFromListToEndOfList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3, 1 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void MoveFromListToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3, 1 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void MoveFomListToNonList()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerValue);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.SimpleDTO.IntegerList);
            Assert.Equal(1, doc.SimpleDTO.IntegerValue);
        }

        [Fact]
        public void MoveFomListToNonListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerList, 0, o => o.SimpleDTO.IntegerValue);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.SimpleDTO.IntegerList);
            Assert.Equal(1, doc.SimpleDTO.IntegerValue);
        }

        [Fact]
        public void MoveFomListToNonListBetweenHierarchy()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerList, 0, o => o.IntegerValue);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.SimpleDTO.IntegerList);
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void MoveFomListToNonListBetweenHierarchyWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTOWithNestedDTO()
            {
                SimpleDTO = new SimpleDTO()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerList, 0, o => o.IntegerValue);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.SimpleDTO.IntegerList);
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void MoveFromNonListToList()
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

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerValue, o => o.SimpleDTO.IntegerList, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void MoveFromNonListToListWithSerialization()
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

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerValue, o => o.SimpleDTO.IntegerList, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void MoveToEndOfList()
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

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerValue, o => o.SimpleDTO.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleDTO.IntegerList);
        }

        [Fact]
        public void MoveToEndOfListWithSerialization()
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

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTOWithNestedDTO>();
            patchDoc.Move<int>(o => o.SimpleDTO.IntegerValue, o => o.SimpleDTO.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTOWithNestedDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleDTO.IntegerList);
        }
    }
}