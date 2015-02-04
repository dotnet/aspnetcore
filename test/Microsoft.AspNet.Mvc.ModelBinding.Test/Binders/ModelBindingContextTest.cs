// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ModelBindingContextTest
    {
        [Fact]
        public void CopyConstructor()
        {
            // Arrange
            var originalBindingContext = new ModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(object)),
                ModelName = "theName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider()
            };

            var newModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(null, typeof(object));

            // Act
            var newBindingContext = new ModelBindingContext(originalBindingContext, string.Empty, newModelMetadata);

            // Assert
            Assert.Same(newModelMetadata, newBindingContext.ModelMetadata);
            Assert.Equal("", newBindingContext.ModelName);
            Assert.Equal(originalBindingContext.ModelState, newBindingContext.ModelState);
            Assert.Equal(originalBindingContext.ValueProvider, newBindingContext.ValueProvider);
        }

        [Fact]
        public void ModelTypeAreFedFromModelMetadata()
        {
            // Act
            var bindingContext = new ModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(() => 42, typeof(int))
            };

            // Assert
            Assert.Equal(typeof(int), bindingContext.ModelType);
        }
    }
}
