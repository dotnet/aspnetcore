// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    [Obsolete("This type is obsolete and will be removed in a future version.")]
    public abstract class PageArgumentBinder
    {
        public async Task<object> BindModelAsync(PageContext context, Type type, object @default, string name)
        {
            var result = await BindAsync(context, null, name, type);
            return result.IsModelSet ? result.Model : @default;
        }

        public Task<TModel> BindModelAsync<TModel>(PageContext context, string name)
        {
            return BindModelAsync(context, default(TModel), name);
        }

        public async Task<TModel> BindModelAsync<TModel>(PageContext context, TModel @default, string name)
        {
            var result = await BindAsync(context, null, name, typeof(TModel));
            return result.IsModelSet ? (TModel)result.Model : @default;
        }

        public async Task<bool> TryUpdateModelAsync<TModel>(PageContext context, TModel value)
        {
            var result = await BindAsync(context, value, string.Empty, typeof(TModel));
            return result.IsModelSet && context.ModelState.IsValid;
        }

        public async Task<bool> TryUpdateModelAsync<TModel>(PageContext context, TModel value, string name)
        {
            var result = await BindAsync(context, value, name, typeof(TModel));
            return result.IsModelSet && context.ModelState.IsValid;
        }

        protected abstract Task<ModelBindingResult> BindAsync(PageContext context, object value, string name, Type type);
    }
}
