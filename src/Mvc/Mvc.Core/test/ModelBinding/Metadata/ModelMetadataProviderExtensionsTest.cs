// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class ModelMetadataProviderExtensionsTest
{
    [Fact]
    public void GetMetadataForPropertyInvalidPropertyNameThrows()
    {
        // Arrange
        var provider = (IModelMetadataProvider)new EmptyModelMetadataProvider();

        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => provider.GetMetadataForProperty(typeof(object), propertyName: "BadPropertyName"),
            "propertyName",
            "The property System.Object.BadPropertyName could not be found.");
    }
}
