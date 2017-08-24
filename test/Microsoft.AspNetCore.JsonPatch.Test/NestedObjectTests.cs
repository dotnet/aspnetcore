// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class NestedObjectTests
    {
        [Fact]
        public void ReplacePropertyInNestedObject()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                IntegerValue = 1
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.NestedObject.StringProperty, "B");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.NestedObject.StringProperty);
        }

        [Fact]
        public void ReplacePropertyInNestedObjectWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                IntegerValue = 1
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.NestedObject.StringProperty, "B");

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.NestedObject.StringProperty);
        }

        [Fact]
        public void ReplaceNestedObject()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                IntegerValue = 1
            };

            var newNested = new NestedObject() { StringProperty = "B" };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.NestedObject, newNested);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.NestedObject.StringProperty);
        }

        [Fact]
        public void ReplaceNestedObjectWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                IntegerValue = 1
            };

            var newNested = new NestedObject() { StringProperty = "B" };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.NestedObject, newNested);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.NestedObject.StringProperty);
        }

        [Fact]
        public void AddResultsInReplace()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.StringProperty, "B");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleObject.StringProperty);
        }

        [Fact]
        public void AddResultsInReplaceWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.StringProperty, "B");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleObject.StringProperty);
        }

        [Fact]
        public void AddToList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void AddToListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void AddToIntegerIList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerIList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => (List<int>)o.SimpleObject.IntegerIList, 4, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.SimpleObject.IntegerIList);
        }

        [Fact]
        public void AddToIntegerIListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerIList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => (List<int>)o.SimpleObject.IntegerIList, 4, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.SimpleObject.IntegerIList);
        }

        [Fact]
        public void AddToNestedIntegerIList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObjectIList = new List<SimpleObject>
                {
                    new SimpleObject
                    {
                        IntegerIList = new List<int>() { 1, 2, 3 }
                    }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => (List<int>)o.SimpleObjectIList[0].IntegerIList, 4, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.SimpleObjectIList[0].IntegerIList);
        }

        [Fact]
        public void AddToNestedIntegerIListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObjectIList = new List<SimpleObject>
                {
                    new SimpleObject
                    {
                        IntegerIList = new List<int>() { 1, 2, 3 }
                    }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => (List<int>)o.SimpleObjectIList[0].IntegerIList, 4, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 1, 2, 3 }, doc.SimpleObjectIList[0].IntegerIList);
        }

        [Fact]
        public void AddToComplextTypeListSpecifyIndex()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObjectList = new List<SimpleObject>()
                {
                    new SimpleObject
                    {
                        StringProperty = "String1"
                    },
                    new SimpleObject
                    {
                        StringProperty = "String2"
                    }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObjectList[0].StringProperty, "ChangedString1");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("ChangedString1", doc.SimpleObjectList[0].StringProperty);
        }

        [Fact]
        public void AddToComplextTypeListSpecifyIndexWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObjectList = new List<SimpleObject>()
                {
                    new SimpleObject
                    {
                        StringProperty = "String1"
                    },
                    new SimpleObject
                    {
                        StringProperty = "String2"
                    }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObjectList[0].StringProperty, "ChangedString1");

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("ChangedString1", doc.SimpleObjectList[0].StringProperty);
        }

        [Fact]
        public void AddToListInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4, 4);

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
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4, 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act & Assert
            var exception = Assert.Throws<JsonPatchException>(() =>
                {
                    deserialized.ApplyTo(doc);
                });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "4"),
                exception.Message);
        }

        [Fact]
        public void AddToListInvalidPositionTooLarge_LogsError()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4, 4);

            var logger = new TestErrorLogger<SimpleObjectWithNestedObject>();

            patchDoc.ApplyTo(doc, logger.LogErrorMessage);


            //Assert
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "4"),
                logger.ErrorMessage);

        }

        [Fact]
        public void AddToListInvalidPositionTooSmall()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4, -1);

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
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

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
        public void AddToListInvalidPositionTooSmall_LogsError()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4, -1);

            var logger = new TestErrorLogger<SimpleObjectWithNestedObject>();


            patchDoc.ApplyTo(doc, logger.LogErrorMessage);


            //Assert
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                logger.ErrorMessage);
        }

        [Fact]
        public void AddToListAppend()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void AddToListAppendWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Add(o => o.SimpleObject.IntegerList, 4);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 4 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void Remove()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.StringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Null(doc.SimpleObject.StringProperty);
        }

        [Fact]
        public void RemoveWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.StringProperty);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Null(doc.SimpleObject.StringProperty);
        }

        [Fact]
        public void RemoveFromList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.IntegerList, 2);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void RemoveFromListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.IntegerList, 2);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void RemoveFromListInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.IntegerList, 3);

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
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.IntegerList, 3);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

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
        public void RemoveFromListInvalidPositionTooLarge_LogsError()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.IntegerList, 3);

            var logger = new TestErrorLogger<SimpleObjectWithNestedObject>();

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
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.IntegerList, -1);

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
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.IntegerList, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

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
        public void RemoveFromListInvalidPositionTooSmall_LogsError()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove(o => o.SimpleObject.IntegerList, -1);

            var logger = new TestErrorLogger<SimpleObjectWithNestedObject>();


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
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove<int>(o => o.SimpleObject.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void RemoveFromEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Remove<int>(o => o.SimpleObject.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void Replace()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    DecimalValue = 10
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.StringProperty, "B");
            patchDoc.Replace(o => o.SimpleObject.DecimalValue, 12);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleObject.StringProperty);
            Assert.Equal(12, doc.SimpleObject.DecimalValue);
        }

        [Fact]
        public void Replace_DTOWithNullCheck()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObjectWithNullCheck()
            {
                SimpleObjectWithNullCheck = new SimpleObjectWithNullCheck()
                {
                    StringProperty = "A"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObjectWithNullCheck>();
            patchDoc.Replace(o => o.SimpleObjectWithNullCheck.StringProperty, "B");

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleObjectWithNullCheck.StringProperty);
        }

        [Fact]
        public void ReplaceWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    DecimalValue = 10
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.StringProperty, "B");
            patchDoc.Replace(o => o.SimpleObject.DecimalValue, 12);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleObject.StringProperty);
            Assert.Equal(12, doc.SimpleObject.DecimalValue);
        }

        [Fact]
        public void SerializationTests()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    DecimalValue = 10,
                    DoubleValue = 10,
                    FloatValue = 10,
                    IntegerValue = 10
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.StringProperty, "B");
            patchDoc.Replace(o => o.SimpleObject.DecimalValue, 12);
            patchDoc.Replace(o => o.SimpleObject.DoubleValue, 12);
            patchDoc.Replace(o => o.SimpleObject.FloatValue, 12);
            patchDoc.Replace(o => o.SimpleObject.IntegerValue, 12);

            // serialize & deserialize
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserizalized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserizalized.ApplyTo(doc);

            // Assert
            Assert.Equal("B", doc.SimpleObject.StringProperty);
            Assert.Equal(12, doc.SimpleObject.DecimalValue);
            Assert.Equal(12, doc.SimpleObject.DoubleValue);
            Assert.Equal(12, doc.SimpleObject.FloatValue);
            Assert.Equal(12, doc.SimpleObject.IntegerValue);
        }

        [Fact]
        public void ReplaceInList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceFullList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, new List<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Arrange
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithSerialiation()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, new List<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceFullListFromEnumerable()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace<IEnumerable<int>>(o => o.SimpleObject.IntegerList, new List<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceFullListFromEnumerableWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace<IEnumerable<int>>(o => o.SimpleObject.IntegerList, new List<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithCollection()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace<IEnumerable<int>>(o => o.SimpleObject.IntegerList, new Collection<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceFullListWithCollectionWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace<IEnumerable<int>>(o => o.SimpleObject.IntegerList, new Collection<int>() { 4, 5, 6 });

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceAtEndOfList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 5 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceAtEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 5 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceInListInvalidInvalidPositionTooLarge()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5, 3);

            // Act & Assert
            var exception = Assert.Throws<JsonPatchException>(() => { patchDoc.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
                exception.Message);
        }

        [Fact]
        public void ReplaceInListInvalidInvalidPositionTooLargeWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5, 3);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

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
        public void ReplaceInListInvalid_PositionTooLarge_LogsError()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5, 3);

            var logger = new TestErrorLogger<SimpleObjectWithNestedObject>();


            patchDoc.ApplyTo(doc, logger.LogErrorMessage);


            // Assert
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "3"),
                logger.ErrorMessage);
        }

        [Fact]
        public void ReplaceInListInvalidPositionTooSmall()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5, -1);

            // Act & Assert
            var exception = Assert.Throws<JsonPatchException>(() => { patchDoc.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
        }

        [Fact]
        public void ReplaceInListInvalidPositionTooSmallWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5, -1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act & Assert
            var exception = Assert.Throws<JsonPatchException>(() => { deserialized.ApplyTo(doc); });
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                exception.Message);
        }

        [Fact]
        public void ReplaceInListInvalidPositionTooSmall_LogsError()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Replace(o => o.SimpleObject.IntegerList, 5, -1);

            var logger = new TestErrorLogger<SimpleObjectWithNestedObject>();


            patchDoc.ApplyTo(doc, logger.LogErrorMessage);


            // Assert
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", "-1"),
                logger.ErrorMessage);
        }

        [Fact]
        public void Copy()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.StringProperty, o => o.SimpleObject.AnotherStringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.SimpleObject.AnotherStringProperty);
        }

        [Fact]
        public void CopyWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.StringProperty, o => o.SimpleObject.AnotherStringProperty);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.SimpleObject.AnotherStringProperty);
        }

        [Fact]
        public void CopyInList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerList, 1);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void CopyInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerList, 1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void CopyFromListToEndOfList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 1 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void CopyFromListToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 1 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void CopyFromListToNonList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerValue);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(1, doc.SimpleObject.IntegerValue);
        }

        [Fact]
        public void CopyFromListToNonListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerValue);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(1, doc.SimpleObject.IntegerValue);
        }

        [Fact]
        public void CopyFromNonListToList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.IntegerList, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void CopyFromNonListToListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.IntegerList, 0);
            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void CopyToEndOfList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void CopyToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void Copy_DeepClonesObject()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                },
                InheritedObject = new InheritedObject()
                {
                    StringProperty = "C",
                    AnotherStringProperty = "D"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.InheritedObject, o => o.SimpleObject);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("C", doc.SimpleObject.StringProperty);
            Assert.Equal("D", doc.SimpleObject.AnotherStringProperty);
            Assert.Equal("C", doc.InheritedObject.StringProperty);
            Assert.Equal("D", doc.InheritedObject.AnotherStringProperty);
            Assert.NotSame(doc.SimpleObject.StringProperty, doc.InheritedObject.StringProperty);
        }

        [Fact]
        public void Copy_KeepsObjectType()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject(),
                InheritedObject = new InheritedObject()
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.InheritedObject, o => o.SimpleObject);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(typeof(InheritedObject), doc.SimpleObject.GetType());
        }

        [Fact]
        public void Copy_BreaksObjectReference()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject(),
                InheritedObject = new InheritedObject()
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Copy(o => o.InheritedObject, o => o.SimpleObject);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.NotSame(doc.SimpleObject, doc.InheritedObject);
        }

        [Fact]
        public void Move()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.StringProperty, o => o.SimpleObject.AnotherStringProperty);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.SimpleObject.AnotherStringProperty);
            Assert.Null(doc.SimpleObject.StringProperty);
        }

        [Fact]
        public void MoveWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.StringProperty, o => o.SimpleObject.AnotherStringProperty);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("A", doc.SimpleObject.AnotherStringProperty);
            Assert.Null(doc.SimpleObject.StringProperty);
        }

        [Fact]
        public void Move_KeepsObjectReference()
        {
            // Arrange
            var sDto = new SimpleObject()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };
            var iDto = new InheritedObject()
            {
                StringProperty = "C",
                AnotherStringProperty = "D"
            };
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = sDto,
                InheritedObject = iDto
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.InheritedObject, o => o.SimpleObject);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal("C", doc.SimpleObject.StringProperty);
            Assert.Equal("D", doc.SimpleObject.AnotherStringProperty);
            Assert.Same(iDto, doc.SimpleObject);
            Assert.Null(doc.InheritedObject);
        }

        [Fact]
        public void Move_KeepsObjectReferenceWithSerialization()
        {
            // Arrange
            var sDto = new SimpleObject()
            {
                StringProperty = "A",
                AnotherStringProperty = "B"
            };
            var iDto = new InheritedObject()
            {
                StringProperty = "C",
                AnotherStringProperty = "D"
            };
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = sDto,
                InheritedObject = iDto
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.InheritedObject, o => o.SimpleObject);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal("C", doc.SimpleObject.StringProperty);
            Assert.Equal("D", doc.SimpleObject.AnotherStringProperty);
            Assert.Same(iDto, doc.SimpleObject);
            Assert.Null(doc.InheritedObject);
        }

        [Fact]
        public void MoveInList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerList, 1);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 1, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void MoveInListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerList, 1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 1, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void Move_KeepsObjectReferenceInList()
        {
            // Arrange
            var sDto1 = new SimpleObject() { IntegerValue = 1 };
            var sDto2 = new SimpleObject() { IntegerValue = 2 };
            var sDto3 = new SimpleObject() { IntegerValue = 3 };
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObjectList = new List<SimpleObject>() {
                    sDto1,
                    sDto2,
                    sDto3
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObjectList, 0, o => o.SimpleObjectList, 1);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<SimpleObject>() { sDto2, sDto1, sDto3 }, doc.SimpleObjectList);
            Assert.Equal(2, doc.SimpleObjectList[0].IntegerValue);
            Assert.Equal(1, doc.SimpleObjectList[1].IntegerValue);
            Assert.Same(sDto2, doc.SimpleObjectList[0]);
            Assert.Same(sDto1, doc.SimpleObjectList[1]);
        }

        [Fact]
        public void Move_KeepsObjectReferenceInListWithSerialization()
        {
            // Arrange
            var sDto1 = new SimpleObject() { IntegerValue = 1 };
            var sDto2 = new SimpleObject() { IntegerValue = 2 };
            var sDto3 = new SimpleObject() { IntegerValue = 3 };
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObjectList = new List<SimpleObject>() {
                    sDto1,
                    sDto2,
                    sDto3
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObjectList, 0, o => o.SimpleObjectList, 1);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<SimpleObject>() { sDto2, sDto1, sDto3 }, doc.SimpleObjectList);
            Assert.Equal(2, doc.SimpleObjectList[0].IntegerValue);
            Assert.Equal(1, doc.SimpleObjectList[1].IntegerValue);
            Assert.Same(sDto2, doc.SimpleObjectList[0]);
            Assert.Same(sDto1, doc.SimpleObjectList[1]);
        }

        [Fact]
        public void MoveFromListToEndOfList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3, 1 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void MoveFromListToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3, 1 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void MoveFomListToNonList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerValue);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.SimpleObject.IntegerList);
            Assert.Equal(1, doc.SimpleObject.IntegerValue);
        }

        [Fact]
        public void MoveFomListToNonListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerList, 0, o => o.SimpleObject.IntegerValue);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.SimpleObject.IntegerList);
            Assert.Equal(1, doc.SimpleObject.IntegerValue);
        }

        [Fact]
        public void MoveFomListToNonListBetweenHierarchy()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerList, 0, o => o.IntegerValue);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.SimpleObject.IntegerList);
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void MoveFomListToNonListBetweenHierarchyWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerList, 0, o => o.IntegerValue);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, doc.SimpleObject.IntegerList);
            Assert.Equal(1, doc.IntegerValue);
        }

        [Fact]
        public void MoveFromNonListToList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.IntegerList, 0);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void MoveFromNonListToListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.IntegerList, 0);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 5, 1, 2, 3 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void MoveToEndOfList()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.IntegerList);

            // Act
            patchDoc.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleObject.IntegerList);
        }

        [Fact]
        public void MoveToEndOfListWithSerialization()
        {
            // Arrange
            var doc = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 5,
                    IntegerList = new List<int>() { 1, 2, 3 }
                }
            };

            // create patch
            var patchDoc = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDoc.Move(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.IntegerList);

            var serialized = JsonConvert.SerializeObject(patchDoc);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(doc);

            // Assert
            Assert.Equal(0, doc.IntegerValue);
            Assert.Equal(new List<int>() { 1, 2, 3, 5 }, doc.SimpleObject.IntegerList);
        }
    }
}
