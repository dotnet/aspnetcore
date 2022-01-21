// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests;

public class ListIntegrationTest
{
    [Fact]
    public void TestInList_IsSuccessful()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Test(o => o.SimpleObject.IntegerList, 3, 2);

        // Act & Assert
        patchDocument.ApplyTo(targetObject);
    }

    [Fact]
    public void TestInList_InvalidPosition()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Test(o => o.SimpleObject.IntegerList, 4, -1);

        // Act & Assert
        var exception = Assert.Throws<JsonPatchException>(() => { patchDocument.ApplyTo(targetObject); });
        Assert.Equal("The index value provided by path segment '-1' is out of bounds of the array size.",
            exception.Message);
    }

    [Fact]
    public void AddToIntegerIList()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObject = new SimpleObject()
            {
                IntegerIList = new List<int>() { 1, 2, 3 }
            }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Add(o => (List<int>)o.SimpleObject.IntegerIList, 4, 0);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<int>() { 4, 1, 2, 3 }, targetObject.SimpleObject.IntegerIList);
    }

    [Fact]
    public void AddToComplextTypeList_SpecifyIndex()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
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

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Add(o => o.SimpleObjectList[0].StringProperty, "ChangedString1");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal("ChangedString1", targetObject.SimpleObjectList[0].StringProperty);
    }

    [Fact]
    public void AddToListAppend()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Add(o => o.SimpleObject.IntegerList, 4);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<int>() { 1, 2, 3, 4 }, targetObject.SimpleObject.IntegerList);
    }

    [Fact]
    public void RemoveFromList()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            IntegerList = new List<int>() { 1, 2, 3 }
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove("IntegerList/2");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<int>() { 1, 2 }, targetObject.IntegerList);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("-1")]
    public void RemoveFromList_InvalidPosition(string position)
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            IntegerList = new List<int>() { 1, 2, 3 }
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove("IntegerList/" + position);

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.ApplyTo(targetObject);
        });

        // Assert
        Assert.Equal($"The index value provided by path segment '{position}' is out of bounds of the array size.", exception.Message);
    }

    [Fact]
    public void Remove_FromEndOfList()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Remove<int>(o => o.SimpleObject.IntegerList);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<int>() { 1, 2 }, targetObject.SimpleObject.IntegerList);
    }

    [Fact]
    public void ReplaceFullList_WithCollection()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            IntegerList = new List<int>() { 1, 2, 3 }
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Replace("IntegerList", new Collection<int>() { 4, 5, 6 });

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<int>() { 4, 5, 6 }, targetObject.IntegerList);
    }

    [Fact]
    public void Replace_AtEndOfList()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Replace(o => o.SimpleObject.IntegerList, 5);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<int>() { 1, 2, 5 }, targetObject.SimpleObject.IntegerList);
    }

    [Fact]
    public void Replace_InList_InvalidPosition()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Replace(o => o.SimpleObject.IntegerList, 5, -1);

        // Act
        var exception = Assert.Throws<JsonPatchException>(() => { patchDocument.ApplyTo(targetObject); });

        // Assert
        Assert.Equal("The index value provided by path segment '-1' is out of bounds of the array size.", exception.Message);
    }

    [Fact]
    public void CopyFromListToEndOfList()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            IntegerList = new List<int>() { 1, 2, 3 }
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("IntegerList/0", "IntegerList/-");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<int>() { 1, 2, 3, 1 }, targetObject.IntegerList);
    }

    [Fact]
    public void CopyFromListToNonList()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            IntegerList = new List<int>() { 1, 2, 3 }
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("IntegerList/0", "IntegerValue");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(1, targetObject.IntegerValue);
    }

    [Fact]
    public void MoveToEndOfList()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            IntegerValue = 5,
            IntegerList = new List<int>() { 1, 2, 3 }
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Move("IntegerValue", "IntegerList/-");

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(0, targetObject.IntegerValue);
        Assert.Equal(new List<int>() { 1, 2, 3, 5 }, targetObject.IntegerList);
    }

    [Fact]
    public void Move_KeepsObjectReferenceInList()
    {
        // Arrange
        var simpleObject1 = new SimpleObject() { IntegerValue = 1 };
        var simpleObject2 = new SimpleObject() { IntegerValue = 2 };
        var simpleObject3 = new SimpleObject() { IntegerValue = 3 };
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObjectList = new List<SimpleObject>() {
                    simpleObject1,
                    simpleObject2,
                    simpleObject3
                }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Move(o => o.SimpleObjectList, 0, o => o.SimpleObjectList, 1);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<SimpleObject>() { simpleObject2, simpleObject1, simpleObject3 }, targetObject.SimpleObjectList);
        Assert.Equal(2, targetObject.SimpleObjectList[0].IntegerValue);
        Assert.Equal(1, targetObject.SimpleObjectList[1].IntegerValue);
        Assert.Same(simpleObject2, targetObject.SimpleObjectList[0]);
        Assert.Same(simpleObject1, targetObject.SimpleObjectList[1]);
    }

    [Fact]
    public void MoveFromList_ToNonList_BetweenHierarchy()
    {
        // Arrange
        var targetObject = new SimpleObjectWithNestedObject()
        {
            SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            }
        };

        var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
        patchDocument.Move(o => o.SimpleObject.IntegerList, 0, o => o.IntegerValue);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(new List<int>() { 2, 3 }, targetObject.SimpleObject.IntegerList);
        Assert.Equal(1, targetObject.IntegerValue);
    }
}
