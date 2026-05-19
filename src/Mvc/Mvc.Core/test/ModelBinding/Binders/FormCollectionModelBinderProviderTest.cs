// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class FormCollectionModelBinderProviderTest
{
    [Theory]
    [InlineData(typeof(FormCollection))]
    [InlineData(typeof(DerivedFormCollection))]
    public void Create_ThrowsException_ForFormCollectionModelType(Type modelType)
    {
        // Arrange
        var provider = new FormCollectionModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetBinder(context));

        Assert.Equal(
            $"The '{typeof(FormCollectionModelBinder).FullName}' cannot bind to a model of type '{modelType.FullName}'. Change the model type to '{typeof(IFormCollection).FullName}' instead.",
            exception.Message);
    }

    [Theory]
    [InlineData(typeof(TestClass))]
    [InlineData(typeof(IList<int>))]
    [InlineData(typeof(int[]))]
    public void Create_ForNonFormCollectionTypes_ReturnsNull(Type modelType)
    {
        // Arrange
        var provider = new FormCollectionModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Create_ForFormCollectionToken_ReturnsBinder()
    {
        // Arrange
        var provider = new FormCollectionModelBinderProvider();
        var context = new TestModelBinderProviderContext(typeof(IFormCollection));

        // Act
        var result = provider.GetBinder(context);

        // Assert
        Assert.IsType<FormCollectionModelBinder>(result);
    }

    private class TestClass
    {
    }

    private class DerivedFormCollection : FormCollection
    {
        public DerivedFormCollection() : base(fields: null, files: null) { }
    }
}
