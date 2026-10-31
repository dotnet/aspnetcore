// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class ModelExplorerTest
{
    [Fact]
    public void ModelType_UsesRuntimeType()
    {
        // Arrange
        var provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(typeof(BaseClass), new DerivedClass());

        // Act
        var modelType = modelExplorer.ModelType;

        // Assert
        Assert.Equal(typeof(DerivedClass), modelType);
    }

    [Fact]
    public void ModelType_UsesDeclaredType_WhenModelIsNull()
    {
        // Arrange
        var provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(typeof(BaseClass), model: null);

        // Act
        var modelType = modelExplorer.ModelType;

        // Assert
        Assert.Equal(typeof(BaseClass), modelType);
    }

    [Fact]
    public void Properties_UsesRuntimeType()
    {
        // Arrange
        var provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(typeof(BaseClass), new DerivedClass());

        // Act
        var properties = modelExplorer.Properties.ToArray();

        // Assert
        Assert.Equal(2, properties.Length);

        var baseProperty = Assert.Single(properties, p => p.Metadata.PropertyName == "Base1");
        Assert.Equal(typeof(int), baseProperty.Metadata.ModelType);
        Assert.Equal(typeof(DerivedClass), baseProperty.Metadata.ContainerType);
        Assert.Same(modelExplorer, baseProperty.Container);

        var derivedProperty = Assert.Single(properties, p => p.Metadata.PropertyName == "Derived1");
        Assert.Equal(typeof(string), derivedProperty.Metadata.ModelType);
        Assert.Equal(typeof(DerivedClass), derivedProperty.Metadata.ContainerType);
        Assert.Same(modelExplorer, derivedProperty.Container);
    }

    [Fact]
    public void Properties_UsesDeclaredType_WhenModelIsNull()
    {
        // Arrange
        var provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(typeof(BaseClass), model: null);

        // Act
        var properties = modelExplorer.Properties.ToArray();

        // Assert
        Assert.Single(properties);

        var baseProperty = Assert.Single(properties, p => p.Metadata.PropertyName == "Base1");
        Assert.Equal(typeof(int), baseProperty.Metadata.ModelType);
        Assert.Equal(typeof(BaseClass), baseProperty.Metadata.ContainerType);
        Assert.Same(modelExplorer, baseProperty.Container);
    }

    [Fact]
    public void GetPropertyExplorer_DeferredModelAccess()
    {
        // Arrange
        var model = new DerivedClass()
        {
            Base1 = 5,
        };

        var provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(typeof(BaseClass), model);

        // Change the model value after creating the explorer
        var propertyExplorer = modelExplorer.GetExplorerForProperty("Base1");
        model.Base1 = 17;

        // Act
        var propertyValue = propertyExplorer.Model;

        // Assert
        Assert.Equal(17, propertyValue);
    }

    [Fact]
    public void GetPropertyExplorer_DeferredModelAccess_ContainerModelIsNull()
    {
        // Arrange
        var provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(typeof(BaseClass), model: null);

        var propertyExplorer = modelExplorer.GetExplorerForProperty("Base1");

        // Act
        var propertyValue = propertyExplorer.Model;

        // Assert
        Assert.Null(propertyValue);
    }

    [Fact]
    public void GetPropertyExplorer_ReturnsNull_ForPropertyNotFound()
    {
        // Arrange
        var model = new DerivedClass()
        {
            Base1 = 5,
        };

        var provider = new EmptyModelMetadataProvider();
        var modelExplorer = provider.GetModelExplorerForType(typeof(BaseClass), model);

        // Act
        var propertyExplorer = modelExplorer.GetExplorerForProperty("BadName");

        // Assert
        Assert.Null(propertyExplorer);
    }

    private class BaseClass
    {
        public int Base1 { get; set; }
    }

    private class DerivedClass : BaseClass
    {
        public string Derived1 { get; set; }
    }
}
