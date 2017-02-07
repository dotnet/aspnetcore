// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public abstract class PageArgumentBinder
    {
        public async Task<object> BindModelAsync(PageContext context, Type type, object defaultValue, string name)
        {
            var result = await BindAsync(context, value: null, name: name, type: type);
            return result.IsModelSet ? result.Model : defaultValue;
        }

        protected abstract Task<ModelBindingResult> BindAsync(PageContext context, object value, string name, Type type);
    }
}
