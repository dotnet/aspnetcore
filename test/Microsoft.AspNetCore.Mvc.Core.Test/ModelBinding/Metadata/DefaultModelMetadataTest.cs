// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    public class DefaultModelMetadataTest
    {
        [Fact]
        public void DefaultValues()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new DefaultCompositeMetadataDetailsProvider(
                Enumerable.Empty<IMetadataDetailsProvider>());

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            // Act
            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Assert
            Assert.NotNull(metadata.AdditionalValues);
            Assert.Empty(metadata.AdditionalValues);
            Assert.Equal(typeof(string), metadata.ModelType);

            Assert.True(metadata.ConvertEmptyStringToNull);
            Assert.False(metadata.HasNonDefaultEditFormat);
            Assert.False(metadata.HideSurroundingHtml);
            Assert.True(metadata.HtmlEncode);
            Assert.True(metadata.IsBindingAllowed);
            Assert.False(metadata.IsBindingRequired);
            Assert.False(metadata.IsCollectionType);
            Assert.False(metadata.IsComplexType);
            Assert.False(metadata.IsEnumerableType);
            Assert.False(metadata.IsEnum);
            Assert.False(metadata.IsFlagsEnum);
            Assert.False(metadata.IsNullableValueType);
            Assert.False(metadata.IsReadOnly);
            Assert.False(metadata.IsRequired); // Defaults to false for reference types
            Assert.True(metadata.ShowForDisplay);
            Assert.True(metadata.ShowForEdit);
            Assert.False(metadata.ValidateChildren); // Defaults to true for complex and enumerable types.

            Assert.Null(metadata.DataTypeName);
            Assert.Null(metadata.Description);
            Assert.Null(metadata.DisplayFormatString);
            Assert.Null(metadata.DisplayName);
            Assert.Null(metadata.EditFormatString);
            Assert.Null(metadata.ElementMetadata);
            Assert.Null(metadata.EnumGroupedDisplayNamesAndValues);
            Assert.Null(metadata.EnumNamesAndValues);
            Assert.Null(metadata.NullDisplayText);
            Assert.Null(metadata.PropertyValidationFilter);
            Assert.Null(metadata.SimpleDisplayProperty);
            Assert.Null(metadata.Placeholder);
            Assert.Null(metadata.TemplateHint);

            Assert.Equal(10000, ModelMetadata.DefaultOrder);
            Assert.Equal(ModelMetadata.DefaultOrder, metadata.Order);

            Assert.Null(metadata.BinderModelName);
            Assert.Null(metadata.BinderType);
            Assert.Null(metadata.BindingSource);
            Assert.Null(metadata.PropertyFilterProvider);
        }

        [Fact]
        public void CreateMetadataForType()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new DefaultCompositeMetadataDetailsProvider(
                Enumerable.Empty<IMetadataDetailsProvider>());

            var key = ModelMetadataIdentity.ForType(typeof(Exception));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            // Act
            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Assert
            Assert.Equal(typeof(Exception), metadata.ModelType);
            Assert.Null(metadata.PropertyName);
            Assert.Null(metadata.ContainerType);
        }

        [Fact]
        public void CreateMetadataForProperty()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForProperty(typeof(string), "Message", typeof(Exception));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], new object[0], null));

            // Act
            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Assert
            Assert.Equal(typeof(string), metadata.ModelType);
            Assert.Equal("Message", metadata.PropertyName);
            Assert.Equal(typeof(Exception), metadata.ContainerType);
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(NonCollectionType))]
        [InlineData(typeof(string))]
        public void ElementMetadata_ReturnsNull_ForNonCollections(Type modelType)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(modelType);
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var elementMetadata = metadata.ElementMetadata;

            // Assert
            Assert.Null(elementMetadata);
        }

        [Theory]
        [InlineData(typeof(int[]), typeof(int))]
        [InlineData(typeof(List<string>), typeof(string))]
        [InlineData(typeof(DerivedList), typeof(int))]
        [InlineData(typeof(IEnumerable), typeof(object))]
        [InlineData(typeof(IEnumerable<string>), typeof(string))]
        [InlineData(typeof(Collection<int>), typeof(int))]
        [InlineData(typeof(Dictionary<object, object>), typeof(KeyValuePair<object, object>))]
        [InlineData(typeof(DerivedDictionary), typeof(KeyValuePair<string, int>))]
        public void ElementMetadata_ReturnsExpectedMetadata(Type modelType, Type elementType)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(modelType);
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var elementMetadata = metadata.ElementMetadata;

            // Assert
            Assert.NotNull(elementMetadata);
            Assert.Equal(elementType, elementMetadata.ModelType);
        }

        private class NonCollectionType
        {
        }

        private class DerivedList : List<int>
        {
        }

        private class DerivedDictionary : Dictionary<string, int>
        {
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        public void IsBindingAllowed_ReturnsTrue_ForTypes(Type modelType)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(modelType);
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));
            cache.BindingMetadata = new BindingMetadata()
            {
                IsBindingAllowed = false, // Will be ignored.
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var isBindingAllowed = metadata.IsBindingAllowed;

            // Assert
            Assert.True(isBindingAllowed);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        public void IsBindingRequired_ReturnsFalse_ForTypes(Type modelType)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(modelType);
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));
            cache.BindingMetadata = new BindingMetadata()
            {
                IsBindingRequired = true, // Will be ignored.
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var isBindingRequired = metadata.IsBindingRequired;

            // Assert
            Assert.False(isBindingRequired);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(IDisposable))]
        [InlineData(typeof(Nullable<int>))]
        public void IsRequired_ReturnsFalse_ForNullableTypes(Type modelType)
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(modelType);
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

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
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(modelType);
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var isRequired = metadata.IsRequired;

            // Assert
            Assert.True(isRequired);
        }

        [Fact]
        public void PropertiesProperty_CallsProvider()
        {
            // Arrange
            var provider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var expectedProperties = new DefaultModelMetadata[]
            {
                new DefaultModelMetadata(
                    provider.Object,
                    detailsProvider,
                    new DefaultMetadataDetails(
                        ModelMetadataIdentity.ForProperty(typeof(int), "Prop1", typeof(string)),
                        attributes: new ModelAttributes(new object[0], new object[0], null))),
                new DefaultModelMetadata(
                    provider.Object,
                    detailsProvider,
                    new DefaultMetadataDetails(
                        ModelMetadataIdentity.ForProperty(typeof(int), "Prop2", typeof(string)),
                        attributes: new ModelAttributes(new object[0], new object[0], null))),
            };

            provider
                .Setup(p => p.GetMetadataForProperties(typeof(string)))
                .Returns(expectedProperties);

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider.Object, detailsProvider, cache);

            // Act
            var properties = metadata.Properties;

            // Assert
            Assert.Equal(expectedProperties.Length, properties.Count);

            for (var i = 0; i < expectedProperties.Length; i++)
            {
                Assert.Same(expectedProperties[i], properties[i]);
            }
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
            var provider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var expectedProperties = new List<DefaultModelMetadata>();
            foreach (var originalName in originalNames)
            {
                expectedProperties.Add(new DefaultModelMetadata(
                    provider.Object,
                    detailsProvider,
                    new DefaultMetadataDetails(
                        ModelMetadataIdentity.ForProperty(typeof(int), originalName, typeof(string)),
                        attributes: new ModelAttributes(new object[0], new object[0], null))));
            }

            provider
                .Setup(p => p.GetMetadataForProperties(typeof(string)))
                .Returns(expectedProperties);

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider.Object, detailsProvider, cache);

            // Act
            var properties = metadata.Properties;

            // Assert
            Assert.Equal(expectedNames.Count(), properties.Count);
            Assert.Equal(expectedNames.ToArray(), properties.Select(p => p.PropertyName).ToArray());
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
            var provider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var expectedProperties = new List<DefaultModelMetadata>();
            foreach (var kvp in originalNamesAndOrders)
            {
                var propertyCache = new DefaultMetadataDetails(
                        ModelMetadataIdentity.ForProperty(typeof(int), kvp.Key, typeof(string)),
                        attributes: new ModelAttributes(new object[0], new object[0], null));

                propertyCache.DisplayMetadata = new DisplayMetadata();
                propertyCache.DisplayMetadata.Order = kvp.Value;

                expectedProperties.Add(new DefaultModelMetadata(
                    provider.Object,
                    detailsProvider,
                    propertyCache));
            }

            provider
                .Setup(p => p.GetMetadataForProperties(typeof(string)))
                .Returns(expectedProperties);

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider.Object, detailsProvider, cache);

            // Act
            var properties = metadata.Properties;

            // Assert
            Assert.Equal(expectedNames.Count(), properties.Count);
            Assert.Equal(expectedNames.ToArray(), properties.Select(p => p.PropertyName).ToArray());
        }

        [Fact]
        public void PropertiesSetOnce()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

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
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var firstPropertiesEvaluation = metadata.Properties.ToList();
            var secondPropertiesEvaluation = metadata.Properties.ToList();

            // Assert
            // Identical ModelMetadata objects every time we run through the Properties collection.
            Assert.Equal(firstPropertiesEvaluation, secondPropertiesEvaluation);
        }

        [Fact]
        public void IsReadOnly_ReturnsFalse_ForType()
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(typeof(int[]));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));
            cache.BindingMetadata = new BindingMetadata()
            {
                IsReadOnly = true, // Will be ignored.
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var isReadOnly = metadata.IsReadOnly;

            // Assert
            Assert.False(isReadOnly);
        }

        [Fact]
        public void IsReadOnly_ReturnsTrue_ForPrivateSetProperty()
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(typeof(TypeWithProperties));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var isReadOnly = metadata.Properties["PublicGetPrivateSetProperty"].IsReadOnly;

            // Assert
            Assert.True(isReadOnly);
        }

        [Fact]
        public void IsReadOnly_ReturnsTrue_ForProtectedSetProperty()
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(typeof(TypeWithProperties));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var isReadOnly = metadata.Properties["PublicGetProtectedSetProperty"].IsReadOnly;

            // Assert
            Assert.True(isReadOnly);
        }

        [Fact]
        public void IsReadOnly_ReturnsFalse_ForPublicSetProperty()
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(typeof(TypeWithProperties));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var isReadOnly = metadata.Properties["PublicGetPublicSetProperty"].IsReadOnly;

            // Assert
            Assert.False(isReadOnly);
        }

        [Theory]
        [InlineData(typeof(int))] // Primitive
        [InlineData(typeof(int?))] // Nullable
        [InlineData(typeof(Guid))] // TypeConverter
        [InlineData(typeof(Guid?))] // Nullable + TypeConverter
        [InlineData(typeof(string))]
        public void ValidateChildren_SimpleTypes(Type modelType)
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(modelType);
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var validateChildren = metadata.ValidateChildren;

            // Assert
            Assert.False(validateChildren);
        }

        [Theory]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(List<decimal>))]
        [InlineData(typeof(IEnumerable))]
        [InlineData(typeof(Dictionary<string, string>))]
        [InlineData(typeof(KeyValuePair<string, string>))]
        [InlineData(typeof(KeyValuePair<string, string>?))]
        [InlineData(typeof(TypeWithProperties))]
        [InlineData(typeof(List<TypeWithProperties>))]
        public void ValidateChildren_ComplexAndEnumerableTypes(Type modelType)
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(typeof(TypeWithProperties));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var validateChildren = metadata.ValidateChildren;

            // Assert
            Assert.True(validateChildren);
        }

        public static TheoryData<IPropertyValidationFilter> ValidationFilterData
        {
            get
            {
                return new TheoryData<IPropertyValidationFilter>
                {
                    null,
                    new ValidateNeverAttribute(),
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValidationFilterData))]
        public void PropertyValidationFilter_ReflectsFilter_FromValidationMetadata(IPropertyValidationFilter value)
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(typeof(int));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));
            cache.ValidationMetadata = new ValidationMetadata
            {
                PropertyValidationFilter = value,
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var validationFilter = metadata.PropertyValidationFilter;

            // Assert
            Assert.Same(value, validationFilter);
        }

        [Fact]
        public void ValidateChildren_OverrideTrue()
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(typeof(int));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));
            cache.ValidationMetadata = new ValidationMetadata()
            {
                ValidateChildren = true,
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var validateChildren = metadata.ValidateChildren;

            // Assert
            Assert.True(validateChildren);
        }

        [Fact]
        public void ValidateChildren_OverrideFalse()
        {
            // Arrange
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();
            var provider = new DefaultModelMetadataProvider(detailsProvider);

            var key = ModelMetadataIdentity.ForType(typeof(XmlDocument));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));
            cache.ValidationMetadata = new ValidationMetadata()
            {
                ValidateChildren = false,
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var validateChildren = metadata.ValidateChildren;

            // Assert
            Assert.False(validateChildren);
        }

        [Fact]
        public void GetMetadataForType_CallsProvider()
        {
            // Arrange
            var detailsProvider = new Mock<ICompositeMetadataDetailsProvider>();
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider
                .Setup(mp => mp.GetMetadataForType(typeof(string)))
                .Verifiable();
            var metadata1 = new DefaultModelMetadata(metadataProvider.Object, detailsProvider.Object, cache);

            // Act
            var metadata2 = metadata1.GetMetadataForType(typeof(string));

            // Assert
            metadataProvider.VerifyAll();
        }

        [Fact]
        public void GetMetadataForProperties_CallsProvider()
        {
            // Arrange
            var detailsProvider = new Mock<ICompositeMetadataDetailsProvider>();
            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));
            var metadataProvider = new Mock<IModelMetadataProvider>();
            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(typeof(Exception)))
                .Verifiable();
            var metadata1 = new DefaultModelMetadata(metadataProvider.Object, detailsProvider.Object, cache);

            // Act
            var metadata2 = metadata1.GetMetadataForProperties(typeof(Exception));

            // Assert
            metadataProvider.VerifyAll();
        }

        private void ActionMethod(string input)
        {
        }

        private class TypeWithProperties
        {
            public string PublicGetPrivateSetProperty { get; private set; }

            public int PublicGetProtectedSetProperty { get; protected set; }

            public int PublicGetPublicSetProperty { get; set; }
        }
    }
}
