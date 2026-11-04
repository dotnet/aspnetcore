// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class ServicesModelBinderTest
{
    [Fact]
    public async Task ServiceModelBinder_BindsService()
    {
        // Arrange
        var type = typeof(IService);

        var binder = new ServicesModelBinder();
        var bindingContext = GetBindingContext(type);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);

        var entry = bindingContext.ValidationState[bindingContext.Result.Model];
        Assert.True(entry.SuppressValidation);
        Assert.Null(entry.Key);
        Assert.Null(entry.Metadata);
    }

    private static DefaultModelBindingContext GetBindingContext(Type modelType)
    {
        var metadataProvider = new TestModelMetadataProvider();
        metadataProvider.ForType(modelType).BindingDetails(d => d.BindingSource = BindingSource.Services);
        var modelMetadata = metadataProvider.GetMetadataForType(modelType);

        var services = new ServiceCollection();
        services.AddSingleton<IService>(new Service());

        var bindingContext = new DefaultModelBindingContext
        {
            ActionContext = new ActionContext()
            {
                HttpContext = new DefaultHttpContext()
                {
                    RequestServices = services.BuildServiceProvider(),
                }
            },
            ModelMetadata = modelMetadata,
            ModelName = "modelName",
            FieldName = "modelName",
            ModelState = new ModelStateDictionary(),
            BinderModelName = modelMetadata.BinderModelName,
            BindingSource = modelMetadata.BindingSource,
            ValidationState = new ValidationStateDictionary(),
        };

        return bindingContext;
    }

    private interface IService
    {
    }

    private class Service : IService
    {
    }
}
