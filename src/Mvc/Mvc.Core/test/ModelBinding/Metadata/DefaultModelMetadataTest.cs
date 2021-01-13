// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml;
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

            var key = ModelMetadataIdentity.ForProperty(typeof(Exception).GetProperty(nameof(Exception.Message)), typeof(string), typeof(Exception));
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], new object[0], null));

            // Act
            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Assert
            Assert.Equal(typeof(string), metadata.ModelType);
            Assert.Equal("Message", metadata.PropertyName);
            Assert.Equal(typeof(Exception), metadata.ContainerType);
        }

        [Fact]
        public void DisplayFormatString_DoesNotCacheInitialDelegateValue()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForProperty(
                typeof(TypeWithProperties).GetProperty(nameof(TypeWithProperties.PublicGetPublicSetProperty)),
                typeof(string),
                typeof(TypeWithProperties));

            var attributes = new ModelAttributes(Array.Empty<object>(), Array.Empty<object>(), null);
            var displayFormat = "initial format";
            var cache = new DefaultMetadataDetails(key, attributes)
            {
                DisplayMetadata = new DisplayMetadata
                {
                    DisplayFormatStringProvider = () => displayFormat,
                },
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            foreach (var newFormat in new[] { "one", "two", "three" })
            {
                // Arrange n
                displayFormat = newFormat;

                // Act n
                var result = metadata.DisplayFormatString;

                // Assert n
                Assert.Equal(newFormat, result);
            }
        }

        [Fact]
        public void EditFormatString_DoesNotCacheInitialDelegateValue()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForProperty(
                typeof(TypeWithProperties).GetProperty(nameof(TypeWithProperties.PublicGetPublicSetProperty)),
                typeof(string),
                typeof(TypeWithProperties));

            var attributes = new ModelAttributes(Array.Empty<object>(), Array.Empty<object>(), null);
            var editFormat = "initial format";
            var cache = new DefaultMetadataDetails(key, attributes)
            {
                DisplayMetadata = new DisplayMetadata
                {
                    EditFormatStringProvider = () => editFormat,
                },
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            foreach (var newFormat in new[] { "one", "two", "three" })
            {
                // Arrange n
                editFormat = newFormat;

                // Act n
                var result = metadata.EditFormatString;

                // Assert n
                Assert.Equal(newFormat, result);
            }
        }

        [Fact]
        public void NullDisplayText_DoesNotCacheInitialDelegateValue()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForProperty(
                typeof(TypeWithProperties).GetProperty(nameof(TypeWithProperties.PublicGetPublicSetProperty)), 
                typeof(string),
                typeof(TypeWithProperties));

            var attributes = new ModelAttributes(Array.Empty<object>(), Array.Empty<object>(), null);
            var nullDisplay = "initial display text";
            var cache = new DefaultMetadataDetails(key, attributes)
            {
                DisplayMetadata = new DisplayMetadata
                {
                    NullDisplayTextProvider = () => nullDisplay,
                },
            };

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            foreach (var newDisplay in new[] { "one", "two", "three" })
            {
                // Arrange n
                nullDisplay = newDisplay;

                // Act n
                var result = metadata.NullDisplayText;

                // Assert n
                Assert.Equal(newDisplay, result);
            }
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
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null))
            {
                BindingMetadata = new BindingMetadata()
                {
                    IsBindingAllowed = false, // Will be ignored.
                },
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
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null))
            {
                BindingMetadata = new BindingMetadata()
                {
                    IsBindingRequired = true, // Will be ignored.
                },
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

            var prop1 = typeof(Exception).GetProperty(nameof(Exception.Message));
            var prop2 = typeof(Exception).GetProperty(nameof(Exception.StackTrace));

            var expectedProperties = new DefaultModelMetadata[]
            {
                new DefaultModelMetadata(
                    provider.Object,
                    detailsProvider,
                    new DefaultMetadataDetails(
                        ModelMetadataIdentity.ForProperty(prop1, typeof(int), typeof(string)),
                        attributes: new ModelAttributes(new object[0], new object[0], null))),
                new DefaultModelMetadata(
                    provider.Object,
                    detailsProvider,
                    new DefaultMetadataDetails(
                        ModelMetadataIdentity.ForProperty(prop2, typeof(int), typeof(string)),
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
#pragma warning disable CS0618 // Using the obsolete overload does not affect the intent of this test, but fixing it requires a lot of code churn.
                        ModelMetadataIdentity.ForProperty(typeof(int), originalName, typeof(string)),
#pragma warning restore CS0618 // Type or member is obsolete
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
#pragma warning disable CS0618 // Using the obsolete overload does not affect the intent of this test, but fixing it requires a lot of code churn.
                        ModelMetadataIdentity.ForProperty(typeof(int), kvp.Key, typeof(string)),
#pragma warning restore CS0618 // Type or member is obsolete
                        attributes: new ModelAttributes(new object[0], new object[0], null))
                {
                    DisplayMetadata = new DisplayMetadata(),
                };

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
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null))
            {
                BindingMetadata = new BindingMetadata()
                {
                    IsReadOnly = true, // Will be ignored.
                },
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

            var key = ModelMetadataIdentity.ForType(modelType);
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
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null))
            {
                ValidationMetadata = new ValidationMetadata
                {
                    PropertyValidationFilter = value,
                },
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
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null))
            {
                ValidationMetadata = new ValidationMetadata()
                {
                    ValidateChildren = true,
                },
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
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null))
            {
                ValidationMetadata = new ValidationMetadata()
                {
                    ValidateChildren = false,
                },
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

        [Fact]
        public void CalculateHasValidators_ParameterMetadata_TypeHasNoValidators()
        {
            // Arrange
            var parameter = GetType()
                .GetMethod(nameof(CalculateHasValidators_ParameterMetadata_TypeHasNoValidatorsMethod), BindingFlags.Static | BindingFlags.NonPublic)
                .GetParameters()[0];
            var modelIdentity = ModelMetadataIdentity.ForParameter(parameter);
            var modelMetadata = CreateModelMetadata(modelIdentity, Mock.Of<IModelMetadataProvider>(), false);

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.False(result);
        }

        private static void CalculateHasValidators_ParameterMetadata_TypeHasNoValidatorsMethod(string model) { }

        [Fact]
        public void CalculateHasValidators_PropertyMetadata_TypeHasNoValidators()
        {
            // Arrange
            var property = GetType()
                .GetProperty(nameof(CalculateHasValidators_PropertyMetadata_TypeHasNoValidatorsProperty), BindingFlags.Static | BindingFlags.NonPublic);
            var modelIdentity = ModelMetadataIdentity.ForProperty(property, property.PropertyType, GetType());
            var modelMetadata = CreateModelMetadata(modelIdentity, Mock.Of<IModelMetadataProvider>(), false);

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.False(result);
        }

        private static int CalculateHasValidators_PropertyMetadata_TypeHasNoValidatorsProperty { get; set; }

        [Fact]
        public void CalculateHasValidators_TypeWithoutProperties_TypeHasNoValidators()
        {
            // Arrange
            var modelIdentity = ModelMetadataIdentity.ForType(typeof(string));
            var modelMetadata = CreateModelMetadata(modelIdentity, Mock.Of<IModelMetadataProvider>(), false);

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CalculateHasValidators_SimpleType_TypeHasValidators()
        {
            // Arrange
            var modelIdentity = ModelMetadataIdentity.ForType(typeof(string));
            var modelMetadata = CreateModelMetadata(modelIdentity, Mock.Of<IModelMetadataProvider>(), true);

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CalculateHasValidators_ReturnsTrue_SimpleType_TypeHasNonDeterministicValidators()
        {
            // Arrange
            var modelIdentity = ModelMetadataIdentity.ForType(typeof(string));
            var modelMetadata = CreateModelMetadata(modelIdentity, Mock.Of<IModelMetadataProvider>(), null);

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CalculateHasValidators_TypeWithProperties_PropertyIsNotDefaultModelMetadata()
        {
            // Arrange
            var modelType = typeof(TypeWithProperties);
            var modelIdentity = ModelMetadataIdentity.ForType(modelType);
            var metadataProvider = new Mock<IModelMetadataProvider>();
            var modelMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);

            var property = typeof(TypeWithProperties).GetProperty(nameof(TypeWithProperties.PublicGetPublicSetProperty));
            var propertyIdentity = ModelMetadataIdentity.ForProperty(property, typeof(int), typeof(TypeWithProperties));
            var propertyMetadata = new Mock<ModelMetadata>(propertyIdentity);

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(modelType))
                .Returns(new[] { propertyMetadata.Object, })
                .Verifiable();

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CalculateHasValidators_TypeWithProperties_HasValidatorForAnyPropertyIsTrue()
        {
            // Arrange
            var modelType = typeof(TypeWithProperties);
            var modelIdentity = ModelMetadataIdentity.ForType(modelType);
            var metadataProvider = new Mock<IModelMetadataProvider>();
            var modelMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);

            var property1Identity = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(TypeWithProperties.PublicGetPublicSetProperty)), typeof(int), modelType);
            var property1Metadata = CreateModelMetadata(property1Identity, metadataProvider.Object, false);

            var property2Identity = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(TypeWithProperties.PublicGetProtectedSetProperty)), typeof(int), modelType);
            var property2Metadata = CreateModelMetadata(property2Identity, metadataProvider.Object, true);

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(modelType))
                .Returns(new[] { property1Metadata, property2Metadata })
                .Verifiable();

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CalculateHasValidators_TypeWithProperties_HasValidatorsForPropertyIsNotDeterminstic()
        {
            // Arrange
            var modelType = typeof(TypeWithProperties);
            var modelIdentity = ModelMetadataIdentity.ForType(modelType);
            var metadataProvider = new Mock<IModelMetadataProvider>();
            var modelMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);

            var propertyIdentity = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(TypeWithProperties.PublicGetPublicSetProperty)), typeof(int), modelType);
            var propertyMetadata = CreateModelMetadata(propertyIdentity, metadataProvider.Object, null);

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(modelType))
                .Returns(new[] { propertyMetadata, })
                .Verifiable();

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CalculateHasValidators_TypeWithProperties_HasValidatorForAllPropertiesIsFalse()
        {
            // Arrange
            var modelType = typeof(TypeWithProperties);
            var modelIdentity = ModelMetadataIdentity.ForType(modelType);
            var metadataProvider = new Mock<IModelMetadataProvider>();
            var modelMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);

            var property1Identity = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(TypeWithProperties.PublicGetPublicSetProperty)), typeof(int), modelType);
            var property1Metadata = CreateModelMetadata(property1Identity, metadataProvider.Object, false);

            var property2Identity = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(TypeWithProperties.PublicGetProtectedSetProperty)), typeof(int), modelType);
            var property2Metadata = CreateModelMetadata(property2Identity, metadataProvider.Object, false);

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(modelType))
                .Returns(new[] { property1Metadata, property2Metadata })
                .Verifiable();

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CalculateHasValidators_SelfReferencingType_HasValidatorOnNestedProperty()
        {
            // Arrange
            var modelType = typeof(Employee);
            var modelIdentity = ModelMetadataIdentity.ForType(modelType);
            var metadataProvider = new Mock<IModelMetadataProvider>();
            var modelMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);

            var employeeId = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Id)), typeof(int), modelType);
            var employeeIdMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);
            var employeeUnit = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Unit)), typeof(BusinessUnit), modelType);
            var employeeUnitMetadata = CreateModelMetadata(employeeUnit, metadataProvider.Object, false);
            var employeeManager = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Manager)), typeof(Employee), modelType);
            var employeeManagerMetadata = CreateModelMetadata(employeeManager, metadataProvider.Object, false);
            var employeeEmployees = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Employees)), typeof(List<Employee>), modelType);
            var employeeEmployeesMetadata = CreateModelMetadata(employeeEmployees, metadataProvider.Object, false);

            var unitModel = typeof(BusinessUnit);
            var unitHead = ModelMetadataIdentity.ForProperty(unitModel.GetProperty(nameof(BusinessUnit.Head)), typeof(Employee), unitModel);
            var unitHeadMetadata = CreateModelMetadata(unitHead, metadataProvider.Object, false);
            var unitId = ModelMetadataIdentity.ForProperty(unitModel.GetProperty(nameof(BusinessUnit.Id)), typeof(int), unitModel);
            var unitIdMetadata = CreateModelMetadata(unitId, metadataProvider.Object, true); // BusinessUnit.Id has validators.

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(modelType))
                .Returns(new[] { employeeIdMetadata, employeeUnitMetadata, employeeManagerMetadata, employeeEmployeesMetadata, })
                .Verifiable();

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(typeof(BusinessUnit)))
                .Returns(new[] { unitHeadMetadata, unitIdMetadata, })
                .Verifiable();

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CalculateHasValidators_SelfReferencingType_HasValidatorOnSelfReferencedProperty()
        {
            // Arrange
            var modelType = typeof(Employee);
            var modelIdentity = ModelMetadataIdentity.ForType(modelType);
            var metadataProvider = new Mock<IModelMetadataProvider>();
            var modelMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);

            var employeeId = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Id)), typeof(int), modelType);
            var employeeIdMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);
            var employeeUnit = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Unit)), typeof(BusinessUnit), modelType);
            var employeeUnitMetadata = CreateModelMetadata(employeeUnit, metadataProvider.Object, false);
            var employeeManager = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Manager)), typeof(Employee), modelType);
            var employeeManagerMetadata = CreateModelMetadata(employeeManager, metadataProvider.Object, false);
            var employeeEmployees = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Employees)), typeof(List<Employee>), modelType);
            var employeeEmployeesMetadata = CreateModelMetadata(employeeEmployees, metadataProvider.Object, false);

            var unitModel = typeof(BusinessUnit);
            var unitHead = ModelMetadataIdentity.ForProperty(unitModel.GetProperty(nameof(BusinessUnit.Head)), typeof(Employee), unitModel);
            var unitHeadMetadata = CreateModelMetadata(unitHead, metadataProvider.Object, true); // BusinessUnit.Head has validators
            var unitId = ModelMetadataIdentity.ForProperty(unitModel.GetProperty(nameof(BusinessUnit.Id)), typeof(int), unitModel);
            var unitIdMetadata = CreateModelMetadata(unitId, metadataProvider.Object, false); 

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(modelType))
                .Returns(new[] { employeeIdMetadata, employeeUnitMetadata, employeeManagerMetadata, employeeEmployeesMetadata, });

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(typeof(BusinessUnit)))
                .Returns(new[] { unitHeadMetadata, unitIdMetadata, });

            metadataProvider
                .Setup(mp => mp.GetMetadataForType(modelType))
                .Returns(modelMetadata);

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CalculateHasValidators_CollectionElementHasValidators()
        {
            // Arrange
            var modelType = typeof(Employee);
            var modelIdentity = ModelMetadataIdentity.ForType(modelType);
            var metadataProvider = new Mock<IModelMetadataProvider>();
            var modelMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);

            var employeeId = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Id)), typeof(int), modelType);
            var employeeIdMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);
            var employeeEmployees = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Employees)), typeof(List<Employee>), modelType);
            var employeeEmployeesMetadata = CreateModelMetadata(employeeEmployees, metadataProvider.Object, false);

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(modelType))
                .Returns(new[] { employeeIdMetadata, employeeEmployeesMetadata, });

            metadataProvider
                .Setup(mp => mp.GetMetadataForType(modelType))
                .Returns(CreateModelMetadata(modelIdentity, metadataProvider.Object, true)); // Employees.Employee has validators

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CalculateHasValidators_SelfReferencingType_NoValidatorsInGraph()
        {
            // Arrange
            var modelType = typeof(Employee);
            var modelIdentity = ModelMetadataIdentity.ForType(modelType);
            var metadataProvider = new Mock<IModelMetadataProvider>();
            var modelMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);

            var employeeId = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Id)), typeof(int), modelType);
            var employeeIdMetadata = CreateModelMetadata(modelIdentity, metadataProvider.Object, false);
            var employeeUnit = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Unit)), typeof(BusinessUnit), modelType);
            var employeeUnitMetadata = CreateModelMetadata(employeeUnit, metadataProvider.Object, false);
            var employeeManager = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Manager)), typeof(Employee), modelType);
            var employeeManagerMetadata = CreateModelMetadata(employeeManager, metadataProvider.Object, false);
            var employeeEmployeesId = ModelMetadataIdentity.ForProperty(modelType.GetProperty(nameof(Employee.Employees)), typeof(List<Employee>), modelType);
            var employeeEmployeesIdMetadata = CreateModelMetadata(employeeEmployeesId, metadataProvider.Object, false);

            var unitModel = typeof(BusinessUnit);
            var unitHead = ModelMetadataIdentity.ForProperty(unitModel.GetProperty(nameof(BusinessUnit.Head)), typeof(Employee), unitModel);
            var unitHeadMetadata = CreateModelMetadata(unitHead, metadataProvider.Object, false);
            var unitId = ModelMetadataIdentity.ForProperty(unitModel.GetProperty(nameof(BusinessUnit.Id)), typeof(int), unitModel);
            var unitIdMetadata = CreateModelMetadata(unitId, metadataProvider.Object, false);

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(modelType))
                .Returns(new[] { employeeIdMetadata, employeeUnitMetadata, employeeManagerMetadata, employeeEmployeesIdMetadata, });

            metadataProvider
                .Setup(mp => mp.GetMetadataForProperties(typeof(BusinessUnit)))
                .Returns(new[] { unitHeadMetadata, unitIdMetadata, });

            metadataProvider
                .Setup(mp => mp.GetMetadataForType(modelType))
                .Returns(modelMetadata);

            // Act
            var result = DefaultModelMetadata.CalculateHasValidators(new HashSet<DefaultModelMetadata>(), modelMetadata);

            // Assert
            Assert.False(result);
        }

        private static DefaultModelMetadata CreateModelMetadata(
            ModelMetadataIdentity modelIdentity, 
            IModelMetadataProvider metadataProvider,
            bool? hasValidators)
        {
            return new DefaultModelMetadata(
                metadataProvider,
                new SetHasValidatorsCompositeMetadataDetailsProvider { HasValidators = hasValidators },
                new DefaultMetadataDetails(modelIdentity, new ModelAttributes(new object[0], new object[0], new object[0])));
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

        public class Employee
        {
            public int Id { get; set; }

            public BusinessUnit Unit { get; set; }

            public Employee Manager { get; set; }

            public List<Employee> Employees { get; set; }
        }

        public class BusinessUnit
        {
            public Employee Head { get; set; }

            public int Id { get; set; }
        }

        private class SetHasValidatorsCompositeMetadataDetailsProvider : ICompositeMetadataDetailsProvider
        {
            public bool? HasValidators { get; set; }

            public void CreateBindingMetadata(BindingMetadataProviderContext context)
            {
            }

            public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
            {
            }

            public void CreateValidationMetadata(ValidationMetadataProviderContext context)
            {
                context.ValidationMetadata.HasValidators = HasValidators;
            }
        }
    }
}
