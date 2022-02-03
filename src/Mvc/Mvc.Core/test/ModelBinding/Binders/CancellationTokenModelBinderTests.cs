// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class CancellationTokenModelBinderTests
{
    [Fact]
    public async Task CancellationTokenModelBinder_ReturnsNonEmptyResult_ForCancellationTokenType()
    {
        // Arrange
        var bindingContext = GetBindingContext(typeof(CancellationToken));
        var binder = new CancellationTokenModelBinder();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Equal(bindingContext.HttpContext.RequestAborted, bindingContext.Result.Model);
    }

    private static DefaultModelBindingContext GetBindingContext(Type modelType)
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        DefaultModelBindingContext bindingContext = new DefaultModelBindingContext
        {
            ActionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext(),
            },
            ModelMetadata = metadataProvider.GetMetadataForType(modelType),
            ModelName = "someName",
            ValueProvider = new SimpleValueProvider(),
            ValidationState = new ValidationStateDictionary(),
        };

        return bindingContext;
    }
}
