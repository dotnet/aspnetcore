// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class DynamicObjectTest
    {
        [Fact]
        public void AddResults_ShouldReplaceExistingPropertyValue_InNestedDynamicObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.Nested = new NestedObject();
            dynamicTestObject.Nested.DynamicProperty = new DynamicTestObject();
            dynamicTestObject.Nested.DynamicProperty.InBetweenFirst = new DynamicTestObject();
            dynamicTestObject.Nested.DynamicProperty.InBetweenFirst.InBetweenSecond = new DynamicTestObject();
            dynamicTestObject.Nested.DynamicProperty.InBetweenFirst.InBetweenSecond.StringProperty = "A";

            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("/Nested/DynamicProperty/InBetweenFirst/InBetweenSecond/StringProperty", "B");

            // Act
            patchDoc.ApplyTo(dynamicTestObject);

            // Assert
            Assert.Equal("B", dynamicTestObject.Nested.DynamicProperty.InBetweenFirst.InBetweenSecond.StringProperty);
        }

        [Fact]
        public void ShouldNotBeAbleToAdd_ToNonExistingProperty_ThatIsNotTheRoot()
        {
            //Adding to a Nonexistent Target
            //
            //   An example target JSON document:
            //   { "foo": "bar" }
            //   A JSON Patch document:
            //   [
            //        { "op": "add", "path": "/baz/bat", "value": "qux" }
            //      ]
            //   This JSON Patch document, applied to the target JSON document above,
            //   would result in an error (therefore, it would not be applied),
            //   because the "add" operation's target location that references neither
            //   the root of the document, nor a member of an existing object, nor a
            //   member of an existing array.

            // Arrange
            var nestedObject = new NestedObject()
            {
                DynamicProperty = new DynamicTestObject()
            };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Add("DynamicProperty/OtherProperty/IntProperty", 1);

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDoc.ApplyTo(nestedObject);
            });

            // Assert
            Assert.Equal(
                string.Format(
                    "The target location specified by path segment '{0}' was not found.",
                    "OtherProperty"),
                exception.Message);
        }

        [Fact]
        public void CopyProperties_InNestedDynamicObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.NestedDynamicObject = new DynamicTestObject();
            dynamicTestObject.NestedDynamicObject.StringProperty = "A";
            dynamicTestObject.NestedDynamicObject.AnotherStringProperty = "B";

            var patchDoc = new JsonPatchDocument();
            patchDoc.Copy("NestedDynamicObject/StringProperty", "NestedDynamicObject/AnotherStringProperty");

            // Act
            patchDoc.ApplyTo(dynamicTestObject);

            // Assert
            Assert.Equal("A", dynamicTestObject.NestedDynamicObject.AnotherStringProperty);
        }


        [Fact]
        public void MoveToNonExistingProperty_InDynamicObject_ShouldAddNewProperty()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.StringProperty = "A";

            var patchDoc = new JsonPatchDocument();
            patchDoc.Move("StringProperty", "AnotherStringProperty");

            // Act
            patchDoc.ApplyTo(dynamicTestObject);
            dynamicTestObject.TryGetValue("StringProperty", out object valueFromDictionary);

            // Assert
            Assert.Equal("A", dynamicTestObject.AnotherStringProperty);
            Assert.Null(valueFromDictionary);
        }

        [Fact]
        public void MovePropertyValue_FromDynamicObject_ToTypedObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.StringProperty = "A";
            dynamicTestObject.SimpleObject = new SimpleObject() { AnotherStringProperty = "B" };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Move("StringProperty", "SimpleObject/AnotherStringProperty");

            // Act
            patchDoc.ApplyTo(dynamicTestObject);
            dynamicTestObject.TryGetValue("StringProperty", out object valueFromDictionary);

            // Assert
            Assert.Equal("A", dynamicTestObject.SimpleObject.AnotherStringProperty);
            Assert.Null(valueFromDictionary);
        }

        [Fact]
        public void MovePropertyValue_FromListToNonList_InNestedTypedObject_InDynamicObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.Nested = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Move("Nested/IntegerList/0", "Nested/IntegerValue");

            // Act
            patchDoc.ApplyTo(dynamicTestObject);

            // Assert
            Assert.Equal(new List<int>() { 2, 3 }, dynamicTestObject.Nested.IntegerList);
            Assert.Equal(1, dynamicTestObject.Nested.IntegerValue);
        }

        [Fact]
        public void RemoveNestedProperty_FromDynamicObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.Test = new DynamicTestObject();
            dynamicTestObject.Test.AnotherTest = "A";

            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("Test");

            // Act
            patchDoc.ApplyTo(dynamicTestObject);
            dynamicTestObject.TryGetValue("Test", out object valueFromDictionary);

            // Assert
            Assert.Null(valueFromDictionary);
        }

        [Fact]
        public void RemoveFromNestedObject_InDynamicObject_MixedCase_ThrowsPathNotFoundException()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.SimpleObject = new SimpleObject()
            {
                StringProperty = "A"
            };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("Simpleobject/stringProperty");

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDoc.ApplyTo(dynamicTestObject);
            });

            // Assert
            Assert.Equal(
               string.Format("The target location specified by path segment '{0}' was not found.",
               "Simpleobject"),
                exception.Message);
        }

        [Fact]
        public void RemoveFromList_NestedInDynamicObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.SimpleObject = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Remove("SimpleObject/IntegerList/2");

            // Act
            patchDoc.ApplyTo(dynamicTestObject);

            // Assert
            Assert.Equal(new List<int>() { 1, 2 }, dynamicTestObject.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceNestedTypedObject_InDynamicObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.SimpleObject = new SimpleObject()
            {
                IntegerValue = 5,
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            var newObject = new SimpleObject()
            {
                DoubleValue = 1
            };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("SimpleObject", newObject);

            // Act
            patchDoc.ApplyTo(dynamicTestObject);

            // Assert
            Assert.Equal(1, dynamicTestObject.SimpleObject.DoubleValue);
            Assert.Equal(0, dynamicTestObject.SimpleObject.IntegerValue);
            Assert.Null(dynamicTestObject.SimpleObject.IntegerList);
        }

        [Fact]
        public void ReplaceFullList_InDynamicObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.IntegerList = new List<int>() { 1, 2, 3 };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Replace("IntegerList", new List<int>() { 4, 5, 6 });

            // Act
            patchDoc.ApplyTo(dynamicTestObject);

            // Assert
            Assert.Equal(new List<int>() { 4, 5, 6 }, dynamicTestObject.IntegerList);
        }

        [Fact]
        public void TestPropertyValue_FromListToNonList_InNestedTypedObject_InDynamicObject()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.Nested = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Test("Nested/IntegerList/1", 2);

            // Act & Assert
            patchDoc.ApplyTo(dynamicTestObject);
        }

        [Fact]
        public void TestPropertyValue_FromListToNonList_InNestedTypedObject_InDynamicObject_ThrowsJsonPatchException_IfTestFails()
        {
            // Arrange
            dynamic dynamicTestObject = new DynamicTestObject();
            dynamicTestObject.Nested = new SimpleObject()
            {
                IntegerList = new List<int>() { 1, 2, 3 }
            };

            var patchDoc = new JsonPatchDocument();
            patchDoc.Test("Nested/IntegerList/0", 2);

            // Act
            var exception = Assert.Throws<JsonPatchException>(() =>
            {
                patchDoc.ApplyTo(dynamicTestObject);
            });

            // Assert
            Assert.Equal("The current value '1' at position '0' is not equal to the test value '2'.", exception.Message);
        }
    }
}
