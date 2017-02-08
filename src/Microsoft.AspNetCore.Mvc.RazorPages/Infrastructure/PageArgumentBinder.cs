// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public abstract class PageArgumentBinder
    {
        public async Task<object> BindModelAsync(PageContext context, Type type, object @default, string name)
        {
            var result = await BindAsync(context, null, name, type);
            return result.IsModelSet ? result.Model : @default;
        }

        public Task<T> BindModelAsync<T>(PageContext context, string name)
        {
            return BindModelAsync<T>(context, default(T), name);
        }

        public async Task<T> BindModelAsync<T>(PageContext context, T @default, string name)
        {
            var result = await BindAsync(context, null, name, typeof(T));
            return result.IsModelSet ? (T)result.Model : @default;
        }

        public async Task<bool> TryUpdateModelAsync<T>(PageContext context, T value)
        {
            var result = await BindAsync(context, value, string.Empty, typeof(T));
            return result.IsModelSet && context.ModelState.IsValid;
        }

        public async Task<bool> TryUpdateModelAsync<T>(PageContext context, T value, string name)
        {
            var result = await BindAsync(context, value, name, typeof(T));
            return result.IsModelSet && context.ModelState.IsValid;
        }

        protected abstract Task<ModelBindingResult> BindAsync(PageContext context, object value, string name, Type type);
    }
}
