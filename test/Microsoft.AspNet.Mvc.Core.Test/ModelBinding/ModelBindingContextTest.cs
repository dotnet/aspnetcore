// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class ModelBindingContextTest
    {
        [Fact]
        public void GetChildModelBindingContext()
        {
            // Arrange
            var originalBindingContext = new ModelBindingContext
            {
                ModelMetadata = new TestModelMetadataProvider().GetMetadataForType(typeof(object)),
                ModelName = "theName",
                ModelState = new ModelStateDictionary(),
                ValueProvider = new SimpleHttpValueProvider()
            };

            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<object>().BindingDetails(d =>
                {
                    d.BindingSource = BindingSource.Custom;
                    d.BinderType = typeof(TestModelBinder);
                    d.BinderModelName = "custom";
                });

            var newModelMetadata = metadataProvider.GetMetadataForType(typeof(object));
            
            // Act
            var newBindingContext = ModelBindingContext.GetChildModelBindingContext(
                originalBindingContext,
                string.Empty,
                newModelMetadata);

            // Assert
            Assert.Same(newModelMetadata, newBindingContext.ModelMetadata);
            Assert.Same(newModelMetadata.BindingSource, newBindingContext.BindingSource);
            Assert.Same(newModelMetadata.BinderModelName, newBindingContext.BinderModelName);
            Assert.Same(newModelMetadata.BinderType, newBindingContext.BinderType);
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
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int))
            };

            // Assert
            Assert.Equal(typeof(int), bindingContext.ModelType);
        }

        private class TestModelBinder : IModelBinder
        {
            public Task<ModelBindingResult> BindModelAsync(ModelBindingContext bindingContext)
            {
                throw new NotImplementedException();
            }
        }
    }
}
