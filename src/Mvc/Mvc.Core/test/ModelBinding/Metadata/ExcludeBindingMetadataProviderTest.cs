// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

public class ExcludeBindingMetadataProviderTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsBindingAllowed_LeftAlone_WhenTypeDoesntMatch(bool initialValue)
    {
        // Arrange
        var provider = new ExcludeBindingMetadataProvider(typeof(string));

        var key = ModelMetadataIdentity.ForProperty(
            typeof(Person).GetProperty(nameof(Person.Age)),
            typeof(int),
            typeof(Person));

        var context = new BindingMetadataProviderContext(key, new ModelAttributes(new object[0], new object[0], null));

        context.BindingMetadata.IsBindingAllowed = initialValue;

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(initialValue, context.BindingMetadata.IsBindingAllowed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsBindingAllowed_IsFalse_WhenTypeMatches(bool initialValue)
    {
        // Arrange
        var provider = new ExcludeBindingMetadataProvider(typeof(int));

        var key = ModelMetadataIdentity.ForProperty(
            typeof(Person).GetProperty(nameof(Person.Age)),
            typeof(int),
            typeof(Person));

        var context = new BindingMetadataProviderContext(key, new ModelAttributes(new object[0], new object[0], null));

        context.BindingMetadata.IsBindingAllowed = initialValue;

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.False(context.BindingMetadata.IsBindingAllowed);
    }

    private class Person
    {
        public int Age { get; set; }
    }
}
