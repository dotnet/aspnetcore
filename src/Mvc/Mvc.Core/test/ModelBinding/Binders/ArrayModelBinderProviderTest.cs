// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class ArrayModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(TestClass))]
    [InlineData(typeof(IList<int>))]
    public void Create_ForNonArrayTypes_ReturnsNull(Type modelType)
    {
        // Arrange
        var provider = new ArrayModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(typeof(byte[]))]
    [InlineData(typeof(string[]))]
    [InlineData(typeof(TestClass[]))]
    [InlineData(typeof(DateTime?[]))]
    public void Create_ForArrayTypes_ReturnsBinder(Type modelType)
    {
        // Arrange
        var provider = new ArrayModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);
        context.OnCreatingBinder((m) =>
        {
            // Expect to be called with the element type to create a binder for elements.
            Assert.Equal(modelType.GetElementType(), m.ModelType);
            return Mock.Of<IModelBinder>();
        });

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType(typeof(ArrayModelBinder<>).MakeGenericType(modelType.GetElementType()), result);
    }

    [Fact]
    public void Create_ForArrayType_ReturnsBinder()
    {
        // Arrange
        var provider = new ArrayModelBinderProvider();

        var context = new TestModelBinderProviderContext(typeof(int[]));
        context.OnCreatingBinder(m =>
        {
            Assert.Equal(typeof(int), m.ModelType);
            return Mock.Of<IModelBinder>();
        });

        // Act
        var result = provider.GetBinder(context);

        // Assert
        var binder = Assert.IsType<ArrayModelBinder<int>>(result);
        Assert.True(binder.AllowValidatingTopLevelNodes);
    }

    [Fact]
    public void Create_ForModelMetadataReadOnly_ReturnsNull()
    {
        // Arrange
        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForProperty(
            typeof(ModelWithIntArrayProperty),
            nameof(ModelWithIntArrayProperty.ArrayProperty)).BindingDetails(bd => bd.IsReadOnly = true);

        var modelMetadata = metadataProvider.GetMetadataForProperty(
            typeof(ModelWithIntArrayProperty),
            nameof(ModelWithIntArrayProperty.ArrayProperty));

        var provider = new ArrayModelBinderProvider();
        var context = new TestModelBinderProviderContext(typeof(int[]));
        context.OnCreatingBinder((m) =>
        {
            // Expect to be called with the element type to create a binder for elements.
            Assert.Equal(typeof(int), m.ModelType);
            return Mock.Of<IModelBinder>();
        });

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<ArrayModelBinder<int>>(result);
    }

    private class TestClass
    {
    }

    private class ModelWithIntArrayProperty
    {
        public int[] ArrayProperty { get; set; }
    }
}
