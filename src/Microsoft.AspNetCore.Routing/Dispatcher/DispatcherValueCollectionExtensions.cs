// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Dispatcher
{
    public static class DispatcherValueCollectionExtensions
    {
        public static RouteValueDictionary AsRouteValueDictionary(this DispatcherValueCollection values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return values as RouteValueDictionary ?? new RouteValueDictionary(values);
        }
    }
}
