// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class ModelValidatorProviderExtensionsTest
{
    [Fact]
    public void RemoveType_RemovesAllOfType()
    {
        // Arrange
        var list = new List<IModelValidatorProvider>
            {
                new FooModelValidatorProvider(),
                new BarModelValidatorProvider(),
                new FooModelValidatorProvider()
            };

        // Act
        list.RemoveType(typeof(FooModelValidatorProvider));

        // Assert
        var provider = Assert.Single(list);
        Assert.IsType<BarModelValidatorProvider>(provider);
    }

    [Fact]
    public void GenericRemoveType_RemovesAllOfType()
    {
        // Arrange
        var list = new List<IModelValidatorProvider>
            {
                new FooModelValidatorProvider(),
                new BarModelValidatorProvider(),
                new FooModelValidatorProvider()
            };

        // Act
        list.RemoveType<FooModelValidatorProvider>();

        // Assert
        var provider = Assert.Single(list);
        Assert.IsType<BarModelValidatorProvider>(provider);
    }

    private class FooModelValidatorProvider : IModelValidatorProvider
    {
        public void CreateValidators(ModelValidatorProviderContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class BarModelValidatorProvider : IModelValidatorProvider
    {
        public void CreateValidators(ModelValidatorProviderContext context)
        {
            throw new NotImplementedException();
        }
    }
}
