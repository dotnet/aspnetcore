// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RouteValueValueProviderFactory : IValueProviderFactory
    {
        public Task<IValueProvider> GetValueProviderAsync(RequestContext requestContext)
        {
            var valueProvider = new DictionaryBasedValueProvider(requestContext.RouteValues);
            return Task.FromResult<IValueProvider>(valueProvider);
        }
    }
}
