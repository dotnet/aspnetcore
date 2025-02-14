// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

internal class KeyedServicesModelBinder : IModelBinder
{
    private readonly object _key;
    private readonly bool _isOptional;

    public KeyedServicesModelBinder(object key, bool isOptional)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        _isOptional = isOptional;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var keyedServices = bindingContext.HttpContext.RequestServices as IKeyedServiceProvider;
        if (keyedServices == null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var model = _isOptional ?
            keyedServices.GetKeyedService(bindingContext.ModelType, _key) :
            keyedServices.GetRequiredKeyedService(bindingContext.ModelType, _key);

        if (model != null)
        {
            bindingContext.ValidationState.Add(model, new ValidationStateEntry() { SuppressValidation = true });
        }

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }
}
