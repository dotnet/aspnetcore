// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RouteValueValueProviderFactory : IValueProviderFactory
    {
        public Task<IValueProvider> GetValueProviderAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult<IValueProvider>(new DictionaryBasedValueProvider(
                BindingSource.Path,
                context.RouteData.Values));
        }
    }
}
