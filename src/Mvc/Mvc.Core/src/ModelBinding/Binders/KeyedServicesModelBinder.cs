// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> which binds models from the request services when a model
/// has the binding source <see cref="BindingSource.KeyedServices"/>.
/// </summary>
public class KeyedServicesModelBinder : IModelBinder
{
    internal bool IsOptional { get; set; }

    internal object? Key { get; set; }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var requestServices = bindingContext.HttpContext.RequestServices as IKeyedServiceProvider;
        if (requestServices == null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var model = IsOptional ?
            requestServices.GetKeyedService(bindingContext.ModelType, Key) :
            requestServices.GetRequiredKeyedService(bindingContext.ModelType, Key);

        if (model != null)
        {
            bindingContext.ValidationState.Add(model, new ValidationStateEntry() { SuppressValidation = true });
        }

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }
}
