// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

public class BindingSourceMetadataProviderTests
{
    [Fact]
    public void CreateBindingMetadata_ForMatchingType_SetsBindingSource()
    {
        // Arrange
        var provider = new BindingSourceMetadataProvider(typeof(Test), BindingSource.Special);

        var key = ModelMetadataIdentity.ForType(typeof(Test));

        var context = new BindingMetadataProviderContext(key, new ModelAttributes(new object[0], new object[0], null));

        // Act
        provider.CreateBindingMetadata(context);

        // Assert
        Assert.Equal(BindingSource.Special, context.BindingMetadata.BindingSource);
    }

    private class Test
    {
    }
}
