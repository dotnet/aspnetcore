// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IValueProviderFactory"/> that creates <see cref="IValueProvider"/> instances that
    /// read values from the request query-string.
    /// </summary>
    public class QueryStringValueProviderFactory : IValueProviderFactory
    {
        /// <inheritdoc />
        public Task<IValueProvider> GetValueProviderAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult<IValueProvider>(new QueryStringValueProvider(
                BindingSource.Query,
                context.HttpContext.Request.Query,
                CultureInfo.InvariantCulture));
        }
    }
}
