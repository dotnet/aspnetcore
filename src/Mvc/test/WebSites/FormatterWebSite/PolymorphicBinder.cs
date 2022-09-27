// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FormatterWebSite.Controllers;

public class PolymorphicBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var model = new DerivedModel
        {
            DerivedProperty = bindingContext.ValueProvider.GetValue(nameof(DerivedModel.DerivedProperty)).FirstValue,
        };

        bindingContext.Result = ModelBindingResult.Success(model);

        return Task.CompletedTask;
    }
}
