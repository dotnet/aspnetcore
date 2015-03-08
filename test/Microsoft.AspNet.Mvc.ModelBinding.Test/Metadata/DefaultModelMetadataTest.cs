// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if !DNXCORE50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
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
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

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
            Assert.Null(metadata.SimpleDisplayProperty);

            Assert.Equal(10000, ModelMetadata.DefaultOrder);
            Assert.Equal(ModelMetadata.DefaultOrder, metadata.Order);

            Assert.Null(metadata.BinderModelName);
            Assert.Null(metadata.BinderType);
            Assert.Null(metadata.BindingSource);
            Assert.Null(metadata.PropertyBindingPredicateProvider);
        }

        [Fact]
        public void CreateMetadataForType()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new DefaultCompositeMetadataDetailsProvider(
                Enumerable.Empty<IMetadataDetailsProvider>());

            var key = ModelMetadataIdentity.ForType(typeof(Exception));
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

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
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

            // Act
            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Assert
            Assert.Equal(typeof(string), metadata.ModelType);
            Assert.Equal("Message", metadata.PropertyName);
            Assert.Equal(typeof(Exception), metadata.ContainerType);
        }

        [Fact]
        public void CreateMetadataForParameter()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var methodInfo = GetType().GetMethod(
                "ActionMethod",
                BindingFlags.Instance | BindingFlags.NonPublic);

            var parameterInfo = methodInfo.GetParameters().Where(p => p.Name == "input").Single();

            var key = ModelMetadataIdentity.ForParameter(parameterInfo);
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

            // Act
            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            Assert.Equal(typeof(string), metadata.ModelType);
            Assert.Equal("input", metadata.PropertyName);
            Assert.Null(metadata.ContainerType);
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
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

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
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var isRequired = metadata.IsRequired;

            // Assert
            Assert.True(isRequired);
        }

#if !DNXCORE50
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
                    new DefaultMetadataDetailsCache(
                        ModelMetadataIdentity.ForProperty(typeof(int), "Prop1", typeof(string)),
                        attributes: null)),
                new DefaultModelMetadata(
                    provider.Object,
                    detailsProvider,
                    new DefaultMetadataDetailsCache(
                        ModelMetadataIdentity.ForProperty(typeof(int), "Prop2", typeof(string)),
                        attributes: null)),
            };

            provider
                .Setup(p => p.GetMetadataForProperties(typeof(string)))
                .Returns(expectedProperties);

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

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
                    new DefaultMetadataDetailsCache(
                        ModelMetadataIdentity.ForProperty(typeof(int), originalName, typeof(string)),
                        attributes: null)));
            }

            provider
                .Setup(p => p.GetMetadataForProperties(typeof(string)))
                .Returns(expectedProperties);

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

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
                var propertyCache = new DefaultMetadataDetailsCache(
                        ModelMetadataIdentity.ForProperty(typeof(int), kvp.Key, typeof(string)),
                        attributes: null);

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
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

            var metadata = new DefaultModelMetadata(provider.Object, detailsProvider, cache);

            // Act
            var properties = metadata.Properties;

            // Assert
            Assert.Equal(expectedNames.Count(), properties.Count);
            Assert.Equal(expectedNames.ToArray(), properties.Select(p => p.PropertyName).ToArray());
        }
#endif

        [Fact]
        public void PropertiesSetOnce()
        {
            // Arrange
            var provider = new EmptyModelMetadataProvider();
            var detailsProvider = new EmptyCompositeMetadataDetailsProvider();

            var key = ModelMetadataIdentity.ForType(typeof(string));
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

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
            var cache = new DefaultMetadataDetailsCache(key, new object[0]);

            var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);

            // Act
            var firstPropertiesEvaluation = metadata.Properties.ToList();
            var secondPropertiesEvaluation = metadata.Properties.ToList();

            // Assert
            // Identical ModelMetadata objects every time we run through the Properties collection.
            Assert.Equal(firstPropertiesEvaluation, secondPropertiesEvaluation);
        }

        private void ActionMethod(string input)
        {
        }
    }
}
