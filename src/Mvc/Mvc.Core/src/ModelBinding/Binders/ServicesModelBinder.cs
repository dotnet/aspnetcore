// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> which binds models from the request services when a model
/// has the binding source <see cref="BindingSource.Services"/>.
/// </summary>
public class ServicesModelBinder : IModelBinder
{
    internal bool IsOptional { get; set; }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var requestServices = bindingContext.HttpContext.RequestServices;
        var model = IsOptional ?
            requestServices.GetService(bindingContext.ModelType) :
            requestServices.GetRequiredService(bindingContext.ModelType);

        if (model != null)
        {
            bindingContext.ValidationState.Add(model, new ValidationStateEntry() { SuppressValidation = true });
        }

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }
}
