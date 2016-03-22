// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    public class CancellationTokenModelBinderTests
    {
        [Fact]
        public async Task CancellationTokenModelBinder_ReturnsNonEmptyResult_ForCancellationTokenType()
        {
            // Arrange
            var bindingContext = GetBindingContext(typeof(CancellationToken));
            var binder = new CancellationTokenModelBinder();

            // Act
            var result = await binder.BindModelResultAsync(bindingContext);

            // Assert
            Assert.NotEqual(default(ModelBindingResult), result);
            Assert.True(result.IsModelSet);
            Assert.Equal(bindingContext.OperationBindingContext.HttpContext.RequestAborted, result.Model);
        }

        private static DefaultModelBindingContext GetBindingContext(Type modelType)
        {
            var metadataProvider = new EmptyModelMetadataProvider();
            DefaultModelBindingContext bindingContext = new DefaultModelBindingContext
            {
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "someName",
                ValueProvider = new SimpleValueProvider(),
                OperationBindingContext = new OperationBindingContext
                {
                    ActionContext = new ActionContext()
                    {
                        HttpContext = new DefaultHttpContext(),
                    },
                    MetadataProvider = metadataProvider,
                },
                ValidationState = new ValidationStateDictionary(),
            };

            return bindingContext;
        }
    }
}
