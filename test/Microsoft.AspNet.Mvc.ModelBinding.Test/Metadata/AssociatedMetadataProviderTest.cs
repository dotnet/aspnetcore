using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test.Binders
{
    public class AssociatedMetadataProviderTest
    {
        // GetMetadataForProperties

        //[Fact]
        //public void GetMetadataForPropertiesNullContainerTypeThrows()
        //{
        //    // Arrange
        //    TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();

        //    // Act & Assert
        //    ExceptionAssert.ThrowsArgumentNull(
        //        () => provider.GetMetadataForProperties(new Object(), containerType: null),
        //        "containerType");
        //}

        [Fact]
        public void GetMetadataForPropertiesCreatesMetadataForAllPropertiesOnModelWithPropertyValues()
        {
            // Arrange
            PropertyModel model = new PropertyModel { LocalAttributes = 42, MetadataAttributes = "hello", MixedAttributes = 21.12 };
            TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();

            // Act
            provider.GetMetadataForProperties(model, typeof(PropertyModel)).ToList(); // Call ToList() to force the lazy evaluation to evaluate

            // Assert
            CreateMetadataPrototypeParams local =
                provider.CreateMetadataPrototypeLog.Single(m => m.ContainerType == typeof(PropertyModel) &&
                                                       m.PropertyName == "LocalAttributes");
            Assert.Equal(typeof(int), local.ModelType);
            Assert.True(local.Attributes.Any(a => a is RequiredAttribute));

            CreateMetadataPrototypeParams metadata =
                provider.CreateMetadataPrototypeLog.Single(m => m.ContainerType == typeof(PropertyModel) &&
                                                       m.PropertyName == "MetadataAttributes");
            Assert.Equal(typeof(string), metadata.ModelType);
            Assert.True(metadata.Attributes.Any(a => a is RangeAttribute));

            CreateMetadataPrototypeParams mixed =
                provider.CreateMetadataPrototypeLog.Single(m => m.ContainerType == typeof(PropertyModel) &&
                                                       m.PropertyName == "MixedAttributes");
            Assert.Equal(typeof(double), mixed.ModelType);
            Assert.True(mixed.Attributes.Any(a => a is RequiredAttribute));
            Assert.True(mixed.Attributes.Any(a => a is RangeAttribute));
        }

        [Fact]
        public void GetMetadataForPropertyWithNullContainerReturnsMetadataWithNullValuesForProperties()
        {
            // Arrange
            TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();

            // Act
            provider.GetMetadataForProperties(null, typeof(PropertyModel)).ToList(); // Call ToList() to force the lazy evaluation to evaluate

            // Assert
            Assert.True(provider.CreateMetadataFromPrototypeLog.Any());
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
            TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => provider.GetMetadataForProperty(modelAccessor: null, containerType: typeof(object), propertyName: null),
                "propertyName",
                "The argument 'propertyName' is null or empty.");
            ExceptionAssert.ThrowsArgument(
                () => provider.GetMetadataForProperty(modelAccessor: null, containerType: typeof(object), propertyName: String.Empty),
                "propertyName",
                "The argument 'propertyName' is null or empty.");
        }

        [Fact]
        public void GetMetadataForPropertyInvalidPropertyNameThrows()
        {
            // Arrange
            TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();

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
            TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();
            ModelMetadata metadata = new ModelMetadata(provider, typeof(PropertyModel), null, typeof(int), "LocalAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            ModelMetadata result = provider.GetMetadataForProperty(null, typeof(PropertyModel), "LocalAttributes");

            // Assert
            Assert.Same(metadata, result);
            Assert.True(provider.CreateMetadataPrototypeLog.Single(parameters => parameters.PropertyName == "LocalAttributes").Attributes.Any(a => a is RequiredAttribute));
        }

        [Fact]
        public void GetMetadataForPropertyWithMetadataAttributes()
        {
            // Arrange
            TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();
            ModelMetadata metadata = new ModelMetadata(provider, typeof(PropertyModel), null, typeof(string), "MetadataAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            ModelMetadata result = provider.GetMetadataForProperty(null, typeof(PropertyModel), "MetadataAttributes");

            // Assert
            Assert.Same(metadata, result);
            CreateMetadataPrototypeParams parms = provider.CreateMetadataPrototypeLog.Single(p => p.PropertyName == "MetadataAttributes");
            Assert.True(parms.Attributes.Any(a => a is RangeAttribute));
        }

        [Fact]
        public void GetMetadataForPropertyWithMixedAttributes()
        {
            // Arrange
            TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();
            ModelMetadata metadata = new ModelMetadata(provider, typeof(PropertyModel), null, typeof(double), "MixedAttributes");
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            ModelMetadata result = provider.GetMetadataForProperty(null, typeof(PropertyModel), "MixedAttributes");

            // Assert
            Assert.Same(metadata, result);
            CreateMetadataPrototypeParams parms = provider.CreateMetadataPrototypeLog.Single(p => p.PropertyName == "MixedAttributes");
            Assert.True(parms.Attributes.Any(a => a is RequiredAttribute));
            Assert.True(parms.Attributes.Any(a => a is RangeAttribute));
        }

        // GetMetadataForType

        //[Fact]
        //public void GetMetadataForTypeNullModelTypeThrows()
        //{
        //    // Arrange
        //    TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();

        //    // Act & Assert
        //    ExceptionAssert.ThrowsArgumentNull(
        //        () => provider.GetMetadataForType(() => new Object(), modelType: null),
        //        "modelType");
        //}

#if NET45 // No ReadOnlyAttribute in K
        [Fact]
        public void GetMetadataForTypeIncludesAttributesOnType()
        {
            TestableAssociatedMetadataProvider provider = new TestableAssociatedMetadataProvider();
            ModelMetadata metadata = new ModelMetadata(provider, null, null, typeof(TypeModel), null);
            provider.CreateMetadataFromPrototypeReturnValue = metadata;

            // Act
            ModelMetadata result = provider.GetMetadataForType(null, typeof(TypeModel));

            // Assert
            Assert.Same(metadata, result);
            CreateMetadataPrototypeParams parms = provider.CreateMetadataPrototypeLog.Single(p => p.ModelType == typeof(TypeModel));
            Assert.True(parms.Attributes.Any(a => a is ReadOnlyAttribute));
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

        private sealed class RequiredAttribute : Attribute
        { }

        private sealed class RangeAttribute : Attribute
        {
            public RangeAttribute(int min, int max)
            {
            }
        }

        private class ModelWithReadOnlyProperty
        {
            public int ReadOnlyProperty { get; private set; }
        }

#if NET45 // No [ReadOnly] in K
        [ReadOnly(true)]
        private class TypeModel
        {
        }
#endif

        class TestableAssociatedMetadataProvider : AssociatedMetadataProvider<ModelMetadata>
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

        class CreateMetadataPrototypeParams
        {
            public IEnumerable<Attribute> Attributes { get; set; }
            public Type ContainerType { get; set; }
            public Type ModelType { get; set; }
            public string PropertyName { get; set; }
        }

        class CreateMetadataFromPrototypeParams
        {
            public ModelMetadata Prototype { get; set; }
            public object Model { get; set; }
        }
    }
}
