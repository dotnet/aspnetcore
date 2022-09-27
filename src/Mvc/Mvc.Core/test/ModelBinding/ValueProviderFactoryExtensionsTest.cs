// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ValueProviderFactoryExtensionsTest
{
    [Fact]
    public void RemoveType_RemovesAllOfType()
    {
        // Arrange
        var list = new List<IValueProviderFactory>
            {
                new FooValueProviderFactory(),
                new BarValueProviderFactory(),
                new FooValueProviderFactory()
            };

        // Act
        list.RemoveType(typeof(FooValueProviderFactory));

        // Assert
        var factory = Assert.Single(list);
        Assert.IsType<BarValueProviderFactory>(factory);
    }

    [Fact]
    public void GenericRemoveType_RemovesAllOfType()
    {
        // Arrange
        var list = new List<IValueProviderFactory>
            {
                new FooValueProviderFactory(),
                new BarValueProviderFactory(),
                new FooValueProviderFactory()
            };

        // Act
        list.RemoveType<FooValueProviderFactory>();

        // Assert
        var factory = Assert.Single(list);
        Assert.IsType<BarValueProviderFactory>(factory);
    }

    private class FooValueProviderFactory : IValueProviderFactory
    {
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class BarValueProviderFactory : IValueProviderFactory
    {
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            throw new NotImplementedException();
        }
    }
}
