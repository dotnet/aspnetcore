// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RouteValueValueProviderFactory : IValueProviderFactory
    {
        public IValueProvider GetValueProvider([NotNull] RouteContext routeContext)
        {
            return new DictionaryBasedValueProvider(routeContext.RouteData.Values);
        }
    }
}
