// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
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
}