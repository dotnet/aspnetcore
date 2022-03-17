// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

public class DefaultModelMetadataProviderTest
{
    [Fact]
    public void GetMetadataForType_IncludesAttributes()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForType(typeof(ModelType));

        // Assert
        var defaultMetadata = Assert.IsType<DefaultModelMetadata>(metadata);

        var attribute = Assert.IsType<ModelAttribute>(Assert.Single(defaultMetadata.Attributes.Attributes));
        Assert.Equal("OnType", attribute.Value);
    }

    [Fact]
    public void GetMetadataForType_Cached()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata1 = Assert.IsType<DefaultModelMetadata>(provider.GetMetadataForType(typeof(ModelType)));
        var metadata2 = Assert.IsType<DefaultModelMetadata>(provider.GetMetadataForType(typeof(ModelType)));

        // Assert
        Assert.Same(metadata1, metadata2);
        Assert.Same(metadata1.Attributes, metadata2.Attributes);
        Assert.Same(metadata1.BindingMetadata, metadata2.BindingMetadata);
        Assert.Same(metadata1.DisplayMetadata, metadata2.DisplayMetadata);
        Assert.Same(metadata1.ValidationMetadata, metadata2.ValidationMetadata);
    }

    [Fact]
    public void GetMetadataForObjectType_Cached()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata1 = provider.GetMetadataForType(typeof(object));
        var metadata2 = provider.GetMetadataForType(typeof(object));

        // Assert
        Assert.Same(metadata1, metadata2);
    }

    [Fact]
    public void GetMetadataForProperties_IncludesContainerMetadataForAllProperties()
    {
        // Arrange
        var provider = CreateProvider();
        var modelType = typeof(ModelType);

        // Act
        var metadata = provider.GetMetadataForProperties(modelType).ToArray();

        // Assert
        Assert.Collection(
            metadata,
            (propertyMetadata) =>
            {
                Assert.Equal("Property1", propertyMetadata.PropertyName);
                Assert.NotNull(propertyMetadata.ContainerMetadata);
                Assert.Equal(modelType, propertyMetadata.ContainerMetadata.ModelType);
            },
            (propertyMetadata) =>
            {
                Assert.Equal("Property2", propertyMetadata.PropertyName);
                Assert.NotNull(propertyMetadata.ContainerMetadata);
                Assert.Equal(modelType, propertyMetadata.ContainerMetadata.ModelType);
            });
    }

    [Fact]
    public void GetMetadataForProperties_IncludesAllProperties()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForProperties(typeof(ModelType)).ToArray();

        // Assert
        Assert.Equal(2, metadata.Length);
        Assert.Single(metadata, m => m.PropertyName == "Property1");
        Assert.Single(metadata, m => m.PropertyName == "Property2");
    }

    [Fact]
    public void GetMetadataForProperties_IncludesAllProperties_ExceptIndexer()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForProperties(typeof(ModelTypeWithIndexer)).ToArray();

        // Assert
        Assert.Single(metadata);
        Assert.Single(metadata, m => m.PropertyName == "Property1");
    }

    [Fact]
    public void GetMetadataForProperties_Cached()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var properties1 = provider.GetMetadataForProperties(typeof(ModelType)).Cast<DefaultModelMetadata>().ToArray();
        var properties2 = provider.GetMetadataForProperties(typeof(ModelType)).Cast<DefaultModelMetadata>().ToArray();

        // Assert
        Assert.Equal(properties1.Length, properties2.Length);
        for (var i = 0; i < properties1.Length; i++)
        {
            Assert.Same(properties1[i], properties2[i]);
            Assert.Same(properties1[i].Attributes, properties2[i].Attributes);
            Assert.Same(properties1[i].BindingMetadata, properties2[i].BindingMetadata);
            Assert.Same(properties1[i].DisplayMetadata, properties2[i].DisplayMetadata);
            Assert.Same(properties1[i].ValidationMetadata, properties2[i].ValidationMetadata);
        }
    }

    [Fact]
    public void GetMetadataForType_PropertiesCollection_Cached()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata1 = Assert.IsType<DefaultModelMetadata>(provider.GetMetadataForType(typeof(ModelType)));
        var metadata2 = Assert.IsType<DefaultModelMetadata>(provider.GetMetadataForType(typeof(ModelType)));

        // Assert
        Assert.Same(metadata1.Properties, metadata2.Properties);
    }

    [Fact]
    public void GetMetadataForProperties_IncludesMergedAttributes()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForProperties(typeof(ModelType)).First();

        // Assert
        var defaultMetadata = Assert.IsType<DefaultModelMetadata>(metadata);

        var attributes = defaultMetadata.Attributes.Attributes.ToArray();
        Assert.Equal("OnProperty", Assert.IsType<ModelAttribute>(attributes[0]).Value);
        Assert.Equal("OnPropertyType", Assert.IsType<ModelAttribute>(attributes[1]).Value);
    }

    [Fact]
    public void GetMetadataForProperties_ExcludesHiddenProperties()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForProperties(typeof(DerivedModelWithHiding));

        // Assert
        var propertyMetadata = Assert.Single(metadata);
        Assert.Equal(typeof(string), propertyMetadata.ModelType);
    }

    [Fact]
    public void GetMetadataForProperties_PropertyGetter_IsNullSafe()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForProperties(typeof(ModelType));

        // Assert
        foreach (var property in metadata)
        {
            Assert.NotNull(property.PropertyGetter);
            Assert.Null(property.PropertyGetter(null));
        }
    }

    [Fact]
    public void GetMetadataForParameter_SuppliesEmptyAttributes_WhenParameterHasNoAttributes()
    {
        // Arrange
        var provider = CreateProvider();
        var parameters = typeof(ModelType)
            .GetMethod(nameof(ModelType.Method1))
            .GetParameters();

        // Act
        var metadata = provider.GetMetadataForParameter(parameters[0]);

        // Assert
        var defaultMetadata = Assert.IsType<DefaultModelMetadata>(metadata);

        // Not exactly "no attributes" due to SerializableAttribute on object.
        Assert.IsType<SerializableAttribute>(Assert.Single(defaultMetadata.Attributes.Attributes));
    }

    [Fact]
    public void GetMetadataForParameter_SuppliesAttributes_WhenParamHasAttributes()
    {
        // Arrange
        var provider = CreateProvider();
        var parameters = typeof(ModelType)
            .GetMethod(nameof(ModelType.Method1))
            .GetParameters();

        // Act
        var metadata = provider.GetMetadataForParameter(parameters[1]);

        // Assert
        var defaultMetadata = Assert.IsType<DefaultModelMetadata>(metadata);
        Assert.Collection(
            // Take(2) to ignore SerializableAttribute on object.
            defaultMetadata.Attributes.Attributes.Take(2),
            attribute =>
            {
                var modelAttribute = Assert.IsType<ModelAttribute>(attribute);
                Assert.Equal("ParamAttrib1", modelAttribute.Value);
            },
            attribute =>
            {
                var modelAttribute = Assert.IsType<ModelAttribute>(attribute);
                Assert.Equal("ParamAttrib2", modelAttribute.Value);
            });
    }

    [Fact]
    public void GetMetadataForParameter_Cached()
    {
        // Arrange
        var provider = CreateProvider();
        var parameter = typeof(ModelType)
            .GetMethod(nameof(ModelType.Method1))
            .GetParameters()[1];

        // Act
        var metadata1 = provider.GetMetadataForParameter(parameter);
        var metadata2 = provider.GetMetadataForParameter(parameter);

        // Assert
        Assert.Same(metadata1, metadata2);
    }

    [Fact]
    public void GetMetadataForParameter_WithModelType_ReturnsCombinedModelMetadata()
    {
        // Arrange
        var parameter = GetType()
            .GetMethod(nameof(GetMetadataForParameterTestMethod), BindingFlags.NonPublic | BindingFlags.Instance)
            .GetParameters()[0];
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForParameter(parameter, typeof(DerivedModelType));

        // Assert
        Assert.Equal(ModelMetadataKind.Parameter, metadata.MetadataKind);
        Assert.Equal(typeof(DerivedModelType), metadata.ModelType);

        var defaultModelMetadata = Assert.IsType<DefaultModelMetadata>(metadata);

        Assert.Collection(
            defaultModelMetadata.Attributes.Attributes,
            a => Assert.Equal("OnParameter", Assert.IsType<ModelAttribute>(a).Value),
            a => Assert.Equal("OnDerivedType", Assert.IsType<ModelAttribute>(a).Value),
            a => Assert.Equal("OnType", Assert.IsType<ModelAttribute>(a).Value));

        Assert.Collection(
            metadata.Properties.OrderBy(p => p.Name),
            p =>
            {
                Assert.Equal(nameof(DerivedModelType.DerivedProperty), p.Name);

                var defaultPropertyMetadata = Assert.IsType<DefaultModelMetadata>(p);
                Assert.Collection(
                    defaultPropertyMetadata.Attributes.Attributes.OfType<ModelAttribute>(),
                    a => Assert.Equal("OnDerivedProperty", Assert.IsType<ModelAttribute>(a).Value));
            },
            p =>
            {
                Assert.Equal(nameof(DerivedModelType.Property1), p.Name);

                var defaultPropertyMetadata = Assert.IsType<DefaultModelMetadata>(p);
                Assert.Collection(
                    defaultPropertyMetadata.Attributes.Attributes.OfType<ModelAttribute>(),
                    a => Assert.Equal("OnProperty", Assert.IsType<ModelAttribute>(a).Value),
                    a => Assert.Equal("OnPropertyType", Assert.IsType<ModelAttribute>(a).Value));
            },
            p =>
            {
                Assert.Equal(nameof(DerivedModelType.Property2), p.Name);
            });
    }

    [Fact]
    public void GetMetadataForParameter_WithModelType_CachesResults()
    {
        // Arrange
        var parameter = GetType()
            .GetMethod(nameof(GetMetadataForParameterTestMethod), BindingFlags.NonPublic | BindingFlags.Instance)
            .GetParameters()[0];
        var provider = CreateProvider();

        // Act
        var metadata1 = provider.GetMetadataForParameter(parameter, typeof(DerivedModelType));
        var metadata2 = provider.GetMetadataForParameter(parameter, typeof(DerivedModelType));

        // Assert
        Assert.Same(metadata1, metadata2);
    }

    [Fact]
    public void GetMetadataForParameter_WithModelType_VariesByModelType()
    {
        // Arrange
        var parameter = GetType()
            .GetMethod(nameof(GetMetadataForParameterTestMethod), BindingFlags.NonPublic | BindingFlags.Instance)
            .GetParameters()[0];
        var provider = CreateProvider();

        // Act
        var metadata1 = provider.GetMetadataForParameter(parameter, typeof(DerivedModelType));
        var metadata2 = provider.GetMetadataForParameter(parameter, typeof(object));

        // Assert
        Assert.NotSame(metadata1, metadata2);
    }

    [Fact]
    public void GetMetadataForProperty_WithModelType_ReturnsCombinedModelMetadata()
    {
        // Arrange
        var property = typeof(TestContainer)
            .GetProperty(nameof(TestContainer.ModelProperty));
        var provider = CreateProvider();

        // Act
        var metadata = provider.GetMetadataForProperty(property, typeof(DerivedModelType));

        // Assert
        Assert.Equal(ModelMetadataKind.Property, metadata.MetadataKind);
        Assert.Equal(typeof(DerivedModelType), metadata.ModelType);

        var defaultModelMetadata = Assert.IsType<DefaultModelMetadata>(metadata);

        Assert.Collection(
            defaultModelMetadata.Attributes.Attributes,
            a => Assert.Equal("OnProperty", Assert.IsType<ModelAttribute>(a).Value),
            a => Assert.Equal("OnDerivedType", Assert.IsType<ModelAttribute>(a).Value),
            a => Assert.Equal("OnType", Assert.IsType<ModelAttribute>(a).Value));

        Assert.Collection(
            metadata.Properties.OrderBy(p => p.Name),
            p =>
            {
                Assert.Equal(nameof(DerivedModelType.DerivedProperty), p.Name);

                var defaultPropertyMetadata = Assert.IsType<DefaultModelMetadata>(p);
                Assert.Collection(
                    defaultPropertyMetadata.Attributes.Attributes.OfType<ModelAttribute>(),
                    a => Assert.Equal("OnDerivedProperty", Assert.IsType<ModelAttribute>(a).Value));
            },
            p =>
            {
                Assert.Equal(nameof(DerivedModelType.Property1), p.Name);

                var defaultPropertyMetadata = Assert.IsType<DefaultModelMetadata>(p);
                Assert.Collection(
                    defaultPropertyMetadata.Attributes.Attributes.OfType<ModelAttribute>(),
                    a => Assert.Equal("OnProperty", Assert.IsType<ModelAttribute>(a).Value),
                    a => Assert.Equal("OnPropertyType", Assert.IsType<ModelAttribute>(a).Value));
            },
            p =>
            {
                Assert.Equal(nameof(DerivedModelType.Property2), p.Name);
            });
    }

    [Fact]
    public void GetMetadataForProperty_WithModelType_CachesResults()
    {
        // Arrange
        var property = typeof(TestContainer)
            .GetProperty(nameof(TestContainer.ModelProperty));
        var provider = CreateProvider();

        // Act
        var metadata1 = provider.GetMetadataForProperty(property, typeof(DerivedModelType));
        var metadata2 = provider.GetMetadataForProperty(property, typeof(DerivedModelType));

        // Assert
        Assert.Same(metadata1, metadata2);
    }

    [Fact]
    public void GetMetadataForProperty_WithModelType_VariesByModelType()
    {
        // Arrange
        var property = typeof(TestContainer)
            .GetProperty(nameof(TestContainer.ModelProperty));
        var provider = CreateProvider();

        // Act
        var metadata1 = provider.GetMetadataForProperty(property, typeof(DerivedModelType));
        var metadata2 = provider.GetMetadataForProperty(property, typeof(object));

        // Assert
        Assert.NotSame(metadata1, metadata2);
    }

    private static DefaultModelMetadataProvider CreateProvider()
    {
        return new DefaultModelMetadataProvider(
            new EmptyCompositeMetadataDetailsProvider(),
            Options.Create(new MvcOptions()));
    }

    [Model("OnType")]
    private class ModelType
    {
        [Model("OnProperty")]
        public PropertyType Property1 { get; } = new PropertyType();

        public PropertyType Property2 { get; set; }

        public void Method1(
            object paramWithNoAttributes,
            [Model("ParamAttrib1"), Model("ParamAttrib2")] object paramWithTwoAttributes)
        {
        }
    }

    [Model("OnPropertyType")]
    private class PropertyType
    {
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    private class ModelAttribute : Attribute
    {
        public ModelAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }

    private class ModelTypeWithIndexer
    {
        public PropertyType this[string key] => null;

        public PropertyType Property1 { get; set; }
    }

    private void GetMetadataForParameterTestMethod([Model("OnParameter")] ModelType parameter)
    {
    }

    private class BaseModelWithHiding
    {
        public int Property { get; set; }
    }

    private class DerivedModelWithHiding : BaseModelWithHiding
    {
        public new string Property { get; set; }
    }

    [Model("OnDerivedType")]
    private class DerivedModelType : ModelType
    {
        [Model("OnDerivedProperty")]
        public string DerivedProperty { get; set; }
    }

    private class TestContainer
    {
        [Model("OnProperty")]
        public ModelType ModelProperty { get; set; }
    }
}
