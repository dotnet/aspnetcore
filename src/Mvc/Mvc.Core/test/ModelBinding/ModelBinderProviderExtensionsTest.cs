// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ModelBinderProviderExtensionsTest
{
    [Fact]
    public void RemoveType_RemovesAllOfType()
    {
        // Arrange
        var list = new List<IModelBinderProvider>
            {
                new FooModelBinderProvider(),
                new BarModelBinderProvider(),
                new FooModelBinderProvider()
            };

        // Act
        list.RemoveType(typeof(FooModelBinderProvider));

        // Assert
        var provider = Assert.Single(list);
        Assert.IsType<BarModelBinderProvider>(provider);
    }

    [Fact]
    public void GenericRemoveType_RemovesAllOfType()
    {
        // Arrange
        var list = new List<IModelBinderProvider>
            {
                new FooModelBinderProvider(),
                new BarModelBinderProvider(),
                new FooModelBinderProvider()
            };

        // Act
        list.RemoveType<FooModelBinderProvider>();

        // Assert
        var provider = Assert.Single(list);
        Assert.IsType<BarModelBinderProvider>(provider);
    }

    private class FooModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class BarModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            throw new NotImplementedException();
        }
    }
}
