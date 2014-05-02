// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class QueryStringValueProviderFactory : IValueProviderFactory
    {
        private static readonly object _cacheKey = new object();

        public Task<IValueProvider> GetValueProviderAsync([NotNull] RequestContext requestContext)
        {
            // Process the query collection once-per request. 
            var storage = requestContext.HttpContext.Items;
            object value;
            IValueProvider provider;
            if (!storage.TryGetValue(_cacheKey, out value))
            {
                var queryCollection = requestContext.HttpContext.Request.Query;
                provider = new ReadableStringCollectionValueProvider(queryCollection, CultureInfo.InvariantCulture);
                storage[_cacheKey] = provider;
            }
            else
            {
                provider = (ReadableStringCollectionValueProvider)value;
            }
            return Task.FromResult(provider);
        }
    }
}
