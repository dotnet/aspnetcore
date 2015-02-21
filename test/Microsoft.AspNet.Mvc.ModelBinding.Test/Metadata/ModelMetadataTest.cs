// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelMetadataTest
    {
        public static TheoryData<Action<ModelMetadata>, Func<ModelMetadata, object>, object> MetadataModifierData
        {
            get
            {
                var emptycontainerModel = new DummyModelContainer();
                var contactModel = new DummyContactModel { FirstName = "test" };
                var nonEmptycontainerModel = new DummyModelContainer { Model = contactModel };

                var binderMetadata = new TestBinderMetadata();
                var predicateProvider = new DummyPropertyBindingPredicateProvider();

                return new TheoryData<Action<ModelMetadata>, Func<ModelMetadata, object>, object>
                {
                    { m => m.ConvertEmptyStringToNull = false, m => m.ConvertEmptyStringToNull, false },
                    { m => m.HasNonDefaultEditFormat = true, m => m.HasNonDefaultEditFormat, true },
                    { m => m.HideSurroundingHtml = true, m => m.HideSurroundingHtml, true },
                    { m => m.HtmlEncode = false, m => m.HtmlEncode, false },
                    { m => m.IsReadOnly = true, m => m.IsReadOnly, true },
                    { m => m.IsRequired = true, m => m.IsRequired, true },
                    { m => m.ShowForDisplay = false, m => m.ShowForDisplay, false },
                    { m => m.ShowForEdit = false, m => m.ShowForEdit, false },

                    { m => m.DataTypeName = "New data type name", m => m.DataTypeName, "New data type name" },
                    { m => m.Description = "New description", m => m.Description, "New description" },
                    { m => m.DisplayFormatString = "New display format", m => m.DisplayFormatString, "New display format" },
                    { m => m.DisplayName = "New display name", m => m.DisplayName, "New display name" },
                    { m => m.EditFormatString = "New edit format", m => m.EditFormatString, "New edit format" },
                    { m => m.NullDisplayText = "New null display", m => m.NullDisplayText, "New null display" },
                    { m => m.SimpleDisplayText = "New simple display", m => m.SimpleDisplayText, "New simple display" },
                    { m => m.TemplateHint = "New template hint", m => m.TemplateHint, "New template hint" },

                    { m => m.Order = 23, m => m.Order, 23 },
                    { m => m.Container = null, m => m.Container, null },
                    { m => m.Container = emptycontainerModel, m => m.Container, emptycontainerModel },
                    { m => m.Container = nonEmptycontainerModel, m => m.Container, nonEmptycontainerModel },

                    { m => m.BinderMetadata = null, m => m.BinderMetadata, null },
                    { m => m.BinderMetadata = binderMetadata, m => m.BinderMetadata, binderMetadata },
                    { m => m.BinderModelName = null, m => m.BinderModelName, null },
                    { m => m.BinderModelName = "newModelName", m => m.BinderModelName, "newModelName" },
                    { m => m.BinderModelName = string.Empty, m => m.BinderModelName, string.Empty },
                    { m => m.BinderType = null, m => m.BinderType, null },
                    { m => m.BinderType = typeof(string), m => m.BinderType, typeof(string) },
                    { m => m.PropertyBindingPredicateProvider = null, m => m.PropertyBindingPredicateProvider, null },
                    { m => m.PropertyBindingPredicateProvider = predicateProvider, m => m.PropertyBindingPredicateProvider, predicateProvider },
                };
            }
        }

        // Constructor

        [Fact]
        public void DefaultValues()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act
            var metadata =
                new ModelMetadata(provider, typeof(Exception), () => "model", typeof(string), "propertyName");

            // Assert
            Assert.NotNull(metadata.AdditionalValues);
            Assert.Empty(metadata.AdditionalValues);
            Assert.Equal(typeof(Exception), metadata.ContainerType);
            Assert.Null(metadata.Container);

            Assert.True(metadata.ConvertEmptyStringToNull);
            Assert.False(metadata.HasNonDefaultEditFormat);
            Assert.False(metadata.HideSurroundingHtml);
            Assert.True(metadata.HtmlEncode);
            Assert.False(metadata.IsComplexType);
            Assert.False(metadata.IsCollectionType);
            Assert.False(metadata.IsNullableValueType);
            Assert.False(metadata.IsReadOnly);
            Assert.False(metadata.IsRequired);
            Assert.True(metadata.ShowForDisplay);
            Assert.True(metadata.ShowForEdit);

            Assert.Null(metadata.DataTypeName);
            Assert.Null(metadata.Description);
            Assert.Null(metadata.DisplayFormatString);
            Assert.Null(metadata.DisplayName);
            Assert.Null(metadata.EditFormatString);
            Assert.Null(metadata.NullDisplayText);
            Assert.Null(metadata.TemplateHint);

            Assert.Equal("model", metadata.Model);
            Assert.Equal("model", metadata.SimpleDisplayText);
            Assert.Equal(typeof(string), metadata.RealModelType);
            Assert.Equal(typeof(string), metadata.ModelType);
            Assert.Equal("propertyName", metadata.PropertyName);

            Assert.Equal(10000, ModelMetadata.DefaultOrder);
            Assert.Equal(ModelMetadata.DefaultOrder, metadata.Order);

            Assert.Null(metadata.BinderModelName);
            Assert.Null(metadata.BinderType);
            Assert.Null(metadata.BinderMetadata);
            Assert.Null(metadata.PropertyBindingPredicateProvider);
        }


        // AdditionalValues

        [Fact]
        public void AdditionalValues_CreatedOnce()
        {

            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: () => null,
                modelType: typeof(object),
                propertyName: null);

            // Act
            var result1 = metadata.AdditionalValues;
            var result2 = metadata.AdditionalValues;

            // Assert
            Assert.Same(result1, result2);
        }

        [Fact]
        public void AdditionalValues_ChangesPersist()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: () => null,
                modelType: typeof(object),
                propertyName: null);
            var valuesDictionary = new Dictionary<object, object>
            {
                { "key1", new object() },
                { "key2", "value2" },
                { "key3", new object() },
            };

            // Act
            foreach (var keyValuePair in valuesDictionary)
            {
                metadata.AdditionalValues.Add(keyValuePair);
            }

            // Assert
            Assert.Equal(valuesDictionary, metadata.AdditionalValues);
        }

        // IsComplexType

        private struct IsComplexTypeModel
        {
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(Nullable<int>))]
        [InlineData(typeof(int))]
        public void IsComplexTypeTestsReturnsFalseForSimpleTypes(Type type)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act
            var modelMetadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: null,
                modelType: type,
                propertyName: null);

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
            var provider = new EmptyModelMetadataProvider();

            // Act
            var modelMetadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: null,
                modelType: type,
                propertyName: null);

            // Assert
            Assert.True(modelMetadata.IsComplexType);
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(NonCollectionType))]
        [InlineData(typeof(string))]
        public void IsCollectionType_NonCollectionTypes(Type type)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act
            var modelMetadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: null,
                modelType: type,
                propertyName: null);

            // Assert
            Assert.False(modelMetadata.IsCollectionType);
        }

        [Theory]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(List<string>))]
        [InlineData(typeof(DerivedList))]
        [InlineData(typeof(IEnumerable))]
        [InlineData(typeof(IEnumerable<string>))]
        [InlineData(typeof(Collection<int>))]
        [InlineData(typeof(Dictionary<object, object>))]
        public void IsCollectionType_CollectionTypes(Type type)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act
            var modelMetadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: null,
                modelType: type,
                propertyName: null);

            // Assert
            Assert.True(modelMetadata.IsCollectionType);
        }

        private class NonCollectionType
        {
        }

        private class DerivedList : List<int>
        {
        }

        // IsNullableValueType

        [Fact]
        public void IsNullableValueTypeTests()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();

            // Act & Assert
            Assert.False(new ModelMetadata(provider, null, null, typeof(string), null).IsNullableValueType);
            Assert.False(new ModelMetadata(provider, null, null, typeof(IDisposable), null).IsNullableValueType);
            Assert.True(new ModelMetadata(provider, null, null, typeof(Nullable<int>), null).IsNullableValueType);
            Assert.False(new ModelMetadata(provider, null, null, typeof(int), null).IsNullableValueType);
        }

        // IsRequired

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(IDisposable))]
        [InlineData(typeof(Nullable<int>))]
        public void IsRequired_ReturnsFalse_ForNullableTypes(Type modelType)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(provider,
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
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(provider,
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
        public void PropertiesProperty_CallsProvider()
        {
            // Arrange
            var modelType = typeof(object);
            var provider = new PropertiesModelMetadataProvider(new List<string>());
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: null,
                modelType: modelType,
                propertyName: null);

            // Act
            var result = metadata.Properties;

            // Assert
            Assert.Empty(result);
            Assert.Equal(1, provider.GetMetadataForPropertiesCalls);
        }

        // Input (original) property names and expected (ordered) property names.
        public static TheoryData<IEnumerable<string>, IEnumerable<string>> PropertyNamesTheoryData
        {
            get
            {
                // ModelMetadata does not reorder properties the provider returns without an Order override.
                return new TheoryData<IEnumerable<string>, IEnumerable<string>>
                {
                    {
                        new List<string> { "Property1", "Property2", "Property3", "Property4", },
                        new List<string> { "Property1", "Property2", "Property3", "Property4", }
                    },
                    {
                        new List<string> { "Property4", "Property3", "Property2", "Property1", },
                        new List<string> { "Property4", "Property3", "Property2", "Property1", }
                    },
                    {
                        new List<string> { "Delta", "Bravo", "Charlie", "Alpha", },
                        new List<string> { "Delta", "Bravo", "Charlie", "Alpha", }
                    },
                    {
                        new List<string> { "John", "Jonathan", "Jon", "Joan", },
                        new List<string> { "John", "Jonathan", "Jon", "Joan", }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(PropertyNamesTheoryData))]
        public void PropertiesProperty_WithDefaultOrder_OrdersPropertyNamesAsProvided(
            IEnumerable<string> originalNames,
            IEnumerable<string> expectedNames)
        {
            // Arrange
            var modelType = typeof(object);
            var provider = new PropertiesModelMetadataProvider(originalNames);
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: null,
                modelType: modelType,
                propertyName: null);

            // Act
            var result = metadata.Properties;

            // Assert
            var propertyNames = result.Select(property => property.PropertyName);
            Assert.Equal(expectedNames, propertyNames);
        }

        // Input (original) property names, Order values, and expected (ordered) property names.
        public static TheoryData<IEnumerable<KeyValuePair<string, int>>, IEnumerable<string>>
            PropertyNamesAndOrdersTheoryData
        {
            get
            {
                return new TheoryData<IEnumerable<KeyValuePair<string, int>>, IEnumerable<string>>
                {
                    {
                        new List<KeyValuePair<string, int>>
                        {
                            new KeyValuePair<string, int>("Property1", 23),
                            new KeyValuePair<string, int>("Property2", 23),
                            new KeyValuePair<string, int>("Property3", 23),
                            new KeyValuePair<string, int>("Property4", 23),
                        },
                        new List<string> { "Property1", "Property2", "Property3", "Property4", }
                    },
                    // Same order if already ordered using Order.
                    {
                        new List<KeyValuePair<string, int>>
                        {
                            new KeyValuePair<string, int>("Property4", 23),
                            new KeyValuePair<string, int>("Property3", 24),
                            new KeyValuePair<string, int>("Property2", 25),
                            new KeyValuePair<string, int>("Property1", 26),
                        },
                        new List<string> { "Property4", "Property3", "Property2", "Property1", }
                    },
                    // Rest of the orderings get updated within ModelMetadata.
                    {
                        new List<KeyValuePair<string, int>>
                        {
                            new KeyValuePair<string, int>("Property1", 26),
                            new KeyValuePair<string, int>("Property2", 25),
                            new KeyValuePair<string, int>("Property3", 24),
                            new KeyValuePair<string, int>("Property4", 23),
                        },
                        new List<string> { "Property4", "Property3", "Property2", "Property1", }
                    },
                    {
                        new List<KeyValuePair<string, int>>
                        {
                            new KeyValuePair<string, int>("Alpha", 26),
                            new KeyValuePair<string, int>("Bravo", 24),
                            new KeyValuePair<string, int>("Charlie", 23),
                            new KeyValuePair<string, int>("Delta", 25),
                        },
                        new List<string> { "Charlie", "Bravo", "Delta", "Alpha", }
                    },
                    // Jonathan and Jon will not be reordered.
                    {
                        new List<KeyValuePair<string, int>>
                        {
                            new KeyValuePair<string, int>("Joan", 1),
                            new KeyValuePair<string, int>("Jonathan", 0),
                            new KeyValuePair<string, int>("Jon", 0),
                            new KeyValuePair<string, int>("John", -1),
                        },
                        new List<string> { "John", "Jonathan", "Jon", "Joan", }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(PropertyNamesAndOrdersTheoryData))]
        public void PropertiesProperty_OrdersPropertyNamesUsingOrder_ThenAsProvided(
            IEnumerable<KeyValuePair<string, int>> originalNamesAndOrders,
            IEnumerable<string> expectedNames)
        {
            // Arrange
            var modelType = typeof(object);
            var provider = new PropertiesModelMetadataProvider(originalNamesAndOrders);
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: null,
                modelType: modelType,
                propertyName: null);

            // Act
            var result = metadata.Properties;

            // Assert
            var propertyNames = result.Select(property => property.PropertyName);
            Assert.Equal(expectedNames, propertyNames);
        }

        [Theory]
        [MemberData(nameof(MetadataModifierData))]
        public void PropertiesPropertyChangesPersist(
            Action<ModelMetadata> setter,
            Func<ModelMetadata, object> getter,
            object expected)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: () => new Class1(),
                modelType: typeof(Class1),
                propertyName: null);

            // Act
            foreach (var property in metadata.Properties)
            {
                setter(property);
            }

            // Assert
            foreach (var property in metadata.Properties)
            {
                // Due to boxing of structs, can't Assert.Same().
                Assert.Equal(expected, getter(property));
            }
        }

        [Theory]
        [MemberData(nameof(MetadataModifierData))]
        public void PropertyChangesPersist(
            Action<ModelMetadata> setter,
            Func<ModelMetadata, object> getter,
            object expected)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: () => new Class1(),
                modelType: typeof(Class1),
                propertyName: null);

            // Act
            setter(metadata);
            var result = getter(metadata);

            // Assert
            // Due to boxing of structs, can't Assert.Same().
            Assert.Equal(expected, result);
        }

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

        [Fact]
        public void PropertiesSetOnce()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: () => new Class1(),
                modelType: typeof(Class1),
                propertyName: null);

            // Act
            var firstPropertiesEvaluation = metadata.Properties;
            var secondPropertiesEvaluation = metadata.Properties;

            // Assert
            // Same IEnumerable<ModelMetadata> object.
            Assert.Same(firstPropertiesEvaluation, secondPropertiesEvaluation);
        }

        [Fact]
        public void PropertiesEnumerationEvaluatedOnce()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(
                provider,
                containerType: null,
                modelAccessor: () => new Class1(),
                modelType: typeof(Class1),
                propertyName: null);

            // Act
            var firstPropertiesEvaluation = metadata.Properties.ToList();
            var secondPropertiesEvaluation = metadata.Properties.ToList();

            // Assert
            // Identical ModelMetadata objects every time we run through the Properties collection.
            Assert.Equal(firstPropertiesEvaluation, secondPropertiesEvaluation);
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

        [Fact]
        public void ReturnsPropertyNameWhenSetAndDisplayNameIsNull()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(provider, null, null, typeof(object), "PropertyName");

            // Act
            var result = metadata.GetDisplayName();

            // Assert
            Assert.Equal("PropertyName", result);
        }

        [Fact]
        public void ReturnsTypeNameWhenPropertyNameAndDisplayNameAreNull()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var metadata = new ModelMetadata(provider, null, null, typeof(object), null);

            // Act
            var result = metadata.GetDisplayName();

            // Assert
            Assert.Equal("Object", result);
        }

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
        [MemberData(nameof(SimpleDisplayTextData))]
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

        private class DummyPropertyBindingPredicateProvider : IPropertyBindingPredicateProvider
        {
            public Func<ModelBindingContext, string, bool> PropertyFilter { get; set; }
        }

        // Gives object type properties with provided names or names and Order values.
        private class PropertiesModelMetadataProvider : IModelMetadataProvider
        {
            private List<ModelMetadata> _properties = new List<ModelMetadata>();

            public PropertiesModelMetadataProvider(IEnumerable<string> propertyNames)
            {
                foreach (var propertyName in propertyNames)
                {
                    var metadata = new ModelMetadata(
                        this,
                        containerType: typeof(DummyContactModel),
                        modelAccessor: null,
                        modelType: typeof(string),
                        propertyName: propertyName);

                    _properties.Add(metadata);
                }
            }

            public PropertiesModelMetadataProvider(IEnumerable<KeyValuePair<string, int>> propertyNamesAndOrders)
            {
                foreach (var keyValuePair in propertyNamesAndOrders)
                {
                    var metadata = new ModelMetadata(
                        this,
                        containerType: typeof(DummyContactModel),
                        modelAccessor: null,
                        modelType: typeof(string),
                        propertyName: keyValuePair.Key)
                    {
                        Order = keyValuePair.Value,
                    };

                    _properties.Add(metadata);
                }
            }

            public int GetMetadataForPropertiesCalls { get; private set; }

            public ModelMetadata GetMetadataForParameter(
                Func<object> modelAccessor,
                [NotNull] MethodInfo methodInfo,
                [NotNull] string parameterName)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ModelMetadata> GetMetadataForProperties(object container, [NotNull] Type containerType)
            {
                Assert.Null(container);
                Assert.Equal(typeof(object), containerType);
                GetMetadataForPropertiesCalls++;

                return _properties;
            }

            public ModelMetadata GetMetadataForProperty(
                Func<object> modelAccessor,
                [NotNull] Type containerType,
                [NotNull] string propertyName)
            {
                throw new NotImplementedException();
            }

            public ModelMetadata GetMetadataForType(Func<object> modelAccessor, [NotNull] Type modelType)
            {
                throw new NotImplementedException();
            }
        }
    }
}
