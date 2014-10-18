// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class QueryStringValueProviderFactory : IValueProviderFactory
    {
        private static readonly object _cacheKey = new object();

        public IValueProvider GetValueProvider([NotNull] ValueProviderFactoryContext context)
        {
            // Process the query collection once-per request. 
            var storage = context.HttpContext.Items;
            object value;
            IValueProvider provider;
            if (!storage.TryGetValue(_cacheKey, out value))
            {
                var queryCollection = context.HttpContext.Request.Query;
                provider = new ReadableStringCollectionValueProvider<IQueryValueProviderMetadata>(queryCollection,
                                                                                         CultureInfo.InvariantCulture);
                storage[_cacheKey] = provider;
            }
            else
            {
                provider = (ReadableStringCollectionValueProvider<IQueryValueProviderMetadata>)value;
            }
            return provider;
        }
    }
}
