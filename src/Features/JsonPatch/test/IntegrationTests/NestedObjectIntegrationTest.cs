// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Dynamic;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests
{
    public class NestedObjectIntegrationTest
    {
        [Fact]
        public void Replace_DTOWithNullCheck()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObjectWithNullCheck()
            {
                SimpleObjectWithNullCheck = new SimpleObjectWithNullCheck()
                {
                    StringProperty = "A"
                }
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObjectWithNullCheck>();
            patchDocument.Replace(o => o.SimpleObjectWithNullCheck.StringProperty, "B");

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal("B", targetObject.SimpleObjectWithNullCheck.StringProperty);
        }

        [Fact]
        public void ReplaceNestedObject_WithSerialization()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                IntegerValue = 1
            };

            var newNested = new NestedObject() { StringProperty = "B" };
            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Replace(o => o.NestedObject, newNested);

            var serialized = JsonConvert.SerializeObject(patchDocument);
            var deserialized = JsonConvert.DeserializeObject<JsonPatchDocument<SimpleObjectWithNestedObject>>(serialized);

            // Act
            deserialized.ApplyTo(targetObject);

            // Assert
            Assert.Equal("B", targetObject.NestedObject.StringProperty);
        }

        [Fact]
        public void TestStringProperty_InNestedObject()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                NestedObject = new NestedObject() { StringProperty = "A"}
            };

            var patchDocument = new JsonPatchDocument<NestedObject>();
            patchDocument.Test(o => o.StringProperty, "A");

            // Act
            patchDocument.ApplyTo(targetObject.NestedObject);

            // Assert
            Assert.Equal("A", targetObject.NestedObject.StringProperty);
        }

        [Fact]
        public void TestNestedObject()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                NestedObject = new NestedObject() { StringProperty = "B"}
            };

            var testNested = new NestedObject() { StringProperty = "B" };
            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Test(o => o.NestedObject, testNested);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal("B", targetObject.NestedObject.StringProperty);
        }

        [Fact]
        public void AddReplaces_ExistingStringProperty()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A"
                }
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Add(o => o.SimpleObject.StringProperty, "B");

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal("B", targetObject.SimpleObject.StringProperty);
        }

        [Fact]
        public void AddNewProperty_ToExpandoOject_InTypedObject()
        {
            var targetObject = new NestedObject()
            {
                DynamicProperty = new ExpandoObject()
            };

            var patchDocument = new JsonPatchDocument();
            patchDocument.Add("DynamicProperty/NewInt", 1);

            patchDocument.ApplyTo(targetObject);

            Assert.Equal(1, targetObject.DynamicProperty.NewInt);
        }

        [Fact]
        public void RemoveStringProperty()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A"
                }
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Remove(o => o.SimpleObject.StringProperty);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Null(targetObject.SimpleObject.StringProperty);
        }

        [Fact]
        public void CopyStringProperty_ToAnotherStringProperty()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = "A",
                    AnotherStringProperty = "B"
                }
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Copy(o => o.SimpleObject.StringProperty, o => o.SimpleObject.AnotherStringProperty);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal("A", targetObject.SimpleObject.AnotherStringProperty);
        }

        [Fact]
        public void CopyNullStringProperty_ToAnotherStringProperty()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    StringProperty = null,
                    AnotherStringProperty = "B"
                }
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Copy(o => o.SimpleObject.StringProperty, o => o.SimpleObject.AnotherStringProperty);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Null(targetObject.SimpleObject.AnotherStringProperty);
        }

        [Fact]
        public void Copy_DeepClonesObject()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
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

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Copy(o => o.InheritedObject, o => o.SimpleObject);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal("C", targetObject.SimpleObject.StringProperty);
            Assert.Equal("D", targetObject.SimpleObject.AnotherStringProperty);
            Assert.Equal("C", targetObject.InheritedObject.StringProperty);
            Assert.Equal("D", targetObject.InheritedObject.AnotherStringProperty);
            Assert.NotSame(targetObject.SimpleObject.StringProperty, targetObject.InheritedObject.StringProperty);
        }

        [Fact]
        public void Copy_KeepsObjectType()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject(),
                InheritedObject = new InheritedObject()
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Copy(o => o.InheritedObject, o => o.SimpleObject);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal(typeof(InheritedObject), targetObject.SimpleObject.GetType());
        }

        [Fact]
        public void Copy_BreaksObjectReference()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject(),
                InheritedObject = new InheritedObject()
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Copy(o => o.InheritedObject, o => o.SimpleObject);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.NotSame(targetObject.SimpleObject, targetObject.InheritedObject);
        }

        [Fact]
        public void MoveIntegerValue_ToAnotherIntegerProperty()
        {
            // Arrange
            var targetObject = new SimpleObjectWithNestedObject()
            {
                SimpleObject = new SimpleObject()
                {
                    IntegerValue = 2,
                    AnotherIntegerValue = 3
                }
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Move(o => o.SimpleObject.IntegerValue, o => o.SimpleObject.AnotherIntegerValue);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal(2, targetObject.SimpleObject.AnotherIntegerValue);
            Assert.Equal(0, targetObject.SimpleObject.IntegerValue);
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
            var targetObject = new SimpleObjectWithNestedObject()
            {
                SimpleObject = sDto,
                InheritedObject = iDto
            };

            var patchDocument = new JsonPatchDocument<SimpleObjectWithNestedObject>();
            patchDocument.Move(o => o.InheritedObject, o => o.SimpleObject);

            // Act
            patchDocument.ApplyTo(targetObject);

            // Assert
            Assert.Equal("C", targetObject.SimpleObject.StringProperty);
            Assert.Equal("D", targetObject.SimpleObject.AnotherStringProperty);
            Assert.Same(iDto, targetObject.SimpleObject);
            Assert.Null(targetObject.InheritedObject);
        }

        private class SimpleObjectWithNullCheck
        {
            private string stringProperty;

            public string StringProperty
            {
                get
                {
                    return stringProperty;
                }

                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(value));
                    }

                    stringProperty = value;
                }
            }
        }

        private class SimpleObjectWithNestedObjectWithNullCheck
        {
            public SimpleObjectWithNullCheck SimpleObjectWithNullCheck { get; set; }

            public SimpleObjectWithNestedObjectWithNullCheck()
            {
                SimpleObjectWithNullCheck = new SimpleObjectWithNullCheck();
            }
        }
    }
}
