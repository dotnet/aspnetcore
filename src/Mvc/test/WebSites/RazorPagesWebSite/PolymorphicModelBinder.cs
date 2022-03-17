// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RazorPagesWebSite;

public class PolymorphicModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var ageValue = bindingContext.ValueProvider.GetValue(nameof(UserModel.Age));
        var age = 0;
        if (ageValue.Length != 0)
        {
            age = int.Parse(ageValue.FirstValue, CultureInfo.InvariantCulture);
        }

        var model = new UserModel
        {
            Name = bindingContext.ValueProvider.GetValue(nameof(UserModel.Name)).FirstValue,
            Age = age,
        };

        bindingContext.Result = ModelBindingResult.Success(model);

        return Task.CompletedTask;
    }
}
