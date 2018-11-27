// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RazorPagesWebSite
{
    public class PolymorphicModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var ageValue = bindingContext.ValueProvider.GetValue(nameof(UserModel.Age));
            var age = 0;
            if (ageValue.Length != 0)
            {
                age = int.Parse(ageValue.FirstValue);
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
}
