// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public static class ModelBinderExtensions
    {
        public static async Task<ModelBindingResult> BindModelResultAsync(
            this IModelBinder binder, 
            ModelBindingContext context)
        {
            await binder.BindModelAsync(context);
            return context.Result ?? default(ModelBindingResult);
        }
    }
}
