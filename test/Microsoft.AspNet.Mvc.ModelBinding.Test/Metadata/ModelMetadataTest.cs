// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
#if NET45
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelMetadataTest
    {
#if NET45
        // Constructor

        [Fact]
        public void DefaultValues()
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>();

            // Act
            var metadata = new ModelMetadata(provider.Object, typeof(Exception), () => "model", typeof(string), "propertyName");

            // Assert
            Assert.Equal(typeof(Exception), metadata.ContainerType);
            Assert.True(metadata.ConvertEmptyStringToNull);
            Assert.False(metadata.IsComplexType);
            Assert.False(metadata.IsReadOnly);
            Assert.False(metadata.IsRequired);

            Assert.Null(metadata.Description);
            Assert.Null(metadata.DisplayFormatString);
            Assert.Null(metadata.DisplayName);
            Assert.Null(metadata.EditFormatString);
            Assert.Null(metadata.NullDisplayText);
            Assert.Null(metadata.TemplateHint);

            Assert.Equal("model", metadata.Model);
            Assert.Equal("model", metadata.SimpleDisplayText);
            Assert.Equal(typeof(string), metadata.ModelType);
            Assert.Equal("propertyName", metadata.PropertyName);
        }
#endif

        // IsComplexType

        private struct IsComplexTypeModel
        {
        }

#if NET45
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(Nullable<int>))]
        [InlineData(typeof(int))]
        public void IsComplexTypeTestsReturnsFalseForSimpleTypes(Type type)
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>();

            // Act
            var modelMetadata = new ModelMetadata(provider.Object, null, null, type, null);

            // Assert
            Assert.False(modelMetadata.IsComplexType);
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(IDisposable))]
        [InlineData(typeof(IsComplexTypeModel))]
        [InlineData(typeof(Nullable<IsComplexTypeModel>))]
        public void IsComplexTypeTestsReturnsTrueForComplexTypes(Type type)
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>();

            // Act
            var modelMetadata = new ModelMetadata(provider.Object, null, null, type, null);

            // Assert
            Assert.True(modelMetadata.IsComplexType);
        }

        // IsNullableValueType

        [Fact]
        public void IsNullableValueTypeTests()
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>();

            // Act & Assert
            Assert.False(new ModelMetadata(provider.Object, null, null, typeof(string), null).IsNullableValueType);
            Assert.False(new ModelMetadata(provider.Object, null, null, typeof(IDisposable), null).IsNullableValueType);
            Assert.True(new ModelMetadata(provider.Object, null, null, typeof(Nullable<int>), null).IsNullableValueType);
            Assert.False(new ModelMetadata(provider.Object, null, null, typeof(int), null).IsNullableValueType);
        }

        // IsRequired

        [Theory]
        [InlineData(typeof(string))]        
        [InlineData(typeof(IDisposable))]
        [InlineData(typeof(Nullable<int>))]
        public void IsRequired_ReturnsFalse_ForNullableTypes(Type modelType)
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>();
            var metadata = new ModelMetadata(provider.Object, 
                                             containerType: null, 
                                             modelAccessor: null, 
                                             modelType: modelType, 
                                             propertyName: null);

            // Act
            var isRequired = metadata.IsRequired;
            
            // Assert
            Assert.False(isRequired); 
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(DayOfWeek))]
        public void IsRequired_ReturnsTrue_ForNonNullableTypes(Type modelType)
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>();
            var metadata = new ModelMetadata(provider.Object,
                                             containerType: null,
                                             modelAccessor: null,
                                             modelType: modelType,
                                             propertyName: null);

            // Act
            var isRequired = metadata.IsRequired;

            // Assert
            Assert.True(isRequired);
        }

        // Properties

        [Fact]
        public void PropertiesCallsProvider()
        {
            // Arrange
            var modelType = typeof(string);
            var propertyMetadata = new List<ModelMetadata>();
            var provider = new Mock<IModelMetadataProvider>();
            var metadata = new ModelMetadata(provider.Object, null, null, modelType, null);
            provider.Setup(p => p.GetMetadataForProperties(null, modelType))
                .Returns(propertyMetadata)
                .Verifiable();

            // Act
            var result = metadata.Properties;

            // Assert
            Assert.Equal(propertyMetadata, result.ToList());
            provider.Verify();
        }
#endif

        [Fact]
        public void PropertiesListGetsResetWhenModelGetsReset()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(provider, null, () => new Class1(), typeof(Class1), null);

            // Act
            var originalProps = metadata.Properties.ToArray();
            metadata.Model = new Class2();
            var newProps = metadata.Properties.ToArray();

            // Assert
            var originalProp = Assert.Single(originalProps);
            Assert.Equal(typeof(string), originalProp.ModelType);
            Assert.Equal("Prop1", originalProp.PropertyName);
            var newProp = Assert.Single(newProps);
            Assert.Equal(typeof(int), newProp.ModelType);
            Assert.Equal("Prop2", newProp.PropertyName);
        }

        private class Class1
        {
            public string Prop1 { get; set; }
            public override string ToString()
            {
                return "Class1";
            }
        }

        private class Class2
        {
            public int Prop2 { get; set; }
        }

        // GetDisplayName()

        [Fact]
        public void GetDisplayName_ReturnsDisplayName_IfSet()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(provider, null, () => null, typeof(object), "unusedName")
            {
                DisplayName = "displayName",
            };

            // Act
            var result = metadata.GetDisplayName();

            // Assert
            Assert.Equal("displayName", result);
        }

#if NET45
        [Fact]
        public void ReturnsPropertyNameWhenSetAndDisplayNameIsNull()
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>();
            var metadata = new ModelMetadata(provider.Object, null, null, typeof(object), "PropertyName");

            // Act
            var result = metadata.GetDisplayName();

            // Assert
            Assert.Equal("PropertyName", result);
        }

        [Fact]
        public void ReturnsTypeNameWhenPropertyNameAndDisplayNameAreNull()
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>();
            var metadata = new ModelMetadata(provider.Object, null, null, typeof(object), null);

            // Act
            var result = metadata.GetDisplayName();

            // Assert
            Assert.Equal("Object", result);
        }
#endif

        // SimpleDisplayText

        public static IEnumerable<object[]> SimpleDisplayTextData
        {
            get
            {
                yield return new object[]
                        {
                            new Func<object>(() => new ComplexClass()
                                                {
                                                    Prop1 = new Class1 { Prop1 = "Hello" }
                                                }),
                            typeof(ComplexClass),
                            "Class1"
                        };
                yield return new object[]
                    {
                        new Func<object>(() => new Class1()),
                        typeof(Class1),
                        "Class1"
                    };
                yield return new object[]
                    {
                        new Func<object>(() => new ClassWithNoProperties()),
                        typeof(ClassWithNoProperties),
                        string.Empty
                    };
                yield return new object[]
                    {
                        null,
                        typeof(object),
                        null
                    };
            }
        }

        [Theory]
        [MemberData("SimpleDisplayTextData")]
        public void TestSimpleDisplayText(Func<object> modelAccessor, Type modelType, string expectedResult)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(provider, null, modelAccessor, modelType, null);

            // Act
            var result = metadata.SimpleDisplayText;

            // Assert
            Assert.Equal(expectedResult, result);
        }

        private class ClassWithNoProperties
        {
            public override string ToString()
            {
                return null;
            }
        }

        private class ComplexClass
        {
            public Class1 Prop1 { get; set; }
        }

        // Helpers

        private class DummyContactModel
        {
            public int IntField = 0;
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public int? NullableIntValue { get; set; }
            public int[] Array { get; set; }

            public string this[int index]
            {
                get { return "Indexed into " + index; }
            }
        }

        private class DummyModelContainer
        {
            public DummyContactModel Model { get; set; }
        }
    }
}
