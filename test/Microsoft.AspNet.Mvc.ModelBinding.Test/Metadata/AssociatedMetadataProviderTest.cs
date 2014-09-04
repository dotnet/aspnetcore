// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            provider.GetMetadataForProperties(model, typeof(PropertyModel)).ToList();

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
            var value = "some value";
            var model = new ModelWithIndexer { Value = value };
            var provider = new TestableAssociatedMetadataProvider();
            var modelType = model.GetType();

            // Act
            provider.GetMetadataForProperties(model, modelType).ToList();

            // Assert
            Assert.Equal(2, provider.CreateMetadataFromPrototypeLog.Count);
            Assert.Equal(value, provider.CreateMetadataFromPrototypeLog[0].Model);
            Assert.Null(provider.CreateMetadataFromPrototypeLog[1].Model);

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
        public void GetMetadataForPropertyWithNullContainerReturnsMetadataWithNullValuesForProperties()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();

            // Act
            provider.GetMetadataForProperties(null, typeof(PropertyModel)).ToList(); // Call ToList() to force the lazy evaluation to evaluate

            // Assert
            Assert.NotEmpty(provider.CreateMetadataFromPrototypeLog);
            foreach (var parms in provider.CreateMetadataFromPrototypeLog)
            {
                Assert.Null(parms.Model);
            }
        }

        // GetMetadataForProperty

        [Fact]
        public void GetMetadataForPropertyNullOrEmptyPropertyNameThrows()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => provider.GetMetadataForProperty(modelAccessor: null, containerType: typeof(object), propertyName: null),
                "propertyName",
                "The value cannot be null or empty.");
            ExceptionAssert.ThrowsArgument(
                () => provider.GetMetadataForProperty(modelAccessor: null, containerType: typeof(object), propertyName: String.Empty),
                "propertyName",
                "The value cannot be null or empty.");
        }

        [Fact]
        public void GetMetadataForPropertyInvalidPropertyNameThrows()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => provider.GetMetadataForProperty(modelAccessor: null, containerType: typeof(object), propertyName: "BadPropertyName"),
                "propertyName",
                "The property System.Object.BadPropertyName could not be found.");
        }

        [Fact]
        public void GetMetadataForPropertyWithLocalAttributes()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();
            var metadata = new ModelMetadata(provider, typeof(PropertyModel), null, typeof(int), "LocalAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            var result = provider.GetMetadataForProperty(null, typeof(PropertyModel), "LocalAttributes");

            // Assert
            Assert.Same(metadata, result);
            var localAttributes = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                parameters => parameters.PropertyName == "LocalAttributes");
            Assert.Single(localAttributes.Attributes, a => a is RequiredAttribute);
        }

        [Fact]
        public void GetMetadataForPropertyWithMetadataAttributes()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();
            var metadata = new ModelMetadata(provider, typeof(PropertyModel), null, typeof(string), "MetadataAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            var result = provider.GetMetadataForProperty(null, typeof(PropertyModel), "MetadataAttributes");

            // Assert
            Assert.Same(metadata, result);
            var parmaters = Assert.Single(
                provider.CreateMetadataPrototypeLog,
                p => p.PropertyName == "MetadataAttributes");
            Assert.Single(parmaters.Attributes, a => a is RangeAttribute);
        }

        [Fact]
        public void GetMetadataForPropertyWithMixedAttributes()
        {
            // Arrange
            var provider = new TestableAssociatedMetadataProvider();
            var metadata = new ModelMetadata(provider, typeof(PropertyModel), null, typeof(double), "MixedAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            var result = provider.GetMetadataForProperty(null, typeof(PropertyModel), "MixedAttributes");

            // Assert
            Assert.Same(metadata, result);
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
            var metadata = new ModelMetadata(provider, null, null, typeof(TypeModel), null);
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            var result = provider.GetMetadataForType(null, typeof(TypeModel));

            // Assert
            Assert.Same(metadata, result);
            var parms = Assert.Single(provider.CreateMetadataPrototypeLog, p => p.ModelType == typeof(TypeModel));
            Assert.Single(parms.Attributes, a => a is ReadOnlyAttribute);
        }
#endif

        // Helpers

        // TODO: This type used System.ComponentModel.MetadataType to separate attribute declaration from property
        // declaration. Need to figure out if this is still relevant since the type does not exist in CoreCLR.
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

            protected override ModelMetadata CreateMetadataPrototype(IEnumerable<Attribute> attributes, Type containerType, Type modelType, string propertyName)
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

            protected override ModelMetadata CreateMetadataFromPrototype(ModelMetadata prototype, Func<object> modelAccessor)
            {
                CreateMetadataFromPrototypeLog.Add(new CreateMetadataFromPrototypeParams
                {
                    Prototype = prototype,
                    Model = modelAccessor == null ? null : modelAccessor()
                });

                return CreateMetadataFromPrototypeReturnValue;
            }
        }

        private class CreateMetadataPrototypeParams
        {
            public IEnumerable<Attribute> Attributes { get; set; }
            public Type ContainerType { get; set; }
            public Type ModelType { get; set; }
            public string PropertyName { get; set; }
        }

        private class CreateMetadataFromPrototypeParams
        {
            public ModelMetadata Prototype { get; set; }
            public object Model { get; set; }
        }
    }
}
