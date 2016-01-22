// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FormValueProviderFactory : IValueProviderFactory
    {
        public Task<IValueProvider> GetValueProviderAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.HttpContext.Request;
            if (request.HasFormContentType)
            {
                return CreateValueProviderAsync(request);
            }

            return TaskCache<IValueProvider>.DefaultCompletedTask;
        }

        private static async Task<IValueProvider> CreateValueProviderAsync(HttpRequest request)
        {
            return new FormValueProvider(
                BindingSource.Form,
                await request.ReadFormAsync(),
                CultureInfo.CurrentCulture);
        }
    }
}
