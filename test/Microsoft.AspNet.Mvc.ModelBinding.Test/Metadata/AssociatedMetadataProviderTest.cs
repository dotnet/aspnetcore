// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
#if ASPNET50
using System.ComponentModel;
#endif
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class AssociatedMetadataProviderTest
    {
        // GetMetadataForProperties

        [Fact]
        public void GetMetadataForPropertiesCreatesMetadataForAllPropertiesOnModelWithPropertyValues()
        {
            // Arrange
            var model = new PropertyModel { LocalAttributes = 42, MetadataAttributes = "hello", MixedAttributes = 21.12 };
            var provider = new TestableAssociatedMetadataProvider();

            // Act
            // Call ToList() to force the lazy evaluation to evaluate
            provider.GetMetadataForProperties(typeof(PropertyModel)).ToList();

            // Assert
            var local = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                m => m.ContainerType == typeof(PropertyModel) && m.PropertyName == "LocalAttributes");
            Assert.Equal(typeof(int), local.ModelType);
            Assert.True(local.Attributes.Any(a => a is RequiredAttribute));

            var metadata = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                m => m.ContainerType == typeof(PropertyModel) && m.PropertyName == "MetadataAttributes");
            Assert.Equal(typeof(string), metadata.ModelType);
            Assert.True(metadata.Attributes.Any(a => a is RangeAttribute));

            var mixed = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                m => m.ContainerType == typeof(PropertyModel) && m.PropertyName == "MixedAttributes");
            Assert.Equal(typeof(double), mixed.ModelType);
            Assert.True(mixed.Attributes.Any(a => a is RequiredAttribute));
            Assert.True(mixed.Attributes.Any(a => a is RangeAttribute));
        }

        [Fact]
        public void GetMetadataForProperties_ExcludesIndexers()
        {
            // Arrange
            var model = new ModelWithIndexer();
            var provider = new TestableAssociatedMetadataProvider();
            var modelType = model.GetType();

            // Act
            provider.GetMetadataForProperties(modelType).ToList();

            // Assert
            Assert.Equal(2, provider.CreateMetadataFromPrototypeLog.Count);

            var valueMetadata = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                m => m.ContainerType == modelType && m.PropertyName == "Value");
            Assert.Equal(typeof(string), valueMetadata.ModelType);
            Assert.Single(valueMetadata.Attributes.OfType<MinLengthAttribute>());

            var testPropertyMetadata = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                m => m.ContainerType == modelType && m.PropertyName == "TestProperty");
            Assert.Equal(typeof(string), testPropertyMetadata.ModelType);
        }

        [Fact]
        public void GetMetadataForParameterNullOrEmptyPropertyNameThrows()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();

            // Act & Assert
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => provider.GetMetadataForParameter(methodInfo: null, parameterName: null),
                "parameterName");
            ExceptionAssert.ThrowsArgumentNullOrEmpty(
                () => provider.GetMetadataForParameter(methodInfo: null, parameterName: null),
                "parameterName");
        }

        // GetMetadata and access metadata for a property

        [Fact]
        public void GetMetadataForProperty_WithLocalAttributes()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();
            var metadata = new ModelMetadata(provider, null, typeof(PropertyModel), null);

            var propertyMetadata = new ModelMetadata(provider, typeof(PropertyModel), typeof(int), "LocalAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = propertyMetadata;

            // Act
            var result = provider.GetMetadataForProperty(typeof(PropertyModel), "LocalAttributes");

            // Assert
            Assert.Same(propertyMetadata, result);
            var localAttributes = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                parameters => parameters.PropertyName == "LocalAttributes");
            Assert.Single(localAttributes.Attributes, a => a is RequiredAttribute);
        }

        [Fact]
        public void GetMetadataForProperty_WithMetadataAttributes()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();
            var metadata = new ModelMetadata(provider, null, typeof(PropertyModel), null);

            var propertyMetadata = new ModelMetadata(provider, typeof(PropertyModel), typeof(string), "MetadataAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = propertyMetadata;

            // Act
            var result = metadata.Properties["MetadataAttributes"];

            // Assert
            Assert.Same(propertyMetadata, result);
            var parmaters = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                p => p.PropertyName == "MetadataAttributes");
            Assert.Single(parmaters.Attributes, a => a is RangeAttribute);
        }

        [Fact]
        public void GetMetadataForProperty_WithMixedAttributes()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();
            var metadata = new ModelMetadata(provider, null, typeof(PropertyModel), null);

            var propertyMetadata = new ModelMetadata(provider, typeof(PropertyModel), typeof(double), "MixedAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = propertyMetadata;

            // Act
            var result = metadata.Properties["MixedAttributes"];

            // Assert
            Assert.Same(propertyMetadata, result);
            var parms = Assert.Single(provider.CreateMetadataPrototypeLog, p => p.PropertyName == "MixedAttributes");
            Assert.Single(parms.Attributes, a => a is RequiredAttribute);
            Assert.Single(parms.Attributes, a => a is RangeAttribute);
        }

        // GetMetadataForType

#if ASPNET50 // No ReadOnlyAttribute in K
        [Fact]
        public void GetMetadataForTypeIncludesAttributesOnType()
        {
            var provider = new TestableAssociatedMetadataProvider();
            var metadata = new ModelMetadata(provider, null, typeof(TypeModel), null);
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            var result = provider.GetMetadataForType(typeof(TypeModel));

            // Assert
            Assert.Same(metadata, result);
            var parms = Assert.Single(provider.CreateMetadataPrototypeLog, p => p.ModelType == typeof(TypeModel));
            Assert.Single(parms.Attributes, a => a is ReadOnlyAttribute);
        }
#endif

        // Helpers

        private class PropertyModel
        {
            [Required]
            public int LocalAttributes { get; set; }

            [Range(10, 100)]
            public string MetadataAttributes { get; set; }

            [Required]
            [Range(10, 100)]
            public double MixedAttributes { get; set; }
        }

        private class BaseType
        {
            public string TestProperty { get; set; }
        }

        private class ModelWithIndexer : BaseType
        {
            public string this[string x]
            {
                get { return string.Empty; }
                set { }
            }

            [MinLength(4)]
            public string Value { get; set; }
        }

#if ASPNET50 // No [ReadOnly] in K
        [ReadOnly(true)]
        private class TypeModel
        {
        }
#endif

        private class TestableAssociatedMetadataProvider : AssociatedMetadataProvider<ModelMetadata>
        {
            public List<CreateMetadataPrototypeParams> CreateMetadataPrototypeLog = new List<CreateMetadataPrototypeParams>();
            public List<CreateMetadataFromPrototypeParams> CreateMetadataFromPrototypeLog = new List<CreateMetadataFromPrototypeParams>();
            public ModelMetadata CreateMetadataPrototypeReturnValue = null;
            public ModelMetadata CreateMetadataFromPrototypeReturnValue = null;

            protected override ModelMetadata CreateMetadataPrototype(IEnumerable<object> attributes, Type containerType, Type modelType, string propertyName)
            {
                CreateMetadataPrototypeLog.Add(new CreateMetadataPrototypeParams
                {
                    Attributes = attributes,
                    ContainerType = containerType,
                    ModelType = modelType,
                    PropertyName = propertyName
                });

                return CreateMetadataPrototypeReturnValue;
            }

            protected override ModelMetadata CreateMetadataFromPrototype(ModelMetadata prototype)
            {
                CreateMetadataFromPrototypeLog.Add(new CreateMetadataFromPrototypeParams
                {
                    Prototype = prototype,
                });

                return CreateMetadataFromPrototypeReturnValue;
            }
        }

        private class CreateMetadataPrototypeParams
        {
            public IEnumerable<object> Attributes { get; set; }
            public Type ContainerType { get; set; }
            public Type ModelType { get; set; }
            public string PropertyName { get; set; }
        }

        private class CreateMetadataFromPrototypeParams
        {
            public ModelMetadata Prototype { get; set; }
        }
    }
}
