// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Adapters
{
    public class ObjectAdapterTests
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
        public void AddToIntegerIList()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerIList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerIList, 4, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.IntegerIList);
        }

        [Fact]
        public void AddToIntegerIListWithSerialization()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerIList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerIList, 4, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.IntegerIList);
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
            var exception = Assert.Throws<JsonPatchException>(() => { patchDoc.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "4"),
                exception.Message);
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
            var exception = Assert.Throws<JsonPatchException>(() => { deserialized.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "4"),
                exception.Message);
        }

        [Fact]
        public void AddToListInvalidPositionTooLarge_LogsError()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, 4);

            var logger = new TestErrorLogger<SimpleDTO>();

            patchDoc.ApplyTo(doc, logger.LogErrorMessage);


            // Assert
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "4"),
                logger.ErrorMessage);
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
            var exception = Assert.Throws<JsonPatchException>(() => { patchDoc.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
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
            var exception = Assert.Throws<JsonPatchException>(() => { deserialized.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
        }

        [Fact]
        public void AddToListInvalidPositionTooSmall_LogsError()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerList, 4, -1);

            var logger = new TestErrorLogger<SimpleDTO>();

            patchDoc.ApplyTo(doc, logger.LogErrorMessage);

            // Assert
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                logger.ErrorMessage);
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
            var exception = Assert.Throws<JsonPatchException>(() => { patchDoc.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
                exception.Message);
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
            var exception = Assert.Throws<JsonPatchException>(() => { deserialized.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
                exception.Message);
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooLarge_LogsError()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList, 3);

            var logger = new TestErrorLogger<SimpleDTO>();

            patchDoc.ApplyTo(doc, logger.LogErrorMessage);


            // Assert
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
                logger.ErrorMessage);
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
            var exception = Assert.Throws<JsonPatchException>(() => { patchDoc.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
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
            var exception = Assert.Throws<JsonPatchException>(() => { deserialized.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooSmall_LogsError()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Remove<int>(o => o.IntegerList, -1);

            var logger = new TestErrorLogger<SimpleDTO>();


            patchDoc.ApplyTo(doc, logger.LogErrorMessage);


            // Assert
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                logger.ErrorMessage);
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
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                var deserialized
                    = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleDTO>>(serialized);
            });

            Assert.Equal("The type 'JsonPatchDocument`1' was malformed and could not be parsed.", exception.Message);
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
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDoc.ApplyTo(doc);
            });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
                exception.Message);
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
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
                exception.Message);
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
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDoc.ApplyTo(doc);
            });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
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
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                deserialized.ApplyTo(doc);
            });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
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

        private class Class6
        {
            public IDictionary<string, int> DictionaryOfStringToInteger { get; } = new Dictionary<string, int>();
        }

        [Fact]
        public void Add_WhenDictionary_ValueIsNonObject_Succeeds()
        {
            // Arrange
            var model = new Class6();
            model.DictionaryOfStringToInteger["one"] = 1;
            model.DictionaryOfStringToInteger["two"] = 2;
            var patchDocument = new JsonPatchDocument();
            patchDocument.Add("/DictionaryOfStringToInteger/three", 3);

            // Act
            patchDocument.ApplyTo(model);

            // Assert
            Assert.Equal(3, model.DictionaryOfStringToInteger.Count);
            Assert.Equal(1, model.DictionaryOfStringToInteger["one"]);
            Assert.Equal(2, model.DictionaryOfStringToInteger["two"]);
            Assert.Equal(3, model.DictionaryOfStringToInteger["three"]);
        }

        [Fact]
        public void Remove_WhenDictionary_ValueIsNonObject_Succeeds()
        {
            // Arrange
            var model = new Class6();
            model.DictionaryOfStringToInteger["one"] = 1;
            model.DictionaryOfStringToInteger["two"] = 2;
            var patchDocument = new JsonPatchDocument();
            patchDocument.Remove("/DictionaryOfStringToInteger/two");

            // Act
            patchDocument.ApplyTo(model);

            // Assert
            Assert.Equal(1, model.DictionaryOfStringToInteger.Count);
            Assert.Equal(1, model.DictionaryOfStringToInteger["one"]);
        }

        [Fact]
        public void Replace_WhenDictionary_ValueIsNonObject_Succeeds()
        {
            // Arrange
            var model = new Class6();
            model.DictionaryOfStringToInteger["one"] = 1;
            model.DictionaryOfStringToInteger["two"] = 2;
            var patchDocument = new JsonPatchDocument();
            patchDocument.Replace("/DictionaryOfStringToInteger/two", 20);

            // Act
            patchDocument.ApplyTo(model);

            // Assert
            Assert.Equal(2, model.DictionaryOfStringToInteger.Count);
            Assert.Equal(1, model.DictionaryOfStringToInteger["one"]);
            Assert.Equal(20, model.DictionaryOfStringToInteger["two"]);
        }

        private class Customer
        {
            public string Name { get; set; }
            public Address Address { get; set; }
        }

        private class Address
        {
            public string City { get; set; }
        }

        private class Class8
        {
            public IDictionary<string, Customer> DictionaryOfStringToCustomer { get; } = new Dictionary<string, Customer>();
        }

        [Fact]
        public void Replace_WhenDictionary_ValueAPocoType_Succeeds()
        {
            // Arrange
            var key1 = "key1";
            var value1 = new Customer() { Name = "Jamesss" };
            var key2 = "key2";
            var value2 = new Customer() { Name = "Mike" };
            var model = new Class8();
            model.DictionaryOfStringToCustomer[key1] = value1;
            model.DictionaryOfStringToCustomer[key2] = value2;
            var patchDocument = new JsonPatchDocument();
            patchDocument.Replace($"/DictionaryOfStringToCustomer/{key1}/Name", "James");

            // Act
            patchDocument.ApplyTo(model);

            // Assert
            Assert.Equal(2, model.DictionaryOfStringToCustomer.Count);
            var actualValue1 = model.DictionaryOfStringToCustomer[key1];
            Assert.NotNull(actualValue1);
            Assert.Equal("James", actualValue1.Name);
        }

        [Fact]
        public void Replace_WhenDictionary_ValueAPocoType_Succeeds_WithSerialization()
        {
            // Arrange
            var key1 = "key1";
            var value1 = new Customer() { Name = "Jamesss" };
            var key2 = "key2";
            var value2 = new Customer() { Name = "Mike" };
            var model = new Class8();
            model.DictionaryOfStringToCustomer[key1] = value1;
            model.DictionaryOfStringToCustomer[key2] = value2;
            var patchDocument = new JsonPatchDocument();
            patchDocument.Replace($"/DictionaryOfStringToCustomer/{key1}/Name", "James");
            var serialized = JsonConvert.SerializeObject(patchDocument);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<Class8>>(serialized);

            // Act
            patchDocument.ApplyTo(model);

            // Assert
            Assert.Equal(2, model.DictionaryOfStringToCustomer.Count);
            var actualValue1 = model.DictionaryOfStringToCustomer[key1];
            Assert.NotNull(actualValue1);
            Assert.Equal("James", actualValue1.Name);
        }

        [Fact]
        public void Replace_DeepNested_DictionaryValue_Succeeds()
        {
            // Arrange
            var key1 = "key1";
            var value1 = new Customer() { Name = "Jamesss" };
            var key2 = "key2";
            var value2 = new Customer() { Name = "Mike" };
            var model = new Class8();
            model.DictionaryOfStringToCustomer[key1] = value1;
            model.DictionaryOfStringToCustomer[key2] = value2;
            var patchDocument = new JsonPatchDocument();
            patchDocument.Replace($"/DictionaryOfStringToCustomer/{key1}/Name", "James");

            // Act
            patchDocument.ApplyTo(model);

            // Assert
            Assert.Equal(2, model.DictionaryOfStringToCustomer.Count);
            var actualValue1 = model.DictionaryOfStringToCustomer[key1];
            Assert.NotNull(actualValue1);
            Assert.Equal("James", actualValue1.Name);
        }

        [Fact]
        public void Replace_DeepNested_DictionaryValue_Succeeds_WithSerialization()
        {
            // Arrange
            var key1 = "key1";
            var value1 = new Customer() { Name = "James", Address = new Address { City = "Redmond" } };
            var key2 = "key2";
            var value2 = new Customer() { Name = "Mike", Address = new Address { City = "Seattle" } };
            var model = new Class8();
            model.DictionaryOfStringToCustomer[key1] = value1;
            model.DictionaryOfStringToCustomer[key2] = value2;
            var patchDocument = new JsonPatchDocument();
            patchDocument.Replace($"/DictionaryOfStringToCustomer/{key1}/Address/City", "Bellevue");
            var serialized = JsonConvert.SerializeObject(patchDocument);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<Class8>>(serialized);

            // Act
            patchDocument.ApplyTo(model);

            // Assert
            Assert.Equal(2, model.DictionaryOfStringToCustomer.Count);
            var actualValue1 = model.DictionaryOfStringToCustomer[key1];
            Assert.NotNull(actualValue1);
            Assert.Equal("James", actualValue1.Name);
            var address = actualValue1.Address;
            Assert.NotNull(address);
            Assert.Equal("Bellevue", address.City);
        }

        class Class9
        {
            public List<string> StringList { get; set; } = new List<string>();
        }

        [Fact]
        public void AddToNonIntegerListAtEnd()
        {
            // Arrange
            var model = new Class9()
            {
                StringList = new List<string>()
            };
            model.StringList.Add("string1");
            model.StringList.Add("string2");
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/StringList/0", "string3");

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            Assert.Equal(new List<string>() { "string3", "string1", "string2" }, model.StringList);
        }

        [Fact]
        public void AddMember_OnPOCO_WithNullPropertyValue_ShouldAddPropertyValue()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                StringProperty = null
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<string>(o => o.StringProperty, "B");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.StringProperty);
        }

        private class Class1
        {
            public IDictionary<string, string> USStates { get; set; } = new Dictionary<string, string>();
        }

        [Fact]
        public void AddMember_OnDictionaryProperty_ShouldAddKeyValueMember()
        {
            // Arrange
            var expected = "Washington";
            var model = new Class1();
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/USStates/WA", expected);

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            Assert.Equal(1, model.USStates.Count);
            Assert.Equal(expected, model.USStates["WA"]);
        }

        [Fact]
        public void AddMember_OnDictionaryProperty_ShouldAddKeyValueMember_WithSerialization()
        {
            // Arrange
            var expected = "Washington";
            var model = new Class1();
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/USStates/WA", expected);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<Class1>>(serialized);

            // Act
            deserialized.ApplyTo(model);

            // Assert
            Assert.Equal(1, model.USStates.Count);
            Assert.Equal(expected, model.USStates["WA"]);
        }

        private class Class2
        {
            public Class1 Class1Property { get; set; } = new Class1();
        }

        [Fact]
        public void AddMember_OnDictionaryPropertyDeeplyNested_ShouldAddKeyValueMember()
        {
            // Arrange
            var expected = "Washington";
            var model = new Class2();
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/Class1Property/USStates/WA", expected);

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            Assert.Equal(1, model.Class1Property.USStates.Count);
            Assert.Equal(expected, model.Class1Property.USStates["WA"]);
        }

        [Fact]
        public void AddMember_OnDictionaryPropertyDeeplyNested_ShouldAddKeyValueMember_WithSerialization()
        {
            // Arrange
            var expected = "Washington";
            var model = new Class2();
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/Class1Property/USStates/WA", expected);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<Class2>>(serialized);

            // Act
            deserialized.ApplyTo(model);

            // Assert
            Assert.Equal(1, model.Class1Property.USStates.Count);
            Assert.Equal(expected, model.Class1Property.USStates["WA"]);
        }

        [Fact]
        public void AddMember_OnDictionaryObjectDirectly_ShouldAddKeyValueMember()
        {
            // Arrange
            var expected = "Washington";
            var model = new Dictionary<string, string>();
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/WA", expected);

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            Assert.Equal(1, model.Count);
            Assert.Equal(expected, model["WA"]);
        }

        [Fact]
        public void AddMember_OnDictionaryObjectDirectly_ShouldAddKeyValueMember_WithSerialization()
        {
            // Arrange
            var expected = "Washington";
            var model = new Dictionary<string, string>();
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/WA", expected);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<Dictionary<string, string>>>(serialized);

            // Act
            deserialized.ApplyTo(model);

            // Assert
            Assert.Equal(1, model.Count);
            Assert.Equal(expected, model["WA"]);
        }

        [Fact]
        public void AddElement_ToListDirectly_ShouldAppendValue()
        {
            // Arrange
            var model = new List<int>() { 1, 2, 3 };
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/-", value: 4);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<List<int>>>(serialized);

            // Act
            deserialized.ApplyTo(model);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, model);
        }

        [Fact]
        public void AddElement_ToListDirectly_ShouldAppendValue_WithSerialization()
        {
            // Arrange
            var model = new List<int>() { 1, 2, 3 };
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/-", value: 4);

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, model);
        }

        [Fact]
        public void AddElement_ToListDirectly_ShouldAddValue_AtSuppliedPosition()
        {
            // Arrange
            var model = new List<int>() { 1, 2, 3 };
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/0", value: 4);

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, model);
        }

        [Fact]
        public void AddElement_ToListDirectly_ShouldAddValue_AtSuppliedPosition_WithSerialization()
        {
            // Arrange
            var model = new List<int>() { 1, 2, 3 };
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/0", value: 4);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<List<int>>>(serialized);

            // Act
            deserialized.ApplyTo(model);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, model);
        }

        class ListOnDictionary
        {
            public IDictionary<string, List<int>> NamesAndBadgeIds { get; set; } = new Dictionary<string, List<int>>();
        }

        [Fact]
        public void AddElement_ToList_OnDictionary_ShouldAddValue_AtSuppliedPosition()
        {
            // Arrange
            var model = new ListOnDictionary();
            model.NamesAndBadgeIds["James"] = new List<int>();
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/NamesAndBadgeIds/James/-", 200);

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            var list = model.NamesAndBadgeIds["James"];
            Assert.NotNull(list);
            Assert.Equal(new List<int>() { 200 }, list);
        }

        [Fact]
        public void AddElement_ToList_OnPOCO_ShouldAddValue_AtSuppliedPosition()
        {
            // Arrange
            var doc = new SimpleDTO()
            {
                IntegerIList = new List<int>() { 1, 2, 3 }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleDTO>();
            patchDoc.Add<int>(o => o.IntegerIList, 4, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.IntegerIList);
        }

        class Class3
        {
            public SimpleDTO SimpleDTOProperty { get; set; } = new SimpleDTO();
        }

        [Fact]
        public void AddElement_ToDeeplyNestedListProperty_OnPOCO_ShouldAddValue_AtSuppliedPosition()
        {
            // Arrange
            var model = new Class3()
            {
                SimpleDTOProperty = new SimpleDTO()
                {
                    IntegerIList = new List<int>() { 1, 2, 3 }
                }
            };
            var patchDoc = new JsonPatchDocument<Class3>();
            patchDoc.Add<int>(o => o.SimpleDTOProperty.IntegerIList, value: 4, position: 0);

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, model.SimpleDTOProperty.IntegerIList);
        }

        [Fact]
        public void AddElement_ToDeeplyNestedListProperty_OnPOCO_ShouldAddValue_AtSuppliedPosition_WithSerialization()
        {
            // Arrange
            var model = new Class3()
            {
                SimpleDTOProperty = new SimpleDTO()
                {
                    IntegerIList = new List<int>() { 1, 2, 3 }
                }
            };
            var patchDoc = new JsonPatchDocument<Class3>();
            patchDoc.Add<int>(o => o.SimpleDTOProperty.IntegerIList, value: 4, position: 0);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<Class3>>(serialized);

            // Act
            deserialized.ApplyTo(model);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, model.SimpleDTOProperty.IntegerIList);
        }

        class Class4
        {
            public int IntegerProperty { get; set; }
        }

        [Fact]
        public void Remove_OnNonReferenceType_POCOProperty_ShouldSetDefaultValue()
        {
            // Arrange
            var model = new Class4()
            {
                IntegerProperty = 10
            };
            var patchDoc = new JsonPatchDocument<Class4>();
            patchDoc.Remove<int>(o => o.IntegerProperty);

            // Act
            patchDoc.ApplyTo(model);

            // Assert
            Assert.Equal(0, model.IntegerProperty);
        }

        [Fact]
        public void Remove_OnNonReferenceType_POCOProperty_ShouldSetDefaultValue_WithSerialization()
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

        class ClassWithPrivateProperties
        {
            public string Name { get; set; }
            private int Age { get; set; } = 45;
        }

        [Fact]
        public void Add_OnPrivateProperties_FailesWithException()
        {
            // Arrange
            var doc = new ClassWithPrivateProperties()
            {
                Name = "James"
            };

            // create patch
            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/Age", 30);

            // Act & Assert
            var exception = Assert.Throws<JsonPatchException>(() => patchDoc.ApplyTo(doc));
            Assert.Equal(
                string.Format("The target location specified by path segment '{0}' was not found.", "Age"), 
                exception.Message);
        }
    }
}