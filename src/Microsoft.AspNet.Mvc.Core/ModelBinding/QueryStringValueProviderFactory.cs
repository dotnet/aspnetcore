// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="IValueProviderFactory"/> that creates <see cref="IValueProvider"/> instances that
    /// read values from the request query-string.
    /// </summary>
    public class QueryStringValueProviderFactory : IValueProviderFactory
    {
        /// <inheritdoc />
        public Task<IValueProvider> GetValueProviderAsync([NotNull] ValueProviderFactoryContext context)
        {
            return Task.FromResult<IValueProvider>(new ReadableStringCollectionValueProvider(
                BindingSource.Query,
                context.HttpContext.Request.Query,
                CultureInfo.InvariantCulture));
        }
    }
}
