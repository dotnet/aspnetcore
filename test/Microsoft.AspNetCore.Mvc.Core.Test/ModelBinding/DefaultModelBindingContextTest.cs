// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Test;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class DefaultModelBindingContextTest
    {
        [Fact]
        public void EnterNestedScope_CopiesProperties()
        {
            // Arrange
            var bindingContext = new DefaultModelBindingContext
            {
                Model = new object(),
                ModelMetadata = new TestModelMetadataProvider().GetMetadataForType(typeof(object)),
                ModelName = "theName",
                OperationBindingContext = new OperationBindingContext(),
                ValueProvider = new SimpleValueProvider(),
                ModelState = new ModelStateDictionary(),
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
            var originalBinderModelName = bindingContext.BinderModelName;
            var originalBinderType = bindingContext.BinderType;
            var originalBindingSource = bindingContext.BindingSource;
            var originalModelState = bindingContext.ModelState;
            var originalOperationBindingContext = bindingContext.OperationBindingContext;
            var originalValueProvider = bindingContext.ValueProvider;

            var disposable = bindingContext.EnterNestedScope(
                modelMetadata: newModelMetadata,
                fieldName: "fieldName",
                modelName: "modelprefix.fieldName",
                model: null);

            // Assert
            Assert.Same(newModelMetadata.BinderModelName, bindingContext.BinderModelName);
            Assert.Same(newModelMetadata.BinderType, bindingContext.BinderType);
            Assert.Same(newModelMetadata.BindingSource, bindingContext.BindingSource);
            Assert.False(bindingContext.FallbackToEmptyPrefix);
            Assert.Equal("fieldName", bindingContext.FieldName);
            Assert.False(bindingContext.IsTopLevelObject);
            Assert.Null(bindingContext.Model);
            Assert.Same(newModelMetadata, bindingContext.ModelMetadata);
            Assert.Equal("modelprefix.fieldName", bindingContext.ModelName);
            Assert.Same(originalModelState, bindingContext.ModelState);
            Assert.Same(originalOperationBindingContext, bindingContext.OperationBindingContext);
            Assert.Same(originalValueProvider, bindingContext.ValueProvider);

            disposable.Dispose();
        }

        [Fact]
        public void ModelTypeAreFedFromModelMetadata()
        {
            // Act
            var bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(int))
            };

            // Assert
            Assert.Equal(typeof(int), bindingContext.ModelType);
        }

        private class TestModelBinder : IModelBinder
        {
            public Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }
                Debug.Assert(bindingContext.Result == null);

                throw new NotImplementedException();
            }
        }
    }
}
