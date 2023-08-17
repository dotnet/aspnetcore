// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class BindingInfoTest
{
    [Fact]
    public void GetBindingInfo_WithAttributes_ConstructsBindingInfo()
    {
        // Arrange
        var attributes = new object[]
        {
                new FromQueryAttribute { Name = "Test" },
        };

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same("Test", bindingInfo.BinderModelName);
        Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
    }

    [Fact]
    public void GetBindingInfo_ReadsPropertyPredicateProvider()
    {
        // Arrange
        var bindAttribute = new BindAttribute(include: "SomeProperty");
        var attributes = new object[]
        {
                bindAttribute,
        };

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same(bindAttribute, bindingInfo.PropertyFilterProvider);
    }

    [Fact]
    public void GetBindingInfo_ReadsEmptyBodyBehavior()
    {
        // Arrange
        var attributes = new object[]
        {
          new FromBodyAttribute { EmptyBodyBehavior = EmptyBodyBehavior.Allow },
        };

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Equal(EmptyBodyBehavior.Allow, bindingInfo.EmptyBodyBehavior);
    }

    [Fact]
    public void GetBindingInfo_ReadsRequestPredicateProvider()
    {
        // Arrange
        var attributes = new object[]
        {
                new BindPropertyAttribute { Name = "PropertyPrefix", SupportsGet = true, },
        };

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same("PropertyPrefix", bindingInfo.BinderModelName);
        Assert.NotNull(bindingInfo.RequestPredicate);
    }

    [Fact]
    public void GetBindingInfo_ReturnsNull_IfNoBindingAttributesArePresent()
    {
        // Arrange
        var attributes = new object[] { new ControllerAttribute(), new BindNeverAttribute(), };

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes);

        // Assert
        Assert.Null(bindingInfo);
    }

    [Fact]
    public void GetBindingInfo_WithAttributesAndModelMetadata_UsesValuesFromBindingInfo_IfAttributesPresent()
    {
        // Arrange
        var attributes = new object[]
        {
                new ModelBinderAttribute { BinderType = typeof(ComplexObjectModelBinder), Name = "Test" },
        };
        var modelType = typeof(Guid);
        var provider = new TestModelMetadataProvider();
        provider.ForType(modelType).BindingDetails(metadata =>
        {
            metadata.BindingSource = BindingSource.Special;
            metadata.BinderType = typeof(SimpleTypeModelBinder);
            metadata.BinderModelName = "Different";
        });
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same(typeof(ComplexObjectModelBinder), bindingInfo.BinderType);
        Assert.Same("Test", bindingInfo.BinderModelName);
    }

    [Fact]
    public void GetBindingInfo_WithAttributesAndModelMetadata_UsesEmptyBodyBehaviorFromBindingInfo_IfAttributesPresent()
    {
        // Arrange
        var attributes = new object[]
        {
                new FromBodyAttribute() { EmptyBodyBehavior = EmptyBodyBehavior.Disallow }
        };
        var modelType = typeof(Guid?);
        var provider = new TestModelMetadataProvider();
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Equal(EmptyBodyBehavior.Disallow, bindingInfo.EmptyBodyBehavior);
    }

    [Fact]
    public void GetBindingInfo_WithAttributesAndModelMetadata_UsesBinderNameFromModelMetadata_WhenNotFoundViaAttributes()
    {
        // Arrange
        var attributes = new object[]
        {
                new ModelBinderAttribute(typeof(ComplexObjectModelBinder)),
                new ControllerAttribute(),
                new BindNeverAttribute(),
        };
        var modelType = typeof(Guid);
        var provider = new TestModelMetadataProvider();
        provider.ForType(modelType).BindingDetails(metadata =>
        {
            metadata.BindingSource = BindingSource.Special;
            metadata.BinderType = typeof(SimpleTypeModelBinder);
            metadata.BinderModelName = "Different";
        });
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same(typeof(ComplexObjectModelBinder), bindingInfo.BinderType);
        Assert.Same("Different", bindingInfo.BinderModelName);
        Assert.Same(BindingSource.Custom, bindingInfo.BindingSource);
    }

    [Fact]
    public void GetBindingInfo_WithAttributesAndModelMetadata_UsesModelBinderFromModelMetadata_WhenNotFoundViaAttributes()
    {
        // Arrange
        var attributes = new object[] { new ControllerAttribute(), new BindNeverAttribute(), };
        var modelType = typeof(Guid);
        var provider = new TestModelMetadataProvider();
        provider.ForType(modelType).BindingDetails(metadata =>
        {
            metadata.BinderType = typeof(ComplexObjectModelBinder);
        });
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same(typeof(ComplexObjectModelBinder), bindingInfo.BinderType);
    }

    [Fact]
    public void GetBindingInfo_WithAttributesAndModelMetadata_UsesBinderSourceFromModelMetadata_WhenNotFoundViaAttributes()
    {
        // Arrange
        var attributes = new object[]
        {
                new BindPropertyAttribute(),
                new ControllerAttribute(),
                new BindNeverAttribute(),
        };
        var modelType = typeof(Guid);
        var provider = new TestModelMetadataProvider();
        provider.ForType(modelType).BindingDetails(metadata =>
        {
            metadata.BindingSource = BindingSource.Services;
        });
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Services, bindingInfo.BindingSource);
    }

    [Fact]
    public void GetBindingInfo_WithAttributesAndModelMetadata_UsesPropertyPredicateProviderFromModelMetadata_WhenNotFoundViaAttributes()
    {
        // Arrange
        var attributes = new object[]
        {
                new ModelBinderAttribute(typeof(ComplexObjectModelBinder)),
                new ControllerAttribute(),
                new BindNeverAttribute(),
        };
        var propertyFilterProvider = Mock.Of<IPropertyFilterProvider>();
        var modelType = typeof(Guid);
        var provider = new TestModelMetadataProvider();
        provider.ForType(modelType).BindingDetails(metadata =>
        {
            metadata.PropertyFilterProvider = propertyFilterProvider;
        });
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same(propertyFilterProvider, bindingInfo.PropertyFilterProvider);
    }

    [Fact]
    public void GetBindingInfo_WithAttributesAndModelMetadata_UsesEmptyBodyFromModelMetadata_WhenNotFoundViaAttributes()
    {
        // Arrange
        var attributes = new object[]
        {
                new ControllerAttribute(),
                new BindNeverAttribute(),
                new FromBodyAttribute(),
        };
        var modelType = typeof(Guid?);
        var provider = new TestModelMetadataProvider();
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Equal(EmptyBodyBehavior.Allow, bindingInfo.EmptyBodyBehavior);
    }

    [Fact]
    public void GetBindingInfo_WithAttributesAndModelMetadata_PreserveEmptyBodyDefault_WhenNotNullable()
    {
        // Arrange
        var attributes = new object[]
        {
                new ControllerAttribute(),
                new BindNeverAttribute(),
                new FromBodyAttribute(),
        };
        var modelType = typeof(Guid);
        var provider = new TestModelMetadataProvider();
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Equal(EmptyBodyBehavior.Default, bindingInfo.EmptyBodyBehavior);
    }

    [Fact]
    public void GetBindingInfo_WithFromKeyedServicesAttribute()
    {
        // Arrange
        var key = new object();
        var attributes = new object[]
        {
                new FromKeyedServicesAttribute(key),
        };
        var modelType = typeof(Guid);
        var provider = new TestModelMetadataProvider();
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        // Assert
        Assert.NotNull(bindingInfo);
        Assert.Same(BindingSource.Services, bindingInfo.BindingSource);
        Assert.Same(key, bindingInfo.ServiceKey);
    }

    [Fact]
    public void GetBindingInfo_ThrowsWhenWithFromKeyedServicesAttributeAndIFromServiceMetadata()
    {
        // Arrange
        var attributes = new object[]
        {
                new FromKeyedServicesAttribute(new object()),
                new FromServicesAttribute()
        };
        var modelType = typeof(Guid);
        var provider = new TestModelMetadataProvider();
        var modelMetadata = provider.GetMetadataForType(modelType);

        // Act and Assert
        Assert.Throws<NotSupportedException>(() => BindingInfo.GetBindingInfo(attributes, modelMetadata));
    }
}
